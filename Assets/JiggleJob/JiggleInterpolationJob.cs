using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public struct JiggleInterpolationJob : IJobParallelFor {
    public NativeArray<Matrix4x4> previousSolve;
    public NativeArray<Matrix4x4> currentSolve;
    //public NativeArray<Vector3> previousLocalPositions;
    //public NativeArray<Quaternion> previousLocalRotations;
    public double timeStamp;
    public double previousTimeStamp;
    public double currentTime;
    
    public Vector3 previousSimulatedRootOffset;
    public Vector3 currentSimulatedRootOffset;
    
    public Vector3 previousSimulatedRootPosition;
    public Vector3 currentSimulatedRootPosition;
    
    public Vector3 realRootPosition;
    
    public NativeArray<Vector3> outputInterpolatedPositions;
    public NativeArray<Quaternion> outputInterpolatedRotations;
    
    public void Execute(int index) {
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
        const double timeCorrection = JiggleJobManager.FIXED_DELTA_TIME * 2f;
        var t = (currentTime-timeCorrection - previousTimeStamp) / diff;
        var position = Vector3.LerpUnclamped(prevPosition, newPosition, (float)t);
        var rotation = Quaternion.SlerpUnclamped(prevRotation, newRotation, (float)t);
        
        var simulatedRootPosition = Vector3.LerpUnclamped(previousSimulatedRootPosition, currentSimulatedRootPosition, (float)t);
        var simulatedRootOffset = Vector3.LerpUnclamped(previousSimulatedRootOffset, currentSimulatedRootOffset, (float)t);
        
        var snapToReal = realRootPosition-simulatedRootPosition;
        outputInterpolatedPositions[index] = position + snapToReal + simulatedRootOffset;
        outputInterpolatedRotations[index] = rotation;
    }
}
