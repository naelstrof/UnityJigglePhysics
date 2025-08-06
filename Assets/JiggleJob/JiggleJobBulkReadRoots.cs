using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct JiggleJobBulkReadRoots : IJobParallelForTransform {
    public NativeList<float3> rootOutputPositions;

    public JiggleJobBulkReadRoots(JiggleMemoryBus bus) {
        rootOutputPositions = bus.rootOutputPositions;
    }

    public void Execute(int index, TransformAccess transform) {
        rootOutputPositions[index] = transform.position;
    }
}
