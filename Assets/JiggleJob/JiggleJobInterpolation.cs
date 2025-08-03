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

    public JiggleJobInterpolation(JiggleTransform[] poses, JiggleJobBulkReadRoots jiggleJobBulkReadRoots) {
        previousPoses = new NativeArray<JiggleTransform>(poses, Allocator.Persistent);
        currentPoses = new NativeArray<JiggleTransform>(poses, Allocator.Persistent);
        outputInterpolatedPoses = new NativeArray<JiggleTransform>(poses, Allocator.Persistent);
        previousSimulatedRootOffset = new NativeArray<float3>(poses.Length, Allocator.Persistent);
        currentSimulatedRootOffset = new NativeArray<float3>(poses.Length, Allocator.Persistent);
        var tempPoses = new float3[poses.Length];
        for (var index = 0; index < poses.Length; index++) {
            tempPoses[index] = poses[index].position;
        }
        previousSimulatedRootPosition = new NativeArray<float3>(tempPoses, Allocator.Persistent);
        currentSimulatedRootPosition = new NativeArray<float3>(tempPoses, Allocator.Persistent);
        realRootPositions = jiggleJobBulkReadRoots.outputPositions;
        
        timeStamp = Time.timeAsDouble;
        previousTimeStamp = timeStamp - JiggleJobManager.FIXED_DELTA_TIME;
        currentTime = timeStamp;
    }
    
    public void Dispose() {
        if (previousPoses.IsCreated) {
            previousPoses.Dispose();
        }
        if (currentPoses.IsCreated) {
            currentPoses.Dispose();
        }
        if (outputInterpolatedPoses.IsCreated) {
            outputInterpolatedPoses.Dispose();
        }
    }
    
    public void Execute(int index) {
        var prevPose = previousPoses[index];
        var newPose = currentPoses[index];

        var diff = timeStamp - previousTimeStamp;
        if (diff == 0) {
            throw new UnityException("Time difference is zero, cannot interpolate.");
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
