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
    public JigglePoseJob jigglePoseJob;
    public bool hasJobHandle;
    public JobHandle jobHandle;
    public JobHandle bulkReadHandle;
    public bool hasBulkReadHandle;
    public JobHandle poseHandle;
    public bool hasPoseHandle;
    
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
        jigglePoseJob = new JigglePoseJob() {
            previousSolve = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
            currentSolve = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
            previousLocalPositions = bulkRead.previousLocalPositions,
            previousLocalRotations = bulkRead.previousLocalRotations,
            previousSimulatedRootOffset = Vector3.zero,
            currentSimulatedRootOffset = Vector3.zero,
            previousSimulatedRootPosition = bones[0].position,
            currentSimulatedRootPosition = bones[0].position,
            timeStamp = Time.timeAsDouble,
            previousTimeStamp = Time.timeAsDouble-JiggleJobManager.FIXED_DELTA_TIME,
        };
        jigglePoseJob.currentSolve.CopyFrom(currentSolve);
        jigglePoseJob.previousSolve.CopyFrom(currentSolve);
        
        transformAccessArray = new TransformAccessArray(bones);
        restPoseTransforms = new Matrix4x4[boneCount];
        RecordAllRestPoseTransforms(bones, restPoseTransforms);
        bulkRead.restPoseMatrices.CopyFrom(restPoseTransforms);
    }

    private void PushBack(JiggleJob job) {
        Profiler.BeginSample("JiggleTree.Pushback");
        if (hasPoseHandle) {
            poseHandle.Complete();
        }
        jigglePoseJob.previousTimeStamp = jigglePoseJob.timeStamp;
        (jigglePoseJob.currentSolve, jigglePoseJob.previousSolve) = (jigglePoseJob.previousSolve, jigglePoseJob.currentSolve);
        //jigglePoseJob.currentSolve.CopyTo(jigglePoseJob.previousSolve);
        jigglePoseJob.timeStamp = job.timeStamp;
        job.output.CopyTo(jigglePoseJob.currentSolve);
        
        jigglePoseJob.previousSimulatedRootOffset = jigglePoseJob.currentSimulatedRootOffset;
        jigglePoseJob.currentSimulatedRootOffset = job.simulatedPoints[1].position - job.simulatedPoints[1].pose;
        
        jigglePoseJob.previousSimulatedRootPosition = jigglePoseJob.currentSimulatedRootPosition;
        jigglePoseJob.currentSimulatedRootPosition = job.simulatedPoints[1].position;
        
        Profiler.EndSample();
    }
    
    public void Simulate(double currentTime) {
        if (dirty) return;
        Profiler.BeginSample("JiggleTree.Simulate");
        Profiler.BeginSample("JiggleTree.CompletePreviousJob");
        if (hasJobHandle) {
            jobHandle.Complete();
            //DrawDebug(jiggleJob);
            PushBack(jiggleJob);
        }
        Profiler.EndSample();
        Profiler.BeginSample("JiggleTree.PrepareJobs");
        jiggleJob.timeStamp = currentTime;
        jiggleJob.gravity = Physics.gravity;
        bulkRead.restPoseMatrices.CopyFrom(restPoseTransforms);
        Profiler.EndSample();
        Profiler.BeginSample("JiggleTree.ScheduleJobs");
        bulkReadHandle = hasPoseHandle ? bulkRead.Schedule(transformAccessArray, poseHandle) : bulkRead.Schedule(transformAccessArray);
        hasBulkReadHandle = true;
        jobHandle = jiggleJob.Schedule(bulkReadHandle);
        hasJobHandle = true;
        Profiler.EndSample();
        Profiler.EndSample();
    }

    public void SchedulePose() {
        Profiler.BeginSample("JiggleTree.SchedulePose");
        jigglePoseJob.realRootPosition = bones[0].position;
        jigglePoseJob.currentTime = Time.timeAsDouble;
        poseHandle = hasBulkReadHandle ? jigglePoseJob.Schedule(transformAccessArray, bulkReadHandle) : jigglePoseJob.Schedule(transformAccessArray);
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
        if (jigglePoseJob.previousSolve.IsCreated) {
            jigglePoseJob.previousSolve.Dispose();
        }
        if (jigglePoseJob.currentSolve.IsCreated) {
            jigglePoseJob.currentSolve.Dispose();
        }
    }

    private static void DrawDebug(JiggleJob job) {
        for (var index = 0; index < job.simulatedPoints.Length; index++) {
            var simulatedPoint = job.simulatedPoints[index];
            if (simulatedPoint.parentIndex == -1) continue;
            DebugDrawSphere(job.debug[simulatedPoint.parentIndex], 0.2f, Color.cyan, (float)JiggleJobManager.FIXED_DELTA_TIME);
            Debug.DrawLine(job.debug[index], job.debug[simulatedPoint.parentIndex], Color.cyan, (float)JiggleJobManager.FIXED_DELTA_TIME);
        }
    }
    
    private static void DebugDrawSphere(Vector3 origin, float radius, Color color, float duration, int segments = 32) {
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
