using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct JiggleJobBulkReadRoots : IJobParallelForTransform {
    public NativeArray<float3> outputPositions;

    public JiggleJobBulkReadRoots(int treeCount) {
        outputPositions = new NativeArray<float3>(treeCount, Allocator.Persistent);
    }
    
    public void Dispose() {
        if (outputPositions.IsCreated) {
            outputPositions.Dispose();
        }
    }   

    public void Execute(int index, TransformAccess transform) {
        outputPositions[index] = transform.position;
    }
}
