using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct JiggleJobBulkColliderTransformRead : IJobParallelForTransform {
    public NativeList<float3> positions;
    public JiggleJobBulkColliderTransformRead(JiggleMemoryBus bus) {
        positions = bus.colliderPositions;
    }
    public void Execute(int index, TransformAccess transform) {
        transform.GetPositionAndRotation(out var position, out var rotation);
        positions[index] = position;
    }
}
