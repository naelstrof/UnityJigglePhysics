using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct JiggleJobInterpolation : IJobFor {
    [ReadOnly] public NativeArray<float3> realRootPositions;
    
    [ReadOnly] public NativeArray<JiggleTransform> previousPoses;
    [ReadOnly] public NativeArray<JiggleTransform> currentPoses;
    
    public double timeStamp;
    public double previousTimeStamp;
    public double currentTime;
    
    [ReadOnly] public NativeArray<float3> previousSimulatedRootOffset;
    [ReadOnly] public NativeArray<float3> currentSimulatedRootOffset;
    
    [ReadOnly] public NativeArray<float3> previousSimulatedRootPosition;
    [ReadOnly] public NativeArray<float3> currentSimulatedRootPosition;
    
    public NativeArray<JiggleTransform> outputInterpolatedPoses;

    public JiggleJobInterpolation(JiggleMemoryBus bus, double time) {
        timeStamp = time - JiggleJobManager.FIXED_DELTA_TIME;
        previousTimeStamp = timeStamp - JiggleJobManager.FIXED_DELTA_TIME;
        currentTime = timeStamp;
        previousPoses = bus.interpolationPreviousPoses;
        currentPoses = bus.interpolationCurrentPoses;
        outputInterpolatedPoses = bus.interpolationOutputPoses;
        previousSimulatedRootOffset = bus.interpolationPreviousRootOffsets;
        currentSimulatedRootOffset = bus.interpolationCurrentRootOffsets;
        previousSimulatedRootPosition = bus.interpolationPreviousRootPositions;
        currentSimulatedRootPosition = bus.interpolationCurrentRootPositions;
        realRootPositions = bus.rootOutputPositions;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        previousPoses = bus.interpolationPreviousPoses;
        currentPoses = bus.interpolationCurrentPoses;
        outputInterpolatedPoses = bus.interpolationOutputPoses;
        previousSimulatedRootOffset = bus.interpolationPreviousRootOffsets;
        currentSimulatedRootOffset = bus.interpolationCurrentRootOffsets;
        previousSimulatedRootPosition = bus.interpolationPreviousRootPositions;
        currentSimulatedRootPosition = bus.interpolationCurrentRootPositions;
        realRootPositions = bus.rootOutputPositions;
    }
    
    public void Execute(int index) {
        var prevPose = previousPoses[index];
        var newPose = currentPoses[index];

        var diff = timeStamp - previousTimeStamp;
        if (diff == 0) {
            throw new UnityException($"Time difference is zero ({timeStamp}-{previousTimeStamp}), cannot interpolate.");
        }
        const double timeCorrection = JiggleJobManager.FIXED_DELTA_TIME * 2f;
        var t = (currentTime-timeCorrection - previousTimeStamp) / diff;
        var interPose = JiggleTransform.Lerp(prevPose, newPose, (float)t);
        
        var simulatedRootPosition = math.lerp(previousSimulatedRootPosition[index], currentSimulatedRootPosition[index], (float)t);
        var simulatedRootOffset = math.lerp(previousSimulatedRootOffset[index], currentSimulatedRootOffset[index], (float)t);
        
        var snapToReal = realRootPositions[index]-simulatedRootPosition;
        interPose.position += snapToReal + simulatedRootOffset;
        outputInterpolatedPoses[index] = interPose;
    }
}
