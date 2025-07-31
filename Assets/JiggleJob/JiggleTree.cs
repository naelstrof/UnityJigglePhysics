using Unity.Collections;
using Unity.Jobs;
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
    
    public JiggleJobSimulate jobSimulate;
    public bool hasHandleSimulate;
    public JobHandle handleSimulate;
    
    public JiggleJobInterpolation jobInterpolation;
    public JobHandle handleInterpolate;
    public bool hasHandleInterpolate;
    
    public JiggleJobTransformWrite jobTransformWrite;
    public JobHandle handleTransformWrite;
    public bool hasHandleTrasnformWrite;

    public bool dirty;

    public JiggleTree(Transform[] bones, JiggleBoneSimulatedPoint[] points) {
        dirty = true;
        var boneCount = bones.Length;
        var pointCount = points.Length;
        this.bones = new Transform[boneCount];
        this.points = new JiggleBoneSimulatedPoint[pointCount];
        for(int i=0; i < boneCount; i++) {
            this.bones[i] = bones[i];
        }
        for(int i=0; i < pointCount; i++) {
            this.points[i] = points[i];
        }

        jobSimulate = new JiggleJobSimulate(bones, points);
        jobBulkRead = new JiggleJobBulkTransformRead(jobSimulate, bones);

        jobInterpolation = new JiggleJobInterpolation(jobSimulate, bones);
        
        jobTransformWrite = new JiggleJobTransformWrite(jobBulkRead, jobInterpolation);
        
        transformAccessArray = new TransformAccessArray(bones);
    }

    private void PushBack() {
        Profiler.BeginSample("JiggleTree.Pushback");
        if (hasHandleSimulate) {
            handleSimulate.Complete();
        }
        if (hasHandleInterpolate) {
            handleInterpolate.Complete();
        }

        // Rotate our three memory buffers
        jobInterpolation.previousTimeStamp = jobInterpolation.timeStamp;
        jobInterpolation.timeStamp = jobSimulate.timeStamp;

        var temp = jobInterpolation.previousPositions;
        jobInterpolation.previousPositions = jobInterpolation.currentPositions;
        jobInterpolation.currentPositions = jobSimulate.outputPositions;
        jobSimulate.outputPositions = temp;
        
        var tempRot = jobInterpolation.previousRotations;
        jobInterpolation.previousRotations = jobInterpolation.currentRotations;
        jobInterpolation.currentRotations = jobSimulate.outputRotations;
        jobSimulate.outputRotations = tempRot;
        
        var tempRootOffset = jobInterpolation.previousSimulatedRootOffset;
        jobInterpolation.previousSimulatedRootOffset = jobInterpolation.currentSimulatedRootOffset;
        jobInterpolation.currentSimulatedRootOffset = jobSimulate.outputSimulatedRootOffset;
        jobSimulate.outputSimulatedRootOffset = tempRootOffset;
        
        var tempRootPosition = jobInterpolation.previousSimulatedRootPosition;
        jobInterpolation.previousSimulatedRootPosition = jobInterpolation.currentSimulatedRootPosition;
        jobInterpolation.currentSimulatedRootPosition = jobSimulate.outputSimulatedRootPosition;
        jobSimulate.outputSimulatedRootPosition = tempRootPosition;
        
        Profiler.EndSample();
    }
    
    public void Simulate(double currentTime) {
        if (dirty) return;
        Profiler.BeginSample("JiggleTree.Simulate");
        if (hasHandleSimulate) {
            PushBack();
        }
        Profiler.BeginSample("JiggleTree.PrepareJobs");
        jobSimulate.timeStamp = currentTime;
        jobSimulate.gravity = Physics.gravity;
        Profiler.EndSample();
        Profiler.BeginSample("JiggleTree.ScheduleJobs");
        handleBulkRead = jobBulkRead.Schedule(transformAccessArray);
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
        handleInterpolate = jobInterpolation.ScheduleParallel(bones.Length, 32, default);
        hasHandleInterpolate = true;

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

    public void Dispose() {
        if (hasHandleBulkRead) {
            handleBulkRead.Complete();
        }
        if (hasHandleSimulate) {
            handleSimulate.Complete();
        }
        if (hasHandleInterpolate) {
            handleInterpolate.Complete();
        }
        if (hasHandleTrasnformWrite) {
            handleTransformWrite.Complete();
        }
        jobSimulate.Dispose();
        jobBulkRead.Dispose();
        jobInterpolation.Dispose();
        jobTransformWrite.Dispose();
        if (transformAccessArray.isCreated) {
            transformAccessArray.Dispose();
        }
    }

    /*private static void DrawDebug(JiggleJob job) {
        for (var index = 0; index < job.simulatedPoints.Length; index++) {
            var simulatedPoint = job.simulatedPoints[index];
            if (simulatedPoint.parentIndex == -1) continue;
            DebugDrawSphere(job.debug[simulatedPoint.parentIndex], 0.05f, Color.cyan, (float)JiggleJobManager.FIXED_DELTA_TIME);
            Debug.DrawLine(job.debug[index], job.debug[simulatedPoint.parentIndex], Color.cyan, (float)JiggleJobManager.FIXED_DELTA_TIME);
        }
    }*/
    
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
