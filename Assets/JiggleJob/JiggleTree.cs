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
    public bool hasJobHandle;
    public JobHandle jobHandle;

    public double previousTimeStamp;
    public Matrix4x4[] previousSolve;
    public double timeStamp;
    public Matrix4x4[] currentSolve;
    public Matrix4x4[] restPoseTransforms;
    public Vector3[] previousLocalPositions;
    public Quaternion[] previousLocalRotations;

    public JiggleTree(Transform[] bones, JiggleBoneSimulatedPoint[] points) {
        var boneCount = bones.Length;
        var pointCount = points.Length;
        this.bones = new Transform[boneCount];
        this.points = new JiggleBoneSimulatedPoint[pointCount];
        previousSolve = new Matrix4x4[boneCount];
        currentSolve = new Matrix4x4[boneCount];
        for (int i = 0; i < pointCount; i++) {
            this.points[i] = points[i];
        }
        for (int i = 0; i < boneCount; i++) {
            this.bones[i] = bones[i];
            previousSolve[i] = bones[i].localToWorldMatrix;
            currentSolve[i] = previousSolve[i];
        }
        timeStamp = Time.timeAsDouble;
        previousTimeStamp = Time.timeAsDouble-JiggleJobManager.FIXED_DELTA_TIME;

        jiggleJob = new JiggleJob() {
            transformMatrices = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
            debug = new NativeArray<Vector3>(pointCount, Allocator.Persistent),
            simulatedPoints = new NativeArray<JiggleBoneSimulatedPoint>(pointCount, Allocator.Persistent),
            output = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
        };
        jiggleJob.simulatedPoints.CopyFrom(points);
        bulkRead = new JiggleBulkTransformRead() {
            matrices = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
        };
        transformAccessArray = new TransformAccessArray(bones);
        previousLocalPositions = new Vector3[boneCount];
        previousLocalRotations = new Quaternion[boneCount];
        restPoseTransforms = new Matrix4x4[boneCount];
        RecordAllRestPoseTransforms();
    }

    private void PushBack(JiggleJob job) {
        previousTimeStamp = timeStamp;
        currentSolve.CopyTo(previousSolve, 0);
        timeStamp = job.timeStamp;
        job.output.CopyTo(currentSolve);
    }

    public void Simulate() {
        if (hasJobHandle) {
            jobHandle.Complete();
            DrawDebug(jiggleJob);
            PushBack(jiggleJob);
        }
        
        ResetUnanimatedTransforms();
        var handle = bulkRead.Schedule(transformAccessArray);
        handle.Complete();
        //var matrices = new Matrix4x4[bones.Length];
        //for (int i = 0; i < bones.Length; i++) {
        //    matrices[i] = bones[i].localToWorldMatrix;
        //}
        jiggleJob.transformMatrices.CopyFrom(bulkRead.matrices);
        jiggleJob.timeStamp = Time.timeAsDouble;
        jiggleJob.gravity = Physics.gravity;
        
        jobHandle = jiggleJob.Schedule();
        hasJobHandle = true;
    }

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
            bones[i].SetPositionAndRotation(position, rotation);
        }
        for (int i = 0; i < boneCount; i++) {
            bones[i].GetLocalPositionAndRotation(out var localPosition, out var localRotation);
            previousLocalPositions[i] = localPosition;
            previousLocalRotations[i] = localRotation;
        }
    }

    private void ResetUnanimatedTransforms() {
        for (var index = 0; index < points.Length; index++) {
            var boneIndex = points[index].transformIndex;
            if (boneIndex == -1) continue;
            bones[boneIndex].GetLocalPositionAndRotation(out var localPosition, out var localRotation);
            if (!points[index].animated) {
                bones[boneIndex].SetLocalPositionAndRotation(restPoseTransforms[boneIndex].GetPosition(), restPoseTransforms[boneIndex].rotation);
                continue;
            }
            if (localPosition == previousLocalPositions[boneIndex] &&
                localRotation == previousLocalRotations[boneIndex]) {
                bones[boneIndex].SetLocalPositionAndRotation(restPoseTransforms[boneIndex].GetPosition(), restPoseTransforms[boneIndex].rotation);
            } else {
                restPoseTransforms[boneIndex] = Matrix4x4.TRS(localPosition, localRotation, Vector3.one);
            }
        }
    }
    
    private void RecordAllRestPoseTransforms() {
        for (var index = 0; index < bones.Length; index++) {
            bones[index].GetLocalPositionAndRotation(out var localPosition, out var localRotation);
            restPoseTransforms[index] = Matrix4x4.TRS(localPosition, localRotation, Vector3.one);
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
