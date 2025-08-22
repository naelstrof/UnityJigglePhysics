using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

namespace GatorDragonGames.JigglePhysics {

[BurstCompile]
public struct JiggleJobBulkTransformReset : IJobParallelForTransform {
    [ReadOnly]
    public NativeArray<JiggleTransform> simulateInputPoses;

    public NativeArray<JiggleTransform> restPoseTransforms;

    [ReadOnly] public NativeArray<JiggleTransform> previousLocalTransforms;

    public JiggleJobBulkTransformReset(JiggleMemoryBus bus) {
        simulateInputPoses = bus.simulateInputPoses;
        restPoseTransforms = bus.restPoseTransforms;
        previousLocalTransforms = bus.previousLocalRestPoseTransforms;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        simulateInputPoses = bus.simulateInputPoses;
        restPoseTransforms = bus.restPoseTransforms;
        previousLocalTransforms = bus.previousLocalRestPoseTransforms;
    }

    public void Execute(int index, TransformAccess transform) {
        var jiggleTransform = simulateInputPoses[index];
        if (!transform.isValid || jiggleTransform.isVirtual) {
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