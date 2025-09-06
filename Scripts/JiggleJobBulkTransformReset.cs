using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
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

    public bool HasChanged(float3 oldPosition, Vector3 newPosition, quaternion oldRotation, Quaternion newRotation) {
        return newPosition == (Vector3)oldPosition && newRotation == (Quaternion)oldRotation;
    }

    public void Execute(int index, TransformAccess transform) {
        if (!transform.isValid) {
            return;
        }

        transform.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
        var restTransform = restPoseTransforms[index];

        var localTransform = previousLocalTransforms[index];
        if (localTransform.isVirtual) {
            return;
        }
        
        if (HasChanged(localTransform.position, localPosition, localTransform.rotation, localRotation)) {
            transform.SetLocalPositionAndRotation(restTransform.position, restTransform.rotation);
        } else {
            restTransform.position = localPosition;
            restTransform.rotation = localRotation;
            restPoseTransforms[index] = restTransform;
        }
    }

}

}