using System;
using Unity.Mathematics;

namespace GatorDragonGames.JigglePhysics {

public unsafe struct JiggleSimulatedPoint {
    public const int MAX_CHILDREN = 32;

    // Generated at runtime
    public float3 lastPosition;
    public float3 position;
    public float3 workingPosition;
    public float3 parentPose;
    public float3 pose;
    public float desiredLengthToParent;
    public bool animated;
    public float worldRadius;
    //public float3 debug;

    // Set at initialization
    public float distanceFromRoot;
    public int parentIndex;
    public fixed int childrenIndices[MAX_CHILDREN];
    public int childenCount;
    public bool hasTransform;

    private static bool GetIsValid(float3 vector) {
        bool valid = true;
        valid &= !float.IsNaN(vector.x);
        valid &= !float.IsNaN(vector.y);
        valid &= !float.IsNaN(vector.z);
        return valid;
    }
    private static bool GetIsValid(float value) {
        bool valid = true;
        valid &= !float.IsNaN(value);
        return valid;
    }

    public bool GetIsValid(int pointCount, out string failReason) {
        if (!GetIsValid(lastPosition)) {
            failReason = $"lastPosition is invalid: {lastPosition}";
            return false;
        }
        if (!GetIsValid(position)) {
            failReason = $"position is invalid: {position}";
            return false;
        }
        if (!GetIsValid(workingPosition)) {
            failReason = $"workingPosition is invalid: {workingPosition}";
            return false;
        }
        if (!GetIsValid(pose)) {
            failReason = $"pose is invalid: {pose}";
            return false;
        }
        if (!GetIsValid(parentPose)) {
            failReason = $"parentPose is invalid: {parentPose}";
            return false;
        }
        if (!GetIsValid(desiredLengthToParent)) {
            failReason = $"desiredLengthToParent is invalid: {desiredLengthToParent}";
            return false;
        }
        if (!GetIsValid(worldRadius)) {
            failReason = $"worldRadius is invalid: {worldRadius}";
            return false;
        }
        if (!GetIsValid(distanceFromRoot)) {
            failReason = $"worldRadius is invalid: {distanceFromRoot}";
            return false;
        }
        if (childenCount < 0 || childenCount > MAX_CHILDREN) {
            failReason = $"childenCount is invalid: {childenCount}";
            return false;
        }
        if (parentIndex < -1 || parentIndex >= pointCount) {
            failReason = $"parentIndex is invalid: {parentIndex}";
            return false;
        }
        for (int i = 0; i < childenCount; i++) {
            int childIndex = childrenIndices[i];
            if (childIndex < 0 || childIndex >= pointCount) {
                failReason = $"childrenIndices[{i}] is invalid: {childIndex}";
                return false;
            }
        }
        failReason = "All good!";
        return true;
    }

    public override string ToString() {
        return $"(position: {position},\nlastPosition: {lastPosition},\n" +
               $"workingPosition: {workingPosition},\n" +
               $"parentPose: {parentPose},\npose: {pose},\ndesiredLengthToParent:{desiredLengthToParent},\n" +
               $"animated: {animated},\n parentIndex: {parentIndex},\n " +
               $"children: [{childrenIndices[0]}, ...],\n childenCount: {childenCount},\n hasTransform: {hasTransform})";
    }
}

}
