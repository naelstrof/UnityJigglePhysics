using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

public struct JiggleJobBulkReadRoots : IJobParallelForTransform {
    public NativeArray<Vector3> outputPositions;

    public JiggleJobBulkReadRoots(int treeCount) {
        outputPositions = new NativeArray<Vector3>(treeCount, Allocator.Persistent);
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
