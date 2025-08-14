using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace GatorDragonGames.JigglePhysics {

[BurstCompile]
public struct JiggleJobBulkColliderTransformRead : IJobParallelForTransform {
    public NativeArray<float3> positions;

    public JiggleJobBulkColliderTransformRead(JiggleMemoryBus bus) {
        positions = bus.colliderPositions;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        positions = bus.colliderPositions;
    }

    public void Execute(int index, TransformAccess transform) {
        transform.GetPositionAndRotation(out var position, out var rotation);
        positions[index] = position;
    }
}

}