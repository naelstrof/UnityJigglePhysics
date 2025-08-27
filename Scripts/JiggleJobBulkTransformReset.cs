using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

namespace GatorDragonGames.JigglePhysics {

[BurstCompile]
public struct JiggleJobBulkTransformReset : IJobParallelForTransform {
    public NativeArray<JiggleTransform> restPoseTransforms;

    [ReadOnly] public NativeArray<JiggleTransform> previousLocalTransforms;

    public JiggleJobBulkTransformReset(JiggleMemoryBus bus) {
        restPoseTransforms = bus.restPoseTransforms;
        previousLocalTransforms = bus.previousLocalRestPoseTransforms;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        restPoseTransforms = bus.restPoseTransforms;
        previousLocalTransforms = bus.previousLocalRestPoseTransforms;
    }

    public void Execute(int index, TransformAccess transform) {
        if (!transform.isValid) {
            return;
        }

        transform.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
        var restTransform = restPoseTransforms[index];

        var localTransform = previousLocalTransforms[index];
        if (localPosition == (Vector3)localTransform.position &&
            localRotation == (Quaternion)localTransform.rotation) {
            transform.SetLocalPositionAndRotation(restTransform.position, restTransform.rotation);
        } else {
            restTransform.position = localPosition;
            restTransform.rotation = localRotation;
            restPoseTransforms[index] = restTransform;
        }
    }

}

}