using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {
[BurstCompile]
public unsafe struct JiggleGridCell {
    public static int2 GetKeyForPosition(float3 position) {
        const float gridSizeMeters = 1f;
        return (int2)math.round(position.xz*(1f/gridSizeMeters));
    }

    public int staleness;
    public int count;
    public int* colliderIndices;

    public JiggleGridCell(int capacity) {
        staleness = 0;
        count = 0;
        colliderIndices = (int*)UnsafeUtility.Malloc(
            sizeof(int) * capacity,
            UnsafeUtility.AlignOf<int>(),
            Allocator.Persistent
        );
    }

    public void Dispose() {
        if (colliderIndices != null) {
            UnsafeUtility.Free(colliderIndices, Allocator.Persistent);
            colliderIndices = null;
        }
    }
}

// TODO: I don't actually know what a broadphase is, might need to be labelled something different?
[BurstCompile]
public struct JiggleJobBroadPhaseClear : IJob {
    public NativeHashMap<int2, JiggleGridCell> broadPhaseMap;

    public JiggleJobBroadPhaseClear(JiggleMemoryBus bus) {
        broadPhaseMap = bus.broadPhaseMap;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        broadPhaseMap = bus.broadPhaseMap;
    }

    public void Execute() {
        var keyArray = broadPhaseMap.GetKeyArray(Allocator.Temp);
        var keyLength = keyArray.Length;
        for (int i = 0; i < keyLength; i++) {
            var key = keyArray[i];
            var gridCell = broadPhaseMap[key];
            gridCell.count = 0;
            gridCell.staleness++;
            if (gridCell.staleness > 3) {
                gridCell.Dispose();
                broadPhaseMap.Remove(key);
            } else {
                broadPhaseMap[key] = gridCell;
            }
        }

        keyArray.Dispose();
    }
}

[BurstCompile]
public struct JiggleJobBroadPhase : IJob {
    public NativeHashMap<int2, JiggleGridCell> broadPhaseMap;
    [ReadOnly] public NativeArray<JiggleCollider> jiggleColliders;
    public int jiggleColliderCount;
    public const int MAX_COLLIDERS = 32;

    public JiggleJobBroadPhase(JiggleMemoryBus bus) {
        broadPhaseMap = bus.broadPhaseMap;
        jiggleColliders = bus.sceneColliders;
        jiggleColliderCount = bus.sceneColliderCount;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        broadPhaseMap = bus.broadPhaseMap;
        jiggleColliders = bus.sceneColliders;
        jiggleColliderCount = bus.sceneColliderCount;
    }

    public void Execute() {
        for (int i = 0; i < jiggleColliderCount; i++) {
            var collider = jiggleColliders[i];
            float3 position = collider.localToWorldMatrix.c3.xyz;
            int2 min = JiggleGridCell.GetKeyForPosition(position-new float3(collider.worldRadius));
            int2 max = JiggleGridCell.GetKeyForPosition(position+new float3(collider.worldRadius));
            for (int x = min.x; x <= max.x; x++) {
                for (int y = min.y; y <= max.y; y++) {
                    int2 grid = new int2(x, y);
                    if (!broadPhaseMap.ContainsKey(grid)) {
                        broadPhaseMap.Add(grid, new JiggleGridCell(MAX_COLLIDERS));
                    }

                    if (broadPhaseMap.TryGetValue(grid, out JiggleGridCell gridCell)) {
                        gridCell.staleness = 0;
                        unsafe {
                            gridCell.colliderIndices[gridCell.count] = i;
                            gridCell.count = math.min(gridCell.count + 1, MAX_COLLIDERS-1);
                        }
                        broadPhaseMap[grid] = gridCell;
                    }

                }
            }

        }
    }
}

}