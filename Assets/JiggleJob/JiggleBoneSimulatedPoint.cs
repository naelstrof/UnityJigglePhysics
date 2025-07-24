using UnityEngine;

public struct JiggleBoneSimulatedPoint {
    // Generated at runtime
    public Vector3 lastPosition;
    public Vector3 position;
    public Vector3 workingPosition;
    public Vector3 desiredConstraint;
    public Vector3 parentPose;
    public Vector3 pose;
    public float desiredLengthToParent;
    public Quaternion rollingError;
    
    // Set at initialization
    public JiggleBoneParameters parameters;
    public JiggleBoneIntrinsic intrinsic;
    public int transformIndex;
}
