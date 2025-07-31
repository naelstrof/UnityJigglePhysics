using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompiled]
public struct JiggleJobPrepareInterpolation : IJob {
    public double incomingTimeStamp;
    [ReadOnly]
    public NativeArray<Matrix4x4> outputPoses;
    [ReadOnly]
    public NativeArray<Matrix4x4> inputPoses;
    public NativeArray<Matrix4x4> currentSolve;
    public NativeReference<double> previousTimeStamp;
    public NativeReference<double> currentTimeStamp;
    public NativeReference<Vector3> previousSimulatedRootOffset;
    public NativeReference<Vector3> currentSimulatedRootOffset;
    public NativeReference<Vector3> previousSimulatedRootPosition;
    public NativeReference<Vector3> currentSimulatedRootPosition;
    
    public void Execute() {
        previousTimeStamp.Value = currentTimeStamp.Value;
        currentTimeStamp.Value = incomingTimeStamp;
        outputPoses.CopyTo(currentSolve);
        
        var simulatedPosition = outputPoses[0].GetPosition();
        var pose = inputPoses[0].GetPosition();
        previousSimulatedRootOffset.Value = currentSimulatedRootOffset.Value;
        currentSimulatedRootOffset.Value = simulatedPosition - pose;
        
       previousSimulatedRootPosition.Value = currentSimulatedRootPosition.Value;
       currentSimulatedRootPosition.Value = simulatedPosition;
    }
}
