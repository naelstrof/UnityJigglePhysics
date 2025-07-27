using Unity.Collections;
using Unity.Jobs;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.Jobs;

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

    public Matrix4x4[] restPoseTransforms;

    Matrix4x4[] matrices;

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
        jiggleJob.simulatedPoints.CopyFrom(points);
        bulkRead = new JiggleBulkTransformRead() {
            matrices = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
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
            timeStamp = Time.timeAsDouble,
            previousTimeStamp = Time.timeAsDouble-JiggleJobManager.FIXED_DELTA_TIME,
        };
        jigglePoseJob.currentSolve.CopyFrom(currentSolve);
        jigglePoseJob.previousSolve.CopyFrom(currentSolve);
        
        transformAccessArray = new TransformAccessArray(bones);
        restPoseTransforms = new Matrix4x4[boneCount];
        RecordAllRestPoseTransforms();
        matrices = new Matrix4x4[bones.Length];
    }

    private void PushBack(JiggleJob job) {
        if (hasPoseHandle) {
            poseHandle.Complete();
        }
        jigglePoseJob.previousTimeStamp = jigglePoseJob.timeStamp;
        jigglePoseJob.currentSolve.CopyTo(jigglePoseJob.previousSolve);
        jigglePoseJob.timeStamp = job.timeStamp;
        job.output.CopyTo(jigglePoseJob.currentSolve);
        jigglePoseJob.lastPositionTimeOffset = jigglePoseJob.positionTimeOffset;
        // TODO: This is slow, double traversal of native arrays
        jigglePoseJob.positionTimeOffset = jigglePoseJob.currentSolve[0].GetPosition() - jigglePoseJob.previousSolve[0].GetPosition();
    }

    public void Simulate() {
        if (dirty) return;
        if (hasJobHandle) {
            jobHandle.Complete();
            //DrawDebug(jiggleJob);
            PushBack(jiggleJob);
        }
        var sharedMatrices = new NativeArray<Matrix4x4>(bones.Length, Allocator.Persistent);
        bulkRead.matrices = sharedMatrices;
        jiggleJob.transformMatrices = sharedMatrices;
        jiggleJob.timeStamp = Time.timeAsDouble;
        jiggleJob.gravity = Physics.gravity;
        bulkRead.restPoseMatrices.CopyFrom(restPoseTransforms);
        bulkReadHandle = hasPoseHandle ? bulkRead.Schedule(transformAccessArray, poseHandle) : bulkRead.Schedule(transformAccessArray);
        hasBulkReadHandle = true;
        jobHandle = jiggleJob.Schedule(bulkReadHandle);
        hasJobHandle = true;
    }

    public void Pose() {
        if (hasPoseHandle) {
            poseHandle.Complete();
        }
        jigglePoseJob.currentTime = Time.timeAsDouble;
        poseHandle = hasBulkReadHandle ? jigglePoseJob.Schedule(transformAccessArray, bulkReadHandle) : jigglePoseJob.Schedule(transformAccessArray);
        hasPoseHandle = true;
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
    
    private void RecordAllRestPoseTransforms() {
        for (var index = 0; index < bones.Length; index++) {
            bones[index].GetLocalPositionAndRotation(out var localPosition, out var localRotation);
            restPoseTransforms[index] = Matrix4x4.TRS(localPosition, localRotation, Vector3.one);
        }
    }

    public void Dispose() {
        jiggleJob.transformMatrices.Dispose();
        jiggleJob.debug.Dispose();
        jiggleJob.simulatedPoints.Dispose();
        jiggleJob.output.Dispose();
        bulkRead.matrices.Dispose();
        bulkRead.restPoseMatrices.Dispose();
        bulkRead.previousLocalPositions.Dispose();
        bulkRead.previousLocalRotations.Dispose();
        bulkRead.animated.Dispose();
        jigglePoseJob.previousSolve.Dispose();
        jigglePoseJob.currentSolve.Dispose();
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
