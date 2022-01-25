using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Uses Verlet to resolve constraints easily 
public class JiggleBone {

    public JiggleBone parent;
    public JiggleBone child;
    public Quaternion boneRotationChangeCheck;
    public Quaternion lastValidPoseBoneRotation;
    public Quaternion cachedBoneRotation;
    public Vector3 targetAnimatedBonePosition;
    public Vector3 position;
    public Transform transform;
    public Vector3 previousPosition;
    public float lengthToParent;
    
    public Vector3 interpolatedPosition {
        get {
            // interpolation, delayed by fixedDeltaTime
            float timeSinceLastUpdate = Time.time-Time.fixedTime;
            return Vector3.Lerp(previousPosition, position, timeSinceLastUpdate/Time.fixedDeltaTime);
        }
    }
    
    public JiggleBone(Transform transform, JiggleBone parent, Vector3 position) {
        this.transform = transform;
        this.parent = parent;
        this.position = position;
        previousPosition = position;
        if (parent == null) {
            lengthToParent = 0;
            return;
        }
        this.parent.child = this;
        lengthToParent = Vector3.Distance(parent.position, position);
    }

    public void Simulate(JiggleSettings jiggleSettings) {
        if (parent == null) {
            SetNewPosition(transform.position);
            return;
        }
        Vector3 newPosition = NextPhysicsPosition(Time.deltaTime, jiggleSettings.gravityMultiplier, jiggleSettings.friction, jiggleSettings.inertness);
        newPosition = ConstrainAngle(newPosition, jiggleSettings.elasticity*jiggleSettings.elasticity);
        newPosition = ConstrainLength(newPosition);
        SetNewPosition(newPosition);
    }

    public void CacheAnimationPosition() {
        // Purely virtual particles need to reconstruct their desired position.
        if (transform == null) {
            // parent.parent is guaranteed to exist here, unless someone's trying to jiggle a single bone entirely by itself (which throws an exception).
            Vector3 projectedForward = (parent.transform.position - parent.parent.transform.position).normalized;
            targetAnimatedBonePosition = parent.transform.position+projectedForward*lengthToParent;
            return;
        }
        targetAnimatedBonePosition = transform.position;
        cachedBoneRotation = transform.rotation;
        lastValidPoseBoneRotation = transform.localRotation;
    }
    
    public Vector3 ConstrainLength(Vector3 newPosition) {
        Vector3 diff = newPosition - parent.position;
        Vector3 dir = diff.normalized;
        return parent.position + dir * lengthToParent;
    }

    public Vector3 ConstrainAngle(Vector3 newPosition, float elasticity) {
        Vector3 parentParentPosition;
        Vector3 poseParentParent;
        if (parent.parent == null) {
            poseParentParent = parent.targetAnimatedBonePosition + (parent.targetAnimatedBonePosition - targetAnimatedBonePosition);
            parentParentPosition = poseParentParent;
        } else {
            parentParentPosition = parent.parent.position;
            poseParentParent = parent.parent.targetAnimatedBonePosition;
        }
        Vector3 parentAimTargetPose = parent.targetAnimatedBonePosition - poseParentParent;
        Vector3 parentAim = parent.position - parentParentPosition;
        Quaternion TargetPoseToPose = Quaternion.FromToRotation(parentAimTargetPose, parentAim);
        Vector3 currentPose = targetAnimatedBonePosition - poseParentParent;
        Vector3 constraintTarget = TargetPoseToPose * currentPose;
        return Vector3.Lerp(newPosition, parentParentPosition + constraintTarget, elasticity);
    }

    public void SetNewPosition(Vector3 newPosition) {
        previousPosition = position;
        position = newPosition;
    }

    public Vector3 NextPhysicsPosition(float deltaTime, float gravityMultiplier, float friction, float inertness) {
        float squaredDeltaTime = deltaTime * deltaTime;
        Vector3 newPosition = position + (position - previousPosition)*(1f-friction) + Physics.gravity * squaredDeltaTime * gravityMultiplier;
        newPosition += (parent.position - parent.previousPosition) * 0.5f * inertness;
        return newPosition;
    }

    public void DebugDraw(Color color, bool interpolated) {
        if (parent == null) return;
        if (interpolated) {
            Debug.DrawLine(interpolatedPosition, parent.interpolatedPosition, color);
        } else {
            Debug.DrawLine(position, parent.position, color);
        }
    }

    public void PrepareBone() {
        // If bone is not animated, return to last unadulterated pose
        if (transform != null && boneRotationChangeCheck == transform.localRotation) {
            transform.localRotation = lastValidPoseBoneRotation;
        }
        CacheAnimationPosition();
    }

    public void PoseBone(float blend) {
        DebugDraw(Color.green, true);
        if (child != null) {
            Vector3 cachedAnimatedVector = child.targetAnimatedBonePosition - targetAnimatedBonePosition;
            Vector3 simulatedVector = child.interpolatedPosition - interpolatedPosition;
            Quaternion animPoseToPhysicsPose = Quaternion.FromToRotation(cachedAnimatedVector, simulatedVector);
            animPoseToPhysicsPose = Quaternion.Lerp(Quaternion.identity, animPoseToPhysicsPose, blend);
            transform.rotation = animPoseToPhysicsPose * cachedBoneRotation;
        }
        if (transform != null) {
            boneRotationChangeCheck = transform.localRotation;
        }
    }
}
