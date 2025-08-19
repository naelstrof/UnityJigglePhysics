using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace GatorDragonGames.JigglePhysics {

[BurstCompile]
public struct JiggleJobBulkColliderTransformRead : IJobParallelForTransform {
    public NativeArray<JiggleCollider> colliders;

    public JiggleJobBulkColliderTransformRead(NativeArray<JiggleCollider> colliders) {
        this.colliders = colliders;
    }

    public void UpdateArrays(NativeArray<JiggleCollider> colliders) {
        this.colliders = colliders;
    }
    
    float AverageScale(float4x4 matrix) {
        float sx = math.length(matrix.c0.xyz);
        float sy = math.length(matrix.c1.xyz);
        float sz = math.length(matrix.c2.xyz);
        return (sx + sy + sz) / 3f;
    }

    public void Execute(int index, TransformAccess transform) {
        var collider = colliders[index];
        if (!transform.isValid || !collider.enabled) {
            return;
        }
        collider.localToWorldMatrix = transform.localToWorldMatrix;
        var averageScale = AverageScale(collider.localToWorldMatrix);
        collider.worldRadius = collider.radius * averageScale;
        colliders[index] = collider;
    }
}

}