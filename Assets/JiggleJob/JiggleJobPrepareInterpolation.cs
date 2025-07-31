using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompiled]
public struct JiggleJobPrepareInterpolation : IJob {
    public double incomingTimeStamp;
    [ReadOnly]
    public NativeArray<float3> outputPositions;
    [ReadOnly]
    public NativeArray<quaternion> outputRotations;
    [ReadOnly]
    public NativeArray<float3> inputPosePositions;
    
    public NativeArray<float3> currentPositions;
    public NativeArray<quaternion> currentRotations;
    
    public NativeReference<double> previousTimeStamp;
    public NativeReference<double> currentTimeStamp;
    public NativeReference<Vector3> previousSimulatedRootOffset;
    public NativeReference<Vector3> currentSimulatedRootOffset;
    public NativeReference<Vector3> previousSimulatedRootPosition;
    public NativeReference<Vector3> currentSimulatedRootPosition;

    public JiggleJobPrepareInterpolation(JiggleJobSimulate jobSimulateSimulate, JiggleJobInterpolation jobInterpolation) {
        incomingTimeStamp = Time.timeAsDouble;
        outputPositions = jobSimulateSimulate.outputPositions;
        outputRotations = jobSimulateSimulate.outputRotations;
        inputPosePositions = jobSimulateSimulate.transformPositions;
        currentPositions = jobInterpolation.currentPositions;
        currentRotations = jobInterpolation.currentRotations;
        previousSimulatedRootOffset = jobInterpolation.previousSimulatedRootOffset;
        currentSimulatedRootOffset = jobInterpolation.currentSimulatedRootOffset;
        previousSimulatedRootPosition = jobInterpolation.previousSimulatedRootPosition;
        currentSimulatedRootPosition = jobInterpolation.currentSimulatedRootPosition;
        previousTimeStamp = jobInterpolation.previousTimeStamp;
        currentTimeStamp = jobInterpolation.timeStamp;
    }

    public void Dispose() {
    }
    
    public void Execute() {
        previousTimeStamp.Value = currentTimeStamp.Value;
        currentTimeStamp.Value = incomingTimeStamp;
        outputPositions.CopyTo(currentPositions);
        outputRotations.CopyTo(currentRotations);
        
        var simulatedPosition = outputPositions[0];
        var pose = inputPosePositions[0];
        previousSimulatedRootOffset.Value = currentSimulatedRootOffset.Value;
        currentSimulatedRootOffset.Value = simulatedPosition - pose;
        
       previousSimulatedRootPosition.Value = currentSimulatedRootPosition.Value;
       currentSimulatedRootPosition.Value = simulatedPosition;
    }
}
