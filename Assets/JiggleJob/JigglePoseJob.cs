using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

public struct JigglePoseJob : IJobParallelForTransform {
    public NativeArray<Matrix4x4> previousSolve;
    public NativeArray<Matrix4x4> currentSolve;
    public NativeArray<Vector3> previousLocalPositions;
    public NativeArray<Quaternion> previousLocalRotations;
    public double timeStamp;
    public double previousTimeStamp;
    public double currentTime;
    public Vector3 lastPositionTimeOffset;
    public Vector3 positionTimeOffset;
    
    public void Execute(int index, TransformAccess transform) {
        var prevPosition = previousSolve[index].GetPosition();
        var prevRotation = previousSolve[index].rotation;

        var newPosition = currentSolve[index].GetPosition();
        var newRotation = currentSolve[index].rotation;

        var diff = timeStamp - previousTimeStamp;
        if (diff == 0) {
            throw new UnityException("Time difference is zero, cannot interpolate.");
        }

        // TODO: Revisit this issue after FEELING the solve in VR in context
        // The issue here is that we are having to operate 3 full frames in the past
        // which might be noticable latency
        var timeCorrection = JiggleJobManager.FIXED_DELTA_TIME * 2f;
        var t = (currentTime-timeCorrection - previousTimeStamp) / diff;
        var position = Vector3.LerpUnclamped(prevPosition, newPosition, (float)t);
        var rotation = Quaternion.SlerpUnclamped(prevRotation, newRotation, (float)t);
        
        var timeOffset = Vector3.LerpUnclamped(lastPositionTimeOffset, positionTimeOffset, (float)t);
        transform.SetPositionAndRotation(position + timeOffset, rotation);
        transform.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
        previousLocalPositions[index] = localPosition;
        previousLocalRotations[index] = localRotation;
    }
}
