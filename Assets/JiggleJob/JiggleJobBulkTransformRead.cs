using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompiled]
public struct JiggleJobBulkTransformRead : IJobParallelForTransform {
    public NativeArray<float3> transformPositions;
    public NativeArray<quaternion> transformRotations;

    public NativeArray<Vector3> restPosePositions;
    public NativeArray<Quaternion> restPoseRotations;
    
    [ReadOnly]
    public NativeArray<Vector3> previousLocalPositions;
    [ReadOnly]
    public NativeArray<Quaternion> previousLocalRotations;

    public JiggleJobBulkTransformRead(JiggleJobSimulate jobSimulate, Transform[] bones) {
        var boneCount = bones.Length;
        transformPositions = jobSimulate.transformPositions;
        transformRotations = jobSimulate.transformRotations;
        Vector3[] restPosePositionsArray = new Vector3[boneCount];
        Quaternion[] restPoseRotationsArray = new Quaternion[boneCount];
        
        for (var index = 0; index < boneCount; index++) {
            bones[index].GetLocalPositionAndRotation(out var localPosition, out var localRotation);
            restPosePositionsArray[index] = localPosition;
            restPoseRotationsArray[index] = localRotation;
        }
        
        restPosePositions = new NativeArray<Vector3>(restPosePositionsArray, Allocator.Persistent);
        restPoseRotations = new NativeArray<Quaternion>(restPoseRotationsArray, Allocator.Persistent);
        previousLocalPositions = new NativeArray<Vector3>(boneCount, Allocator.Persistent);
        previousLocalRotations = new NativeArray<Quaternion>(boneCount, Allocator.Persistent);
    }
    public void Dispose() {
        if (restPosePositions.IsCreated) {
            restPosePositions.Dispose();
        }
        if (restPoseRotations.IsCreated) {
            restPoseRotations.Dispose();
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
            transform.SetLocalPositionAndRotation(restPosePositions[index], restPoseRotations[index]);
            return;
        }
        if (localPosition == previousLocalPositions[index] &&
            localRotation == previousLocalRotations[index]) {
            transform.SetLocalPositionAndRotation(restPosePositions[index], restPoseRotations[index]);
        } else {
            restPosePositions[index] = localPosition;
            restPoseRotations[index] = localRotation;
        }
        transform.GetPositionAndRotation(out var position, out var rotation);
        transformPositions[index] = position;
        transformRotations[index] = rotation;
    }
    
}

public class BurstCompiledAttribute : Attribute {
}
