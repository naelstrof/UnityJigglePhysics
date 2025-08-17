using System.Runtime.InteropServices;
using GatorDragonGames.JigglePhysics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public unsafe struct JiggleGridCell {
    public static int2 GetKey(float3 position) {
        return (int2)position.xz;
    }
    public int staleness;
    public int count;
    public int* colliderIndices;
    public JiggleGridCell(int capacity) {
        staleness = 0;
        count = 0;
        colliderIndices = (int*)UnsafeUtility.Malloc(
            Marshal.SizeOf<int>() * capacity,
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

[BurstCompile]
public struct JiggleJobBroadPhaseClear : IJob {
    public NativeHashMap<int2,JiggleGridCell> broadPhaseMap;
    
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
    public NativeHashMap<int2,JiggleGridCell> broadPhaseMap;
    public NativeArray<JiggleCollider> jiggleColliders;
    public int jiggleColliderCount;
    
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
            int2 gridPosition = JiggleGridCell.GetKey(position);
            if (!broadPhaseMap.ContainsKey(gridPosition)) {
                broadPhaseMap.Add(gridPosition, new JiggleGridCell(255));
            }

            var gridCell = broadPhaseMap[gridPosition];
            gridCell.staleness = 0;
            unsafe {
                gridCell.colliderIndices[gridCell.count] = i;
                gridCell.count = math.min(gridCell.count + 1, 255);
            }

            broadPhaseMap[gridPosition] = gridCell;
        }
    }
}
