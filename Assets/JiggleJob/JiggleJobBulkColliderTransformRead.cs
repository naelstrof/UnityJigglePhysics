using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct JiggleJobBulkColliderTransformRead : IJobParallelForTransform {
public NativeArray<float3> positions;
    
public JiggleJobBulkColliderTransformRead(JiggleJobSimulate jobSimulate, Transform[] colliderTransforms) {
    positions = jobSimulate.testColliders;
}
    
public void Dispose() {
    if (positions.IsCreated) {
        positions.Dispose();
    }
}

public void Execute(int index, TransformAccess transform) {
    transform.GetPositionAndRotation(out var position, out var rotation);
    positions[index] = position;
}
    
}
