using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct JiggleJobInterpolation : IJobFor {
    [ReadOnly] public NativeArray<float3> realRootPositions;
    
    [ReadOnly] public NativeArray<PoseData> previousPoses;
    [ReadOnly] public NativeArray<PoseData> currentPoses;
    
    public double timeStamp;
    public double previousTimeStamp;
    public double currentTime;
    
    public NativeArray<JiggleTransform> outputInterpolatedPoses;

    public JiggleJobInterpolation(JiggleMemoryBus bus, double time) {
        timeStamp = time - JiggleJobManager.FIXED_DELTA_TIME;
        previousTimeStamp = timeStamp - JiggleJobManager.FIXED_DELTA_TIME;
        currentTime = timeStamp;
        previousPoses = bus.interpolationPreviousPoseData;
        currentPoses = bus.interpolationCurrentPoseData;
        outputInterpolatedPoses = bus.interpolationOutputPoses;
        realRootPositions = bus.rootOutputPositions;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        previousPoses = bus.interpolationPreviousPoseData;
        currentPoses = bus.interpolationCurrentPoseData;
        outputInterpolatedPoses = bus.interpolationOutputPoses;
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
        var interPose = PoseData.Lerp(prevPose, newPose, (float)t);
        
        var snapToReal = realRootPositions[index]-interPose.rootPosition;
        interPose.pose.position += snapToReal + interPose.rootOffset;
        outputInterpolatedPoses[index] = interPose.pose;
    }
}
