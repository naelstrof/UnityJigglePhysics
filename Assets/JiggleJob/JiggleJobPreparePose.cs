using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct JiggleJobPreparePose : IJob {
    public NativeArray<JiggleBoneSimulatedPoint> simulatedPoints;
    public NativeArray<Vector3> currentSimulatedRootOffsets;
    public NativeArray<Vector3> previousSimulatedRootOffsets;
    public NativeArray<Vector3> currentSimulatedRootPositions;
    public NativeArray<Vector3> previousSimulatedRootPositions;
    public NativeArray<int> rootPointIndices;
    
    public void Execute() {
        previousSimulatedRootOffsets.CopyFrom(currentSimulatedRootOffsets);
        for (int i = 0; i < simulatedPoints.Length; i++) {
            var point = simulatedPoints[i];
            if (point.transformIndex == -1) {
                continue;
            }
            currentSimulatedRootOffsets[point.transformIndex] = simulatedPoints[rootPointIndices[i]].position - simulatedPoints[rootPointIndices[i]].pose;
        }
        previousSimulatedRootPositions.CopyFrom(currentSimulatedRootPositions);
        for (int i = 0; i < simulatedPoints.Length; i++) {
            var point = simulatedPoints[i];
            if (point.transformIndex == -1) {
                continue;
            }
            currentSimulatedRootPositions[point.transformIndex] = simulatedPoints[rootPointIndices[i]].position;
        }
    }
}
