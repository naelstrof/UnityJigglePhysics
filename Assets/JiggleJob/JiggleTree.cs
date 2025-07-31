using Unity.Collections;
using Unity.Jobs;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

// TODO: One IJobParallelForTransform for each jiggle tree so that it represents a single transform root
// NOT an IJobParallelForTransform for each bone
public class JiggleTree {
    public Transform[] bones;
    public JiggleBoneSimulatedPoint[] points;
    
    public TransformAccessArray transformAccessArray;
    public JiggleJobBulkTransformRead jobBulkRead;
    public JobHandle handleBulkRead;
    public bool hasHandleBulkRead;
    
    public JiggleJob jobSimulate;
    public bool hasHandleSimulate;
    public JobHandle handleSimulate;
    
    public JiggleJobInterpolation jobInterpolation;
    public JobHandle handleInterpolate;
    
    public JiggleJobTransformWrite jobTransformWrite;
    public JobHandle handleTransformWrite;
    public bool hasHandleTrasnformWrite;
    
    public JiggleJobPrepareInterpolation jobPrepareInterpolation;
    public JobHandle handlePrepareInterpolation;
    public bool hasHandlePrepareInterpolation;

    public Matrix4x4[] restPoseTransforms;

    public bool dirty;

    public JiggleTree(Transform[] bones, JiggleBoneSimulatedPoint[] points) {
        dirty = true;
        var boneCount = bones.Length;
        var pointCount = points.Length;
        this.bones = new Transform[boneCount];
        this.points = new JiggleBoneSimulatedPoint[pointCount];
        var currentSolve = new Matrix4x4[boneCount];
        for (int i = 0; i < pointCount; i++) {
            this.points[i] = points[i];
        }
        for (int i = 0; i < boneCount; i++) {
            this.bones[i] = bones[i];
            currentSolve[i] = bones[i].localToWorldMatrix;
        }

        jobSimulate = new JiggleJob() {
            transformMatrices = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
            debug = new NativeArray<Vector3>(pointCount, Allocator.Persistent),
            simulatedPoints = new NativeArray<JiggleBoneSimulatedPoint>(pointCount, Allocator.Persistent),
            output = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
        };
        jobSimulate.transformMatrices.CopyFrom(currentSolve);
        jobSimulate.simulatedPoints.CopyFrom(points);
        jobSimulate.output.CopyFrom(currentSolve);
        
        jobBulkRead = new JiggleJobBulkTransformRead() {
            matrices = jobSimulate.transformMatrices,
            restPoseMatrices = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
            previousLocalPositions = new NativeArray<Vector3>(boneCount, Allocator.Persistent),
            previousLocalRotations = new NativeArray<Quaternion>(boneCount, Allocator.Persistent),
            animated = new NativeArray<bool>(boneCount, Allocator.Persistent)
        };
        jobInterpolation = new JiggleJobInterpolation() {
            previousSolve = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
            currentSolve = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
            previousSimulatedRootOffset = new NativeReference<Vector3>(Vector3.zero, Allocator.Persistent),
            currentSimulatedRootOffset = new NativeReference<Vector3>(Vector3.zero, Allocator.Persistent),
            previousSimulatedRootPosition = new NativeReference<Vector3>(bones[0].position, Allocator.Persistent),
            currentSimulatedRootPosition = new NativeReference<Vector3>(bones[0].position, Allocator.Persistent),
            timeStamp = new NativeReference<double>(Time.timeAsDouble, Allocator.Persistent),
            previousTimeStamp = new NativeReference<double>(Time.timeAsDouble-JiggleJobManager.FIXED_DELTA_TIME, Allocator.Persistent),
            outputInterpolatedPositions = new NativeArray<Vector3>(boneCount, Allocator.Persistent),
            outputInterpolatedRotations = new NativeArray<Quaternion>(boneCount, Allocator.Persistent),
        };

        jobTransformWrite = new JiggleJobTransformWrite() {
            previousLocalPositions = jobBulkRead.previousLocalPositions,
            previousLocalRotations = jobBulkRead.previousLocalRotations,
            outputInterpolatedPositions = jobInterpolation.outputInterpolatedPositions,
            outputInterpolatedRotations = jobInterpolation.outputInterpolatedRotations,
        };
        
        jobInterpolation.currentSolve.CopyFrom(currentSolve);
        jobInterpolation.previousSolve.CopyFrom(currentSolve);

        jobPrepareInterpolation = new JiggleJobPrepareInterpolation() {
            outputPoses = jobSimulate.output,
            inputPoses = jobSimulate.transformMatrices,
            currentSolve = jobInterpolation.currentSolve,
            previousSimulatedRootOffset = jobInterpolation.previousSimulatedRootOffset,
            currentSimulatedRootOffset = jobInterpolation.currentSimulatedRootOffset,
            previousSimulatedRootPosition = jobInterpolation.previousSimulatedRootPosition,
            currentSimulatedRootPosition = jobInterpolation.currentSimulatedRootPosition,
            previousTimeStamp = jobInterpolation.previousTimeStamp,
            currentTimeStamp = jobInterpolation.timeStamp,
        };
        
        transformAccessArray = new TransformAccessArray(bones);
        restPoseTransforms = new Matrix4x4[boneCount];
        RecordAllRestPoseTransforms(bones, restPoseTransforms);
        jobBulkRead.restPoseMatrices.CopyFrom(restPoseTransforms);
    }

    private void PushBack() {
        Profiler.BeginSample("JiggleTree.Pushback");
        if (hasHandleTrasnformWrite) {
            handleTransformWrite.Complete();
        }
        jobPrepareInterpolation.incomingTimeStamp = jobSimulate.timeStamp;
        (jobInterpolation.currentSolve, jobInterpolation.previousSolve) = (jobInterpolation.previousSolve, jobInterpolation.currentSolve);
        jobPrepareInterpolation.currentSolve = jobInterpolation.currentSolve;
        handlePrepareInterpolation = jobPrepareInterpolation.Schedule();
        hasHandlePrepareInterpolation = true;
        Profiler.EndSample();
    }
    
    public void Simulate(double currentTime) {
        if (dirty) return;
        Profiler.BeginSample("JiggleTree.Simulate");
        Profiler.BeginSample("JiggleTree.CompletePreviousJob");
        if (hasHandleSimulate) {
            handleSimulate.Complete();
            //DrawDebug(jiggleJob);
            PushBack();
        }
        Profiler.EndSample();
        Profiler.BeginSample("JiggleTree.PrepareJobs");
        jobSimulate.timeStamp = currentTime;
        jobSimulate.gravity = Physics.gravity;
        Profiler.EndSample();
        Profiler.BeginSample("JiggleTree.ScheduleJobs");
        // TODO: Reading transforms shouldn't be reliant on interpolation
        if (hasHandlePrepareInterpolation) {
            handleBulkRead = jobBulkRead.Schedule(transformAccessArray, handlePrepareInterpolation);
        } else {
            handleBulkRead = jobBulkRead.Schedule(transformAccessArray);
        }
        hasHandleBulkRead = true;

        handleSimulate = jobSimulate.Schedule(handleBulkRead);
        hasHandleSimulate = true;
        Profiler.EndSample();
        Profiler.EndSample();
    }

    public void SchedulePose() {
        Profiler.BeginSample("JiggleTree.SchedulePose");
        jobInterpolation.realRootPosition = bones[0].position;
        jobInterpolation.currentTime = Time.timeAsDouble;
        if (hasHandlePrepareInterpolation) {
            handleInterpolate = jobInterpolation.ScheduleParallel(bones.Length, 32, handlePrepareInterpolation);
        } else {
            handleInterpolate = jobInterpolation.ScheduleParallel(bones.Length, 32, default);
        }

        // TODO: Posing shouldn't rely on the bulk read, maybe duplicate some data?
        if (hasHandleBulkRead) {
            handleTransformWrite = jobTransformWrite.Schedule(transformAccessArray, JobHandle.CombineDependencies(handleInterpolate, handleBulkRead));
        } else {
            handleTransformWrite = jobTransformWrite.Schedule(transformAccessArray, handleInterpolate);
        }

        hasHandleTrasnformWrite = true;
        Profiler.EndSample();
    }

    public void CompletePose() {
        Profiler.BeginSample("JiggleTree.CompletePose");
        if (hasHandleTrasnformWrite) {
            handleTransformWrite.Complete();
        }
        Profiler.EndSample();
    }

    /*
    public void Pose() {
        var boneCount = bones.Length;
        for (int i = 0; i < boneCount; i++) {
            var prevPosition = previousSolve[i].GetPosition();
            var prevRotation = previousSolve[i].rotation;

            var newPosition = currentSolve[i].GetPosition();
            var newRotation = currentSolve[i].rotation;


            var diff = timeStamp - previousTimeStamp;
            if (diff == 0) {
                throw new UnityException("Time difference is zero, cannot interpolate.");
                return;
            }

            // TODO: Revisit this issue after FEELING the solve in VR in context
            // The issue here is that we are having to operate 3 full frames in the past
            // which might be noticable latency
            var timeCorrection = JiggleJobManager.FIXED_DELTA_TIME * 2f;
            var t = (Time.timeAsDouble-timeCorrection - previousTimeStamp) / diff;
            var position = Vector3.LerpUnclamped(prevPosition, newPosition, (float)t);
            var rotation = Quaternion.SlerpUnclamped(prevRotation, newRotation, (float)t);
            //Debug.DrawRay(position + Vector3.up*Mathf.Repeat(Time.timeSinceLevelLoad,5f), Vector3.up, Color.magenta, 5f);
            // NULL CHECK (OPTIONAL IF YOU WANT TO BE REALLY CAREFUL)
            if (!bones[i]) {
                dirty = true;
                MonobehaviourHider.JiggleRoot.SetDirty();
                return;
            }
            var timeOffset = Vector3.LerpUnclamped(lastPositionTimeOffset, positionTimeOffset, (float)t);
            bones[i].SetPositionAndRotation(position + timeOffset, rotation);
        }
        for (int i = 0; i < boneCount; i++) {
            bones[i].GetLocalPositionAndRotation(out var localPosition, out var localRotation);
            previousLocalPositions[i] = localPosition;
            previousLocalRotations[i] = localRotation;
        }
    }
    */
    
    private static void RecordAllRestPoseTransforms(Transform[] bones, Matrix4x4[] output) {
        for (var index = 0; index < bones.Length; index++) {
            bones[index].GetLocalPositionAndRotation(out var localPosition, out var localRotation);
            output[index] = Matrix4x4.TRS(localPosition, localRotation, Vector3.one);
        }
    }

    public void Dispose() {
        if (hasHandleSimulate) {
            handleSimulate.Complete();
        }
        if (hasHandleBulkRead) {
            handleBulkRead.Complete();
        }
        if (hasHandleTrasnformWrite) {
            handleTransformWrite.Complete();
        }
        
        transformAccessArray.Dispose();
        
        if (jobSimulate.transformMatrices.IsCreated) {
            jobSimulate.transformMatrices.Dispose();
        }
        if (jobSimulate.debug.IsCreated) {
            jobSimulate.debug.Dispose();
        }
        if (jobSimulate.simulatedPoints.IsCreated) {
            jobSimulate.simulatedPoints.Dispose();
        }
        if (jobSimulate.output.IsCreated) {
            jobSimulate.output.Dispose();
        }
        if (jobBulkRead.restPoseMatrices.IsCreated) {
            jobBulkRead.restPoseMatrices.Dispose();
        }
        if (jobBulkRead.previousLocalPositions.IsCreated) {
            jobBulkRead.previousLocalPositions.Dispose();
        }
        if (jobBulkRead.previousLocalRotations.IsCreated) {
            jobBulkRead.previousLocalRotations.Dispose();
        }
        if (jobBulkRead.animated.IsCreated) {
            jobBulkRead.animated.Dispose();
        }
        if (jobInterpolation.previousSolve.IsCreated) {
            jobInterpolation.previousSolve.Dispose();
        }
        if (jobInterpolation.currentSolve.IsCreated) {
            jobInterpolation.currentSolve.Dispose();
        }
    }

    private static void DrawDebug(JiggleJob job) {
        for (var index = 0; index < job.simulatedPoints.Length; index++) {
            var simulatedPoint = job.simulatedPoints[index];
            if (simulatedPoint.parentIndex == -1) continue;
            DebugDrawSphere(job.debug[simulatedPoint.parentIndex], 0.05f, Color.cyan, (float)JiggleJobManager.FIXED_DELTA_TIME);
            Debug.DrawLine(job.debug[index], job.debug[simulatedPoint.parentIndex], Color.cyan, (float)JiggleJobManager.FIXED_DELTA_TIME);
        }
    }
    
    private static void DebugDrawSphere(Vector3 origin, float radius, Color color, float duration, int segments = 8) {
        float angleStep = 360f / segments;
        Vector3 prevPoint = Vector3.zero;
        Vector3 currPoint = Vector3.zero;

        // Draw circle in XY plane
        for (int i = 0; i <= segments; i++) {
            float angle = Mathf.Deg2Rad * i * angleStep;
            currPoint = origin + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            if (i > 0) Debug.DrawLine(prevPoint, currPoint, color, duration);
            prevPoint = currPoint;
        }

        // Draw circle in XZ plane
        for (int i = 0; i <= segments; i++) {
            float angle = Mathf.Deg2Rad * i * angleStep;
            currPoint = origin + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            if (i > 0) Debug.DrawLine(prevPoint, currPoint, color, duration);
            prevPoint = currPoint;
        }

        // Draw circle in YZ plane
        for (int i = 0; i <= segments; i++) {
            float angle = Mathf.Deg2Rad * i * angleStep;
            currPoint = origin + new Vector3(0, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            if (i > 0) Debug.DrawLine(prevPoint, currPoint, color, duration);
            prevPoint = currPoint;
        }
    }
    
}
