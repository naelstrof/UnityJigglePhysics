using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

// Uses Verlet to resolve constraints easily 
public class JiggleBone {
    private struct PositionFrame {
        public Vector3 position;
        public float time;
        public PositionFrame(Vector3 position, float time) {
            this.position = position;
            this.time = time;
        }
    }

    public JiggleBone parent;
    public JiggleBone child;
    private Quaternion boneRotationChangeCheck;
    private Vector3 bonePositionChangeCheck;
    public Quaternion lastValidPoseBoneRotation;
    private Vector3 lastValidPoseBoneLocalPosition;
    //public Vector3 targetAnimatedBonePosition;
    
    private PositionFrame targetAnimatedBoneFrame;
    private PositionFrame lastTargetAnimatedBoneFrame;

    public Vector3 GetTargetBonePosition(float time) {
        float diff = targetAnimatedBoneFrame.time - lastTargetAnimatedBoneFrame.time;
        if (diff == 0) {
            return targetAnimatedBoneFrame.position;
        }
        float t = (time - lastTargetAnimatedBoneFrame.time) / diff;
        return Vector3.LerpUnclamped(lastTargetAnimatedBoneFrame.position, targetAnimatedBoneFrame.position, t);
    }

    public Transform transform;

    private float updateTime;
    private float previousUpdateTime;
    
    public Vector3 position;
    public Vector3 previousPosition;
    
    public float LengthToParent(float time) {
        if (parent == null) {
            return 0.1f;
        }
        return Vector3.Distance(GetTargetBonePosition(time), parent.GetTargetBonePosition(time));
    }

    private Vector3 cachedInterpolatedPosition;

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
        targetAnimatedBoneFrame = lastTargetAnimatedBoneFrame;

        if (parent == null) {
            return;
        }

        //previousLocalPosition = parent.transform.InverseTransformPoint(previousPosition);
        this.parent.child = this;
    }

    public void Simulate(JiggleSettingsBase jiggleSettings, Vector3 wind, JiggleBone root) {
        if (parent == null) {
            SetNewPosition(GetTargetBonePosition(Time.time));
            return;
        }
        Vector3 localSpaceVelocity = (position-previousPosition) - (parent.position-parent.previousPosition);
        //Debug.DrawLine(position, position + localSpaceVelocity, Color.cyan);
        Vector3 newPosition = JiggleBone.NextPhysicsPosition(
            position, previousPosition, localSpaceVelocity, Time.deltaTime,
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Gravity),
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Friction),
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.AirFriction)
        );
        newPosition += wind * (Time.deltaTime * jiggleSettings.GetParameter(JiggleSettingsBase.JiggleSettingParameter.AirFriction));
        newPosition = ConstrainAngle(newPosition, jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.AngleElasticity)*jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.AngleElasticity), jiggleSettings.GetParameter(JiggleSettingsBase.JiggleSettingParameter.ElasticitySoften));
        newPosition = ConstrainLength(newPosition, jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.LengthElasticity)*jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.LengthElasticity));
        SetNewPosition(newPosition);
    }

    public void CacheAnimationPosition() {
        // Purely virtual particles need to reconstruct their desired position.
        lastTargetAnimatedBoneFrame = targetAnimatedBoneFrame;
        if (transform == null) {
            Vector3 parentTransformPosition = parent.transform.position;
            if (parent.parent != null) {
                //Vector3 projectedForward = (parentTransformPosition - parent.parent.transform.position).normalized;
                Vector3 pos = parent.transform.TransformPoint( parent.parent.transform.InverseTransformPoint(parentTransformPosition));
                targetAnimatedBoneFrame = new PositionFrame(pos, Time.time);
            } else {
                // parent.transform.parent is guaranteed to exist here, unless the user is jiggling a single bone by itself (which throws an exception).
                //Vector3 projectedForward = (parentTransformPosition - parent.transform.parent.position).normalized;
                Vector3 pos = parent.transform.TransformPoint(parent.transform.parent.InverseTransformPoint(parentTransformPosition));
                targetAnimatedBoneFrame = new PositionFrame(pos, Time.time);
            }
            return;
        }
        targetAnimatedBoneFrame = new PositionFrame(transform.position, Time.time);
        lastValidPoseBoneRotation = transform.localRotation;
        lastValidPoseBoneLocalPosition = transform.localPosition;
    }
    
    public Vector3 ConstrainLength(Vector3 newPosition, float elasticity) {
        Vector3 diff = newPosition - parent.position;
        Vector3 dir = diff.normalized;
        return Vector3.Lerp(newPosition, parent.position + dir * LengthToParent(Time.time), elasticity);
    }

    public Vector3 ConstrainAngle(Vector3 newPosition, float elasticity, float elasticitySoften) {
        Vector3 parentParentPosition;
        Vector3 poseParentParent;
        if (parent.parent == null) {
            poseParentParent = parent.GetTargetBonePosition(Time.time) + (parent.GetTargetBonePosition(Time.time) - GetTargetBonePosition(Time.time));
            parentParentPosition = poseParentParent;
        } else {
            parentParentPosition = parent.parent.position;
            poseParentParent = parent.parent.GetTargetBonePosition(Time.time);
        }
        Vector3 parentAimTargetPose = parent.GetTargetBonePosition(Time.time) - poseParentParent;
        Vector3 parentAim = parent.position - parentParentPosition;
        Quaternion TargetPoseToPose = Quaternion.FromToRotation(parentAimTargetPose, parentAim);
        Vector3 currentPose = GetTargetBonePosition(Time.time) - poseParentParent;
        Vector3 constraintTarget = TargetPoseToPose * currentPose;
        float error = Vector3.Distance(newPosition, parentParentPosition + constraintTarget);
        error /= LengthToParent(Time.time);
        error = Mathf.Clamp01(error);
        error = Mathf.Pow(error, elasticitySoften * 2f);
        return Vector3.Lerp(newPosition, parentParentPosition + constraintTarget, elasticity * error);
    }

    public void SetNewPosition(Vector3 newPosition) {
        previousUpdateTime = updateTime;
        previousPosition = position;
        //if (parent!=null) previousLocalPosition = parent.transform.InverseTransformPoint(previousPosition);
        updateTime = Time.time;
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
            Debug.DrawLine(cachedInterpolatedPosition, parent.cachedInterpolatedPosition, simulateColor, 0, false);
        } else {
            Debug.DrawLine(position, parent.position, simulateColor, 0, false);
        }

        Debug.DrawLine(GetTargetBonePosition(Time.time), parent.GetTargetBonePosition(Time.time), targetColor, 0, false);
    }
    public void PrepareInterpolation(bool interpolate) {
        if (interpolate) {
            float diff = updateTime - previousUpdateTime;
            if (diff == 0) {
                cachedInterpolatedPosition = position;
                return;
            }
            float t = (Time.time - previousUpdateTime) / diff;
            cachedInterpolatedPosition = Vector3.LerpUnclamped(previousPosition, position, t);
        } else {
            cachedInterpolatedPosition = position;
        }
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
            float cachedDistance = Vector3.Distance(transform.position, cachedInterpolatedPosition);

            Vector3 interpolatedPositionBlend = Vector3.Lerp(GetTargetBonePosition(Time.time), cachedInterpolatedPosition, blend);
            Vector3 interpolatedChildPositionBlend = Vector3.Lerp(child.GetTargetBonePosition(Time.time), child.cachedInterpolatedPosition, blend);

            if (parent != null) {
                transform.position = interpolatedPositionBlend;
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
            Vector3 simulatedVector = interpolatedChildPositionBlend - interpolatedPositionBlend;
            Quaternion animPoseToPhysicsPose = Quaternion.FromToRotation(cachedAnimatedVector, simulatedVector);
            transform.rotation = animPoseToPhysicsPose * transform.rotation;
        }
        if (transform != null) {
            boneRotationChangeCheck = transform.localRotation;
            bonePositionChangeCheck = transform.localPosition;
            //Debug.DrawLine(transform.position, transform.position+boneRotationChangeCheck * Vector3.up, Color.blue);
        }
    }
}

}