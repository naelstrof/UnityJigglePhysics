using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace GatorDragonGames.JigglePhysics {

[BurstCompile]
public struct JiggleJobBulkTransformRead : IJobParallelForTransform {
    public NativeArray<JiggleTransform> simulateInputPoses;

    public JiggleJobBulkTransformRead(JiggleMemoryBus bus) {
        simulateInputPoses = bus.simulateInputPoses;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        simulateInputPoses = bus.simulateInputPoses;
    }

    public void Execute(int index, TransformAccess transform) {
        var jiggleTransform = simulateInputPoses[index];
        if (!transform.isValid || jiggleTransform.isVirtual) {
            return;
        }
        transform.GetPositionAndRotation(out var position, out var rotation);
        jiggleTransform.position = position;
        jiggleTransform.rotation = rotation;
        jiggleTransform.scale = transform.localToWorldMatrix.lossyScale;
        simulateInputPoses[index] = jiggleTransform;
    }

}

}