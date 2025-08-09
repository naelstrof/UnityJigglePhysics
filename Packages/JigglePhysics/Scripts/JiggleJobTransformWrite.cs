using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct JiggleJobTransformWrite : IJobParallelForTransform {
    public NativeArray<JiggleTransform> previousLocalPoses;
    [ReadOnly] public NativeArray<JiggleTransform> inputInterpolatedPoses;
    public JiggleJobTransformWrite(JiggleMemoryBus bus) {
        previousLocalPoses = bus.previousLocalRestPoseTransforms;
        inputInterpolatedPoses = bus.interpolationOutputPoses;
    }
    
    public void UpdateArrays(JiggleMemoryBus bus) {
        previousLocalPoses = bus.previousLocalRestPoseTransforms;
        inputInterpolatedPoses = bus.interpolationOutputPoses;
    }
    
    public void Execute(int index, TransformAccess transform) {
        if (!transform.isValid) {
            return;
        }
        var pose = inputInterpolatedPoses[index];
        if (pose.isVirtual) {
            return;
        }
        
        transform.SetPositionAndRotation(pose.position, pose.rotation);
        transform.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
        
        var previousLocalPose = previousLocalPoses[index];
        previousLocalPose.position = localPosition;
        previousLocalPose.rotation = localRotation;
        previousLocalPoses[index] = previousLocalPose;
    }
}
