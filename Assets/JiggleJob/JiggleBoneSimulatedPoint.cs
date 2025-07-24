using UnityEngine;

public unsafe struct JiggleBoneSimulatedPoint {
    public const int MAX_CHILDREN = 16;
    // Generated at runtime
    public Vector3 lastPosition;
    public Vector3 position;
    public Vector3 workingPosition;
    public Vector3 desiredConstraint;
    public Vector3 parentPose;
    public Vector3 pose;
    public float desiredLengthToParent;
    
    // Set at initialization
    public JiggleBoneParameters parameters;
    public int parentIndex;
    public fixed int childrenIndices[MAX_CHILDREN];
    public int childenCount;
    public int transformIndex;
}
