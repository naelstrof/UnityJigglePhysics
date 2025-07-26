using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

public struct JiggleBulkTransformRead : IJobParallelForTransform {
    public NativeArray<Matrix4x4> matrices;
    public NativeArray<Matrix4x4> restPoseMatrices;
    public NativeArray<Vector3> previousLocalPositions;
    public NativeArray<Quaternion> previousLocalRotations;
    public NativeArray<bool> animated;
    public void Execute(int index, TransformAccess transform) {
        // TODO: Stop going back and forth between matrices and positions/rotations
        transform.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
        if (!true) {
            transform.SetLocalPositionAndRotation(restPoseMatrices[index].GetPosition(), restPoseMatrices[index].rotation);
            return;
        }
        if (localPosition == previousLocalPositions[index] &&
            localRotation == previousLocalRotations[index]) {
            transform.SetLocalPositionAndRotation(restPoseMatrices[index].GetPosition(), restPoseMatrices[index].rotation);
        } else {
            restPoseMatrices[index] = Matrix4x4.TRS(localPosition, localRotation, Vector3.one);
        }
        matrices[index] = transform.localToWorldMatrix;
    }
    
}
