using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct JiggleJobTransformWrite : IJobParallelForTransform {
    public NativeList<JiggleTransform> previousLocalPoses;
    [ReadOnly] public NativeList<JiggleTransform> inputInterpolatedPoses;
    public JiggleJobTransformWrite(JiggleMemoryBus bus) {
        previousLocalPoses = bus.previousLocalRestPoseTransforms;
        inputInterpolatedPoses = bus.interpolationOutputPoses;
    }
    
    public void Execute(int index, TransformAccess transform) {
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
