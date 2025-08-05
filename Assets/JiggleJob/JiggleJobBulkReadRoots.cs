using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct JiggleJobBulkReadRoots : IJobParallelForTransform {
    public NativeArray<float3> outputPositions;

    public JiggleJobBulkReadRoots(Transform[] rootBones) {
        var tempPoses = new float3[rootBones.Length];
        for (var index = 0; index < rootBones.Length; index++) {
            tempPoses[index] = rootBones[index].position;
        }
        outputPositions = new NativeArray<float3>(tempPoses, Allocator.Persistent);
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
