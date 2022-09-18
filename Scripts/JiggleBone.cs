using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

// Uses Verlet to resolve constraints easily 
public class JiggleBone {
    private struct PositionFrame {
        public Vector3 position;
        public double time;
        public PositionFrame(Vector3 position, double time) {
            this.position = position;
            this.time = time;
        }
    }
    
    private PositionFrame currentTargetAnimatedBoneFrame;
    private PositionFrame lastTargetAnimatedBoneFrame;
    private PositionFrame poppableBoneFrame;
    private Vector3 currentFixedAnimatedBonePosition;

    public JiggleBone parent;
    public JiggleBone child;
    private Quaternion boneRotationChangeCheck;
    private Vector3 bonePositionChangeCheck;
    public Quaternion lastValidPoseBoneRotation;
    private Vector3 lastValidPoseBoneLocalPosition;
    //public Vector3 targetAnimatedBonePosition;


    public Transform transform;

    private float updateTime;
    private float previousUpdateTime;
    
    public Vector3 position;
    public Vector3 previousPosition;

    private Vector3 extrapolatedPosition;

    // Optimized out, faster to cache once during PrepareBone and reuse.
    /*public Vector3 interpolatedPosition {
        get {
            // extrapolation, because interpolation is delayed by fixedDeltaTime
            float timeSinceLastUpdate = Time.time-Time.fixedTime;
            return Vector3.Lerp(position, position+(position-previousPosition), timeSinceLastUpdate/Time.fixedDeltaTime);

            // Interpolation
            //return Vector3.Lerp(previousPosition, position, timeSinceLastUpdate/Time.fixedDeltaTime);
        }
    }*/
    
    private float GetLengthToParent() {
        if (parent == null) {
            return 0.1f;
        }
        return Vector3.Distance(currentFixedAnimatedBonePosition, parent.currentFixedAnimatedBonePosition);
    }
    
    private Vector3 GetTargetBonePosition(PositionFrame prev, PositionFrame next, double time) {
        double diff = next.time - prev.time;
        if (diff == 0) {
            return next.position;
        }
        double t = (time - prev.time) / diff;
        return Vector3.Lerp(prev.position, next.position, (float)t);
    }
    
    public JiggleBone(Transform transform, JiggleBone parent, Vector3 position) {
        this.transform = transform;
        this.parent = parent;
        this.position = position;
        previousPosition = position;
        if (transform != null) {
            lastValidPoseBoneRotation = transform.localRotation;
            lastValidPoseBoneLocalPosition = transform.localPosition;
        }

        updateTime = Time.time;
        previousUpdateTime = Time.time;
        lastTargetAnimatedBoneFrame = new PositionFrame(position, Time.time);
        currentTargetAnimatedBoneFrame = lastTargetAnimatedBoneFrame;

        if (parent == null) {
            return;
        }

        //previousLocalPosition = parent.transform.InverseTransformPoint(previousPosition);
        this.parent.child = this;
    }

    public void Simulate(JiggleSettingsBase jiggleSettings, Vector3 wind, float time) {
        currentFixedAnimatedBonePosition = GetTargetBonePosition(lastTargetAnimatedBoneFrame, currentTargetAnimatedBoneFrame, time);
        
        if (parent == null) {
            SetNewPosition(currentFixedAnimatedBonePosition, time);
            return;
        }
        Vector3 localSpaceVelocity = (position-previousPosition) - (parent.position-parent.previousPosition);
        //Debug.DrawLine(position, position + localSpaceVelocity, Color.cyan);
        Vector3 newPosition = JiggleBone.NextPhysicsPosition(
            position, previousPosition, localSpaceVelocity, Time.fixedDeltaTime,
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Gravity),
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Friction),
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.AirFriction)
        );
        newPosition += wind * (Time.fixedDeltaTime * jiggleSettings.GetParameter(JiggleSettingsBase.JiggleSettingParameter.AirFriction));
        newPosition = ConstrainAngle(newPosition, jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.AngleElasticity)*jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.AngleElasticity), jiggleSettings.GetParameter(JiggleSettingsBase.JiggleSettingParameter.ElasticitySoften));
        newPosition = ConstrainLength(newPosition, jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.LengthElasticity)*jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.LengthElasticity));
        SetNewPosition(newPosition, time);
    }

    public void PopAnimationPosition() {
        currentTargetAnimatedBoneFrame = lastTargetAnimatedBoneFrame;
        lastTargetAnimatedBoneFrame = poppableBoneFrame;
    }

    public void CacheAnimationPosition() {
        // Purely virtual particles need to reconstruct their desired position.
        poppableBoneFrame = lastTargetAnimatedBoneFrame;
        lastTargetAnimatedBoneFrame = currentTargetAnimatedBoneFrame;
        if (transform == null) {
            Vector3 parentTransformPosition = parent.transform.position;
            if (parent.parent != null) {
                //Vector3 projectedForward = (parentTransformPosition - parent.parent.transform.position).normalized;
                Vector3 pos = parent.transform.TransformPoint( parent.parent.transform.InverseTransformPoint(parentTransformPosition));
                currentTargetAnimatedBoneFrame = new PositionFrame(pos, Time.timeAsDouble);
            } else {
                // parent.transform.parent is guaranteed to exist here, unless the user is jiggling a single bone by itself (which throws an exception).
                //Vector3 projectedForward = (parentTransformPosition - parent.transform.parent.position).normalized;
                Vector3 pos = parent.transform.TransformPoint(parent.transform.parent.InverseTransformPoint(parentTransformPosition));
                currentTargetAnimatedBoneFrame = new PositionFrame(pos, Time.timeAsDouble);
            }
            return;
        }
        currentTargetAnimatedBoneFrame = new PositionFrame(transform.position, Time.timeAsDouble);
        lastValidPoseBoneRotation = transform.localRotation;
        lastValidPoseBoneLocalPosition = transform.localPosition;
    }
    
    public Vector3 ConstrainLength(Vector3 newPosition, float elasticity) {
        Vector3 diff = newPosition - parent.position;
        Vector3 dir = diff.normalized;
        return Vector3.Lerp(newPosition, parent.position + dir * GetLengthToParent(), elasticity);
    }

    public Vector3 ConstrainAngle(Vector3 newPosition, float elasticity, float elasticitySoften) {
        Vector3 parentParentPosition;
        Vector3 poseParentParent;
        if (parent.parent == null) {
            poseParentParent = parent.currentFixedAnimatedBonePosition + (parent.currentFixedAnimatedBonePosition - currentFixedAnimatedBonePosition);
            parentParentPosition = poseParentParent;
        } else {
            parentParentPosition = parent.parent.position;
            poseParentParent = parent.parent.currentFixedAnimatedBonePosition;
        }
        Vector3 parentAimTargetPose = parent.currentFixedAnimatedBonePosition - poseParentParent;
        Vector3 parentAim = parent.position - parentParentPosition;
        Quaternion TargetPoseToPose = Quaternion.FromToRotation(parentAimTargetPose, parentAim);
        Vector3 currentPose = currentFixedAnimatedBonePosition - poseParentParent;
        Vector3 constraintTarget = TargetPoseToPose * currentPose;
        float error = Vector3.Distance(newPosition, parentParentPosition + constraintTarget);
        error /= GetLengthToParent();
        error = Mathf.Clamp01(error);
        error = Mathf.Pow(error, elasticitySoften * 2f);
        return Vector3.Lerp(newPosition, parentParentPosition + constraintTarget, elasticity * error);
    }

    public void SetNewPosition(Vector3 newPosition, float time) {
        previousUpdateTime = updateTime;
        previousPosition = position;
        //if (parent!=null) previousLocalPosition = parent.transform.InverseTransformPoint(previousPosition);
        updateTime = time;
        position = newPosition;
    }

    public static Vector3 NextPhysicsPosition(Vector3 newPosition, Vector3 previousPosition, Vector3 localSpaceVelocity, float deltaTime, float gravityMultiplier, float friction, float airFriction) {
        float squaredDeltaTime = deltaTime * deltaTime;
        Vector3 vel = newPosition - previousPosition - localSpaceVelocity;
        return newPosition + vel * (1f - airFriction) + localSpaceVelocity * (1f - friction) + Physics.gravity * (gravityMultiplier * squaredDeltaTime);
    }

    public void DebugDraw(Color simulateColor, Color targetColor, bool interpolated) {
        if (parent == null) return;
        if (interpolated) {
            Debug.DrawLine(extrapolatedPosition, parent.extrapolatedPosition, simulateColor, 0, false);
        } else {
            Debug.DrawLine(position, parent.position, simulateColor, 0, false);
        }

        //Debug.DrawLine(currentFixedAnimatedBonePosition, parent.currentFixedAnimatedBonePosition, targetColor, 0, false);
    }
    public void DeriveFinalSolvePosition() {
        float t = (Time.time - previousUpdateTime) / Time.fixedDeltaTime;
        //Debug.DrawLine(Vector3.right * Mathf.LerpUnclamped(previousUpdateTime, updateTime, t)*5f,
            //Vector3.right * Mathf.LerpUnclamped(previousUpdateTime, updateTime, t)*5f + Vector3.up*4f, Color.yellow, 100f);
        extrapolatedPosition = Vector3.LerpUnclamped(previousPosition, position, t);
    }

    public void PrepareBone() {
        // If bone is not animated, return to last unadulterated pose
        if (transform != null) {
            if (boneRotationChangeCheck == transform.localRotation) {
                transform.localRotation = lastValidPoseBoneRotation;
            }
            if (bonePositionChangeCheck == transform.localPosition) {
                transform.localPosition = lastValidPoseBoneLocalPosition;
            }
        }
        CacheAnimationPosition();
    }

    public void PoseBone(float blend) {
        if (child != null) {
            Vector3 positionBlend = Vector3.Lerp(currentTargetAnimatedBoneFrame.position, extrapolatedPosition, blend);
            Vector3 childPositionBlend = Vector3.Lerp(child.currentTargetAnimatedBoneFrame.position, child.extrapolatedPosition, blend);

            if (parent != null) {
                transform.position = positionBlend;
            }
            Vector3 childPosition;
            if (child.transform == null) {
                if (parent != null) { // If we have a proper jigglebone parent...
                    childPosition = transform.TransformPoint(parent.transform.InverseTransformPoint(transform.position));
                } else { // Otherwise we guess with the parent transform
                    childPosition = transform.TransformPoint(transform.parent.InverseTransformPoint(transform.position));
                }
            } else {
                childPosition = child.transform.position;
            }
            Vector3 cachedAnimatedVector = childPosition - transform.position;
            Vector3 simulatedVector = childPositionBlend - positionBlend;
            Quaternion animPoseToPhysicsPose = Quaternion.FromToRotation(cachedAnimatedVector, simulatedVector);
            transform.rotation = animPoseToPhysicsPose * transform.rotation;
        }
        if (transform != null) {
            boneRotationChangeCheck = transform.localRotation;
            bonePositionChangeCheck = transform.localPosition;
        }
    }
}

}