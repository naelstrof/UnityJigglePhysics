using Unity.Collections;
using Unity.Jobs;
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
        
        var handle = bulkRead.Schedule(transformAccessArray);
        handle.Complete();
        jiggleJob.transformMatrices.CopyFrom(bulkRead.matrices);
        jiggleJob.timeStamp = Time.timeAsDouble;
        jiggleJob.gravity = Physics.gravity;
        
        jobHandle = jiggleJob.Schedule();
        hasJobHandle = true;
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
