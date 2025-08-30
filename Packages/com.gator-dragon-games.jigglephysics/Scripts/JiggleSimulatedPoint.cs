using Unity.Mathematics;

namespace GatorDragonGames.JigglePhysics {

public unsafe struct JiggleSimulatedPoint {
    public const int MAX_CHILDREN = 16;

    // Generated at runtime
    public float3 lastPosition;
    public float3 position;
    public float3 workingPosition;
    public float3 parentPose;
    public float3 pose;
    public float desiredLengthToParent;
    public bool animated;
    public float worldRadius;

    // Set at initialization
    public float distanceFromRoot;
    public int parentIndex;
    public fixed int childrenIndices[MAX_CHILDREN];
    public int childenCount;
    public bool hasTransform;

    public override string ToString() {
        return $"(position: {position},\nlastPosition: {lastPosition},\n" +
               $"workingPosition: {workingPosition},\n" +
               $"parentPose: {parentPose},\npose: {pose},\ndesiredLengthToParent:{desiredLengthToParent},\n" +
               $"animated: {animated},\n parentIndex: {parentIndex},\n " +
               $"children: [{childrenIndices[0]}, ...],\n childenCount: {childenCount},\n hasTransform: {hasTransform})";
    }
}

}
