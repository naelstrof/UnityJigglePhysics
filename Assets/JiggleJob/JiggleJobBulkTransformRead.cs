using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct JiggleJobBulkTransformRead : IJobParallelForTransform {
    public NativeArray<JiggleTransform> simulateInputPoses;
    
    public NativeArray<JiggleTransform> restPoseTransforms;
    
    [ReadOnly] public NativeArray<JiggleTransform> previousLocalTransforms;
    
    public JiggleJobBulkTransformRead(JiggleJobSimulate jobSimulate, JiggleTransform[] localPoses) {
        simulateInputPoses = jobSimulate.inputPoses;
        restPoseTransforms = new NativeArray<JiggleTransform>(localPoses, Allocator.Persistent);
        previousLocalTransforms = new NativeArray<JiggleTransform>(localPoses, Allocator.Persistent);
    }
    
    public void Dispose() {
        if (restPoseTransforms.IsCreated) {
            restPoseTransforms.Dispose();
        }
        if (previousLocalTransforms.IsCreated) {
            previousLocalTransforms.Dispose();
        }
    }

    public void Execute(int index, TransformAccess transform) {
        var jiggleTransform = simulateInputPoses[index];
        if (jiggleTransform.isVirtual) {
            return;
        }
        
        transform.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
        var restTransform = restPoseTransforms[index];
        if (!true) {
            transform.SetLocalPositionAndRotation(restTransform.position, restTransform.rotation);
            return;
        }

        var localTransform = previousLocalTransforms[index];
        if (localPosition == (Vector3)localTransform.position &&
            localRotation == (Quaternion)localTransform.rotation) {
            transform.SetLocalPositionAndRotation(restTransform.position, restTransform.rotation);
        } else {
            restTransform.position = localPosition;
            restTransform.rotation = localRotation;
            restPoseTransforms[index] = restTransform;
        }

        transform.GetPositionAndRotation(out var position, out var rotation);
        jiggleTransform.position = position;
        jiggleTransform.rotation = rotation;
        simulateInputPoses[index] = jiggleTransform;
    }
    
}
