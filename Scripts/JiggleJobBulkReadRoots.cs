using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace GatorDragonGames.JigglePhysics {

[BurstCompile]
public struct JiggleJobBulkReadRoots : IJobParallelForTransform {
    public NativeArray<float3> rootOutputPositions;

    public JiggleJobBulkReadRoots(JiggleMemoryBus bus) {
        rootOutputPositions = bus.rootOutputPositions;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        rootOutputPositions = bus.rootOutputPositions;
    }
    private static float3 SanitizeVector(Vector3 position) {
        float3 pos = position;
        if (float.IsNaN(pos.x)) {
            pos.x = 0f;
        }
        if (float.IsNaN(pos.y)) {
            pos.y = 0f;
        }
        if (float.IsNaN(pos.z)) {
            pos.z = 0f;
        }
        return pos;
    }
    public void Execute(int index, TransformAccess transform) {
        if (!transform.isValid) {
            return;
        }

        rootOutputPositions[index] = SanitizeVector(transform.position);
    }
}

}