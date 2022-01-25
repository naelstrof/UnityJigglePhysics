using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatedPoint {

    public SimulatedPoint parent;
    public SimulatedPoint child;
    public Quaternion cachedLocalBoneRotation;
    public Quaternion cachedInitialLocalBoneRotation;
    public Quaternion cachedBoneRotation;
    public Vector3 cachedAnimatedPosition;
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
    
    public SimulatedPoint(Transform transform, SimulatedPoint parent, Vector3 position) {
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

    public void CacheAnimationPosition() {
        cachedAnimatedPosition = transform.position;
        cachedBoneRotation = transform.rotation;
        cachedInitialLocalBoneRotation = transform.localRotation;
    }
    
    public void SnapTo(Transform t) {
        previousPosition = position;
        position = t.position;
    }
    
    public void ConstrainLength() {
        Vector3 diff = position - parent.position;
        Vector3 dir = diff.normalized;
        position = parent.position + dir * lengthToParent;
    }

    public void ConstrainAngle() {
        Vector3 parentParentPosition;
        Vector3 poseParentParent;
        if (parent.parent == null) {
            poseParentParent = parent.cachedAnimatedPosition + (parent.cachedAnimatedPosition - cachedAnimatedPosition);
            parentParentPosition = poseParentParent;
        } else {
            parentParentPosition = parent.parent.position;
            poseParentParent = parent.parent.cachedAnimatedPosition;
        }
        Vector3 parentAimTargetPose = parent.cachedAnimatedPosition - poseParentParent;
        Vector3 parentAim = parent.position - parentParentPosition;
        Quaternion TargetPoseToPose = Quaternion.FromToRotation(parentAimTargetPose, parentAim);
        Vector3 currentPose = cachedAnimatedPosition - poseParentParent;
        Vector3 constraintTarget = TargetPoseToPose * currentPose;
        position = Vector3.Lerp(position, parentParentPosition + constraintTarget, 0.1f);
    }

    public void StepPhysics(float deltaTime, float gravityMultiplier) {
        float squaredDeltaTime = deltaTime * deltaTime;
        Vector3 newPosition = position + (position - previousPosition) + Physics.gravity * squaredDeltaTime * gravityMultiplier;
        previousPosition = position;
        position = newPosition;
    }

    public void DebugDraw(Color color, bool interpolated) {
        if (parent == null) return;
        if (interpolated) {
            Debug.DrawLine(interpolatedPosition, parent.interpolatedPosition, color);
        } else {
            Debug.DrawLine(position, parent.position, color);
        }
    }
    
}
