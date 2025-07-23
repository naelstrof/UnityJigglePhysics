using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

public struct JiggleBulkTransformRead : IJobParallelForTransform {
    public NativeArray<Matrix4x4> matrices;
    public void Execute(int index, TransformAccess transform) {
        matrices[index] = transform.localToWorldMatrix;
    }
}
