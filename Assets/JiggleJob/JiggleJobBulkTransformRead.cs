using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompiled]
public struct JiggleJobBulkTransformRead : IJobParallelForTransform {
    public NativeArray<Vector3> transformPositions;
    public NativeArray<Quaternion> transformRotations;
    
    public NativeArray<Matrix4x4> restPoseMatrices;
    [ReadOnly]
    public NativeArray<Vector3> previousLocalPositions;
    [ReadOnly]
    public NativeArray<Quaternion> previousLocalRotations;

    public JiggleJobBulkTransformRead(JiggleJobSimulate jobSimulate, Transform[] bones) {
        var boneCount = bones.Length;
        transformPositions = jobSimulate.transformPositions;
        transformRotations = jobSimulate.transformRotations;
        Matrix4x4[] restPoseMatricesArray = new Matrix4x4[boneCount];
        for (var index = 0; index < boneCount; index++) {
            bones[index].GetLocalPositionAndRotation(out var localPosition, out var localRotation);
            restPoseMatricesArray[index] = Matrix4x4.TRS(localPosition, localRotation, Vector3.one);
        }
        
        restPoseMatrices = new NativeArray<Matrix4x4>(restPoseMatricesArray, Allocator.Persistent);
        previousLocalPositions = new NativeArray<Vector3>(boneCount, Allocator.Persistent);
        previousLocalRotations = new NativeArray<Quaternion>(boneCount, Allocator.Persistent);
    }
    public void Dispose() {
        if (restPoseMatrices.IsCreated) {
            restPoseMatrices.Dispose();
        }
        if (previousLocalPositions.IsCreated) {
            previousLocalPositions.Dispose();
        }
        if (previousLocalRotations.IsCreated) {
            previousLocalRotations.Dispose();
        }
    }
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
        transform.GetPositionAndRotation(out var position, out var rotation);
        transformPositions[index] = position;
        transformRotations[index] = rotation;
    }
    
}

public class BurstCompiledAttribute : Attribute {
}
