using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompiled]
public struct JiggleJobTransformWrite : IJobParallelForTransform {
    public NativeArray<Vector3> previousLocalPositions;
    public NativeArray<Quaternion> previousLocalRotations;
    [ReadOnly]
    public NativeArray<float3> outputInterpolatedPositions;
    [ReadOnly]
    public NativeArray<quaternion> outputInterpolatedRotations;

    public JiggleJobTransformWrite(JiggleJobBulkTransformRead jobBulkRead, JiggleJobInterpolation jobInterpolation) {
        previousLocalPositions = jobBulkRead.previousLocalPositions;
        previousLocalRotations = jobBulkRead.previousLocalRotations;
        outputInterpolatedPositions = jobInterpolation.outputInterpolatedPositions;
        outputInterpolatedRotations = jobInterpolation.outputInterpolatedRotations;
    }
    
    public void Dispose() {
    }
    
    public void Execute(int index, TransformAccess transform) {
        transform.SetPositionAndRotation(outputInterpolatedPositions[index], outputInterpolatedRotations[index]);
        transform.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
        previousLocalPositions[index] = localPosition;
        previousLocalRotations[index] = localRotation;
    }
}
