using Unity.Mathematics;
using UnityEngine;

public unsafe struct JiggleBoneSimulatedPoint {
    public const int MAX_CHILDREN = 16;
    // Generated at runtime
    public float3 lastPosition;
    public float3 position;
    public float3 workingPosition;
    public float3 desiredConstraint;
    public float3 parentPose;
    public float3 pose;
    public float desiredLengthToParent;
    public bool animated;

    // Set at initialization
    public JiggleBoneParameters parameters;
    public int parentIndex;
    public fixed int childrenIndices[MAX_CHILDREN];
    public int childenCount;
    public int transformIndex;
    public override string ToString() {
        return $"Parent: {parentIndex}, ChildrenCount: {childenCount}, TransformIndex:{transformIndex}, FirstChild: {childrenIndices[0]}";
    }
}
