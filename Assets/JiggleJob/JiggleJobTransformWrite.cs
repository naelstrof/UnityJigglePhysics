using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public struct JiggleJobTransformWrite : IJobParallelForTransform {
    public NativeArray<Vector3> previousLocalPositions;
    public NativeArray<Quaternion> previousLocalRotations;
    public NativeArray<Vector3> outputInterpolatedPositions;
    public NativeArray<Quaternion> outputInterpolatedRotations;
    
    public void Execute(int index, TransformAccess transform) {
        transform.SetPositionAndRotation(outputInterpolatedPositions[index], outputInterpolatedRotations[index]);
        transform.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
        previousLocalPositions[index] = localPosition;
        previousLocalRotations[index] = localRotation;
    }
}
