using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Uses Verlet to resolve constraints easily 
public class JiggleBone {
    public JiggleBone parent;
    public JiggleBone child;
    public Quaternion boneRotationChangeCheck;
    public Quaternion lastValidPoseBoneRotation;
    private Vector3 lastValidPoseBoneLocalPosition;
    public Vector3 targetAnimatedBonePosition;
    public Vector3 position;
    public Transform transform;
    public Vector3 previousPosition;
    public float lengthToParent;
    
    public Vector3 interpolatedPosition {
        get {
            // extrapolation, because interpolation is delayed by fixedDeltaTime
            float timeSinceLastUpdate = Time.time-Time.fixedTime;
            return Vector3.Lerp(position, position+(position-previousPosition), timeSinceLastUpdate/Time.fixedDeltaTime);

            // Interpolation
            //return Vector3.Lerp(previousPosition, position, timeSinceLastUpdate/Time.fixedDeltaTime);
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

    public void Simulate(JiggleSettings jiggleSettings, JiggleBone root) {
        if (parent == null) {
            SetNewPosition(transform.position);
            return;
        }
        Vector3 newPosition = JiggleBone.NextPhysicsPosition(position, previousPosition, (root.position-root.previousPosition),Time.deltaTime, jiggleSettings.gravityMultiplier, jiggleSettings.friction, jiggleSettings.airFriction);
        newPosition = ConstrainInertia(newPosition, jiggleSettings.inertness);
        newPosition = ConstrainAngle(newPosition, jiggleSettings.angleElasticity*jiggleSettings.angleElasticity);
        newPosition = ConstrainLength(newPosition, jiggleSettings.lengthElasticity*jiggleSettings.lengthElasticity);
        SetNewPosition(newPosition);
    }

    public void CacheAnimationPosition() {
        // Purely virtual particles need to reconstruct their desired position.
        if (transform == null) {
            // parent.parent is guaranteed to exist here, unless someone's trying to jiggle a single bone entirely by itself (which throws an exception).
            Vector3 projectedForward = (parent.transform.position - parent.parent.transform.position).normalized;
            targetAnimatedBonePosition = parent.transform.TransformPoint(parent.parent.transform.InverseTransformPoint(parent.transform.position));
            return;
        }
        targetAnimatedBonePosition = transform.position;
        lastValidPoseBoneRotation = transform.localRotation;
        lastValidPoseBoneLocalPosition = transform.localPosition;
    }
    
    public Vector3 ConstrainLength(Vector3 newPosition, float elasticity) {
        Vector3 diff = newPosition - parent.position;
        Vector3 dir = diff.normalized;
        return Vector3.Lerp(newPosition, parent.position + dir * lengthToParent, elasticity);
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

    public static Vector3 NextPhysicsPosition(Vector3 newPosition, Vector3 previousPosition, Vector3 parentVel, float deltaTime, float gravityMultiplier, float friction, float airFriction) {
        float squaredDeltaTime = deltaTime * deltaTime;
        Vector3 vel = (newPosition - previousPosition) - parentVel*(1f-airFriction);
        return newPosition + vel*(1f-friction) + parentVel*(1f-airFriction) + Physics.gravity * squaredDeltaTime * gravityMultiplier;
    }
    public Vector3 ConstrainInertia(Vector3 newPosition, float inertness) {
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
            transform.localPosition = lastValidPoseBoneLocalPosition;
        }
        CacheAnimationPosition();
    }

    public void PoseBone(float blend) {
        if (child != null) {
            float cachedDistance = Vector3.Distance(transform.position, interpolatedPosition);

            Vector3 interpolatedPositionBlend = Vector3.Lerp(targetAnimatedBonePosition, interpolatedPosition, blend);
            Vector3 interpolatedChildPositionBlend = Vector3.Lerp(child.targetAnimatedBonePosition, child.interpolatedPosition, blend);

            if (parent != null) {
                transform.position = interpolatedPositionBlend;
            }
            Vector3 childPosition;
            if (child.transform == null) {
                childPosition = transform.TransformPoint(parent.transform.InverseTransformPoint(transform.position));
            } else {
                childPosition = child.transform.position;
            }
            Vector3 cachedAnimatedVector = childPosition - transform.position;
            Vector3 simulatedVector = interpolatedChildPositionBlend - interpolatedPositionBlend;
            Quaternion animPoseToPhysicsPose = Quaternion.FromToRotation(cachedAnimatedVector, simulatedVector);
            transform.rotation = animPoseToPhysicsPose * transform.rotation;
        }
        if (transform != null) {
            boneRotationChangeCheck = transform.localRotation;
        }
    }
}
