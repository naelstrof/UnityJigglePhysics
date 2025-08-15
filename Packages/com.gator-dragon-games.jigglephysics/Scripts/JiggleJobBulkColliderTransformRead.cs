using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace GatorDragonGames.JigglePhysics {

[BurstCompile]
public struct JiggleJobBulkColliderTransformRead : IJobParallelForTransform {
    public NativeArray<JiggleCollider> colliders;

    public JiggleJobBulkColliderTransformRead(JiggleMemoryBus bus) {
        colliders = bus.colliders;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        colliders = bus.colliders;
    }

    public void Execute(int index, TransformAccess transform) {
        var collider = colliders[index];
        collider.localToWorldMatrix = transform.localToWorldMatrix;
        colliders[index] = collider;
    }
}

}