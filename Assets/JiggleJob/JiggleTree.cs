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
    
    public JiggleBulkTransformRead bulkRead;
    public TransformAccessArray transformAccessArray;
    public JiggleJob jiggleJob;
    public JiggleInterpolationJob jiggleJobInterpolation;
    public JiggleJobTransformWrite jiggleJobTransformWrite;
    public JiggleJobPrepareInterpolation jiggleJobPrepareInterpolation;
    public bool hasJobHandle;
    public JobHandle jobHandle;
    public JobHandle bulkReadHandle;
    public bool hasBulkReadHandle;
    
    public JobHandle poseHandle;
    public bool hasPoseHandle;

    public JobHandle interpolateHandle;
    public JobHandle prepareInterpolationHandle;
    public bool hasPrepareInterpolationHandle;
    
    private Vector3 previousRootPosition;
    private Vector3 currentRootPosition;

    public Matrix4x4[] restPoseTransforms;

    NativeArray<Matrix4x4> sharedMatrices;

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

        jiggleJob = new JiggleJob() {
            transformMatrices = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
            debug = new NativeArray<Vector3>(pointCount, Allocator.Persistent),
            simulatedPoints = new NativeArray<JiggleBoneSimulatedPoint>(pointCount, Allocator.Persistent),
            output = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
        };
        jiggleJob.transformMatrices.CopyFrom(currentSolve);
        jiggleJob.simulatedPoints.CopyFrom(points);
        jiggleJob.output.CopyFrom(currentSolve);
        
        bulkRead = new JiggleBulkTransformRead() {
            matrices = jiggleJob.transformMatrices,
            restPoseMatrices = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
            previousLocalPositions = new NativeArray<Vector3>(boneCount, Allocator.Persistent),
            previousLocalRotations = new NativeArray<Quaternion>(boneCount, Allocator.Persistent),
            animated = new NativeArray<bool>(boneCount, Allocator.Persistent)
        };
        jiggleJobInterpolation = new JiggleInterpolationJob() {
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

        jiggleJobTransformWrite = new JiggleJobTransformWrite() {
            previousLocalPositions = bulkRead.previousLocalPositions,
            previousLocalRotations = bulkRead.previousLocalRotations,
            outputInterpolatedPositions = jiggleJobInterpolation.outputInterpolatedPositions,
            outputInterpolatedRotations = jiggleJobInterpolation.outputInterpolatedRotations,
        };
        
        jiggleJobInterpolation.currentSolve.CopyFrom(currentSolve);
        jiggleJobInterpolation.previousSolve.CopyFrom(currentSolve);

        jiggleJobPrepareInterpolation = new JiggleJobPrepareInterpolation() {
            simulatedPoints = jiggleJob.simulatedPoints,
            outputPoses = jiggleJob.output,
            inputPoses = jiggleJob.transformMatrices,
            currentSolve = jiggleJobInterpolation.currentSolve,
            previousSimulatedRootOffset = jiggleJobInterpolation.previousSimulatedRootOffset,
            currentSimulatedRootOffset = jiggleJobInterpolation.currentSimulatedRootOffset,
            previousSimulatedRootPosition = jiggleJobInterpolation.previousSimulatedRootPosition,
            currentSimulatedRootPosition = jiggleJobInterpolation.currentSimulatedRootPosition,
            previousTimeStamp = jiggleJobInterpolation.previousTimeStamp,
            currentTimeStamp = jiggleJobInterpolation.timeStamp,
        };
        
        transformAccessArray = new TransformAccessArray(bones);
        restPoseTransforms = new Matrix4x4[boneCount];
        RecordAllRestPoseTransforms(bones, restPoseTransforms);
        bulkRead.restPoseMatrices.CopyFrom(restPoseTransforms);
    }

    private void PushBack() {
        Profiler.BeginSample("JiggleTree.Pushback");
        if (hasPoseHandle) {
            poseHandle.Complete();
        }
        jiggleJobPrepareInterpolation.incomingTimeStamp = jiggleJob.timeStamp;
        (jiggleJobInterpolation.currentSolve, jiggleJobInterpolation.previousSolve) = (jiggleJobInterpolation.previousSolve, jiggleJobInterpolation.currentSolve);
        jiggleJobPrepareInterpolation.currentSolve = jiggleJobInterpolation.currentSolve;
        prepareInterpolationHandle = jiggleJobPrepareInterpolation.Schedule();
        hasPrepareInterpolationHandle = true;
        Profiler.EndSample();
    }
    
    public void Simulate(double currentTime) {
        if (dirty) return;
        Profiler.BeginSample("JiggleTree.Simulate");
        Profiler.BeginSample("JiggleTree.CompletePreviousJob");
        if (hasJobHandle) {
            jobHandle.Complete();
            //DrawDebug(jiggleJob);
            PushBack();
        }
        Profiler.EndSample();
        Profiler.BeginSample("JiggleTree.PrepareJobs");
        jiggleJob.timeStamp = currentTime;
        jiggleJob.gravity = Physics.gravity;
        Profiler.EndSample();
        Profiler.BeginSample("JiggleTree.ScheduleJobs");
        // TODO: Reading transforms shouldn't be reliant on interpolation
        if (hasPrepareInterpolationHandle) {
            bulkReadHandle = bulkRead.Schedule(transformAccessArray, prepareInterpolationHandle);
        } else {
            bulkReadHandle = bulkRead.Schedule(transformAccessArray);
        }
        hasBulkReadHandle = true;

        jobHandle = jiggleJob.Schedule(bulkReadHandle);
        hasJobHandle = true;
        Profiler.EndSample();
        Profiler.EndSample();
    }

    public void SchedulePose() {
        Profiler.BeginSample("JiggleTree.SchedulePose");
        jiggleJobInterpolation.realRootPosition = bones[0].position;
        jiggleJobInterpolation.currentTime = Time.timeAsDouble;
        if (hasPrepareInterpolationHandle) {
            interpolateHandle = jiggleJobInterpolation.ScheduleParallel(bones.Length, 32, prepareInterpolationHandle);
        } else {
            interpolateHandle = jiggleJobInterpolation.ScheduleParallel(bones.Length, 32, default);
        }

        // TODO: Posing shouldn't rely on the bulk read, maybe duplicate some data?
        if (hasBulkReadHandle) {
            poseHandle = jiggleJobTransformWrite.Schedule(transformAccessArray, JobHandle.CombineDependencies(interpolateHandle, bulkReadHandle));
        } else {
            poseHandle = jiggleJobTransformWrite.Schedule(transformAccessArray, interpolateHandle);
        }

        hasPoseHandle = true;
        Profiler.EndSample();
    }

    public void CompletePose() {
        Profiler.BeginSample("JiggleTree.CompletePose");
        if (hasPoseHandle) {
            poseHandle.Complete();
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
        if (hasJobHandle) {
            jobHandle.Complete();
        }
        if (hasBulkReadHandle) {
            bulkReadHandle.Complete();
        }
        if (hasPoseHandle) {
            poseHandle.Complete();
        }
        
        transformAccessArray.Dispose();
        
        if (jiggleJob.transformMatrices.IsCreated) {
            jiggleJob.transformMatrices.Dispose();
        }
        if (jiggleJob.debug.IsCreated) {
            jiggleJob.debug.Dispose();
        }
        if (jiggleJob.simulatedPoints.IsCreated) {
            jiggleJob.simulatedPoints.Dispose();
        }
        if (jiggleJob.output.IsCreated) {
            jiggleJob.output.Dispose();
        }
        if (bulkRead.restPoseMatrices.IsCreated) {
            bulkRead.restPoseMatrices.Dispose();
        }
        if (bulkRead.previousLocalPositions.IsCreated) {
            bulkRead.previousLocalPositions.Dispose();
        }
        if (bulkRead.previousLocalRotations.IsCreated) {
            bulkRead.previousLocalRotations.Dispose();
        }
        if (bulkRead.animated.IsCreated) {
            bulkRead.animated.Dispose();
        }
        if (jiggleJobInterpolation.previousSolve.IsCreated) {
            jiggleJobInterpolation.previousSolve.Dispose();
        }
        if (jiggleJobInterpolation.currentSolve.IsCreated) {
            jiggleJobInterpolation.currentSolve.Dispose();
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
