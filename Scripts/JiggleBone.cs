using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace JigglePhysics {

// Uses Verlet to resolve constraints easily 
public partial class JiggleBone {
    private readonly bool hasTransform;
    private readonly PositionSignal targetAnimatedBoneSignal;
    private Vector3 currentFixedAnimatedBonePosition;

    public readonly JiggleBone parent;
    private JiggleBone child;
    private Quaternion boneRotationChangeCheck;
    private Vector3 bonePositionChangeCheck;
    private Quaternion lastValidPoseBoneRotation;
    private float projectionAmount;

    private Vector3 lastValidPoseBoneLocalPosition;
    private float normalizedIndex;

    public readonly Transform transform;

    private readonly PositionSignal particleSignal;
    private Vector3 workingPosition;
    private Vector3? preTeleportPosition;
    private Vector3 extrapolatedPosition;
    
    private float GetLengthToParent() {
        if (parent == null) {
            return 0.1f;
        }
        return Vector3.Distance(currentFixedAnimatedBonePosition, parent.currentFixedAnimatedBonePosition);
    }
    
    public JiggleBone(Transform transform, JiggleBone parent, float projectionAmount = 1f) {
        this.transform = transform;
        this.parent = parent;
        this.projectionAmount = projectionAmount;

        Vector3 position;
        if (transform != null) {
            lastValidPoseBoneRotation = transform.localRotation;
            lastValidPoseBoneLocalPosition = transform.localPosition;
            position = transform.position;
        } else {
            position = GetProjectedPosition();
        }

        targetAnimatedBoneSignal = new PositionSignal(position, Time.timeAsDouble);
        particleSignal = new PositionSignal(position, Time.timeAsDouble);

        hasTransform = transform != null;
        if (parent == null) {
            return;
        }
        this.parent.child = this;
    }

    public void CalculateNormalizedIndex() {
        int distanceToRoot = 0;
        JiggleBone test = this;
        while (test.parent != null) {
            test = test.parent;
            distanceToRoot++;
        }

        int distanceToChild = 0;
        test = this;
        while (test.child != null) {
            test = test.child;
            distanceToChild++;
        }

        int max = distanceToRoot + distanceToChild;
        float frac = (float)distanceToRoot / max;
        normalizedIndex = frac;
    }

    public void VerletPass(JiggleSettingsData jiggleSettings, Vector3 wind, double time) {
        currentFixedAnimatedBonePosition = targetAnimatedBoneSignal.SamplePosition(time);
        if (parent == null) {
            workingPosition = currentFixedAnimatedBonePosition;
            particleSignal.SetPosition(workingPosition, time);
            return;
        }
        Vector3 localSpaceVelocity = (particleSignal.GetCurrent()-particleSignal.GetPrevious()) - (parent.particleSignal.GetCurrent()-parent.particleSignal.GetPrevious());
        workingPosition = NextPhysicsPosition(
            particleSignal.GetCurrent(), particleSignal.GetPrevious(), localSpaceVelocity, Time.fixedDeltaTime,
            jiggleSettings.gravityMultiplier,
            jiggleSettings.friction,
            jiggleSettings.airDrag
        );
        workingPosition += wind * (Time.fixedDeltaTime * jiggleSettings.airDrag);
    }

    public void CollisionPreparePass(JiggleSettingsData jiggleSettings) {
        workingPosition = ConstrainLengthBackwards(workingPosition, jiggleSettings.lengthElasticity*jiggleSettings.lengthElasticity*0.5f);
    }

    public void ConstraintPass(JiggleSettingsData jiggleSettings) {
        if (parent == null) {
            return;
        }
        workingPosition = ConstrainAngle(workingPosition, jiggleSettings.angleElasticity*jiggleSettings.angleElasticity, jiggleSettings.elasticitySoften); 
        workingPosition = ConstrainLength(workingPosition, jiggleSettings.lengthElasticity*jiggleSettings.lengthElasticity);
    }

    public void CollisionPass(JiggleSettingsBase jiggleSettings, List<Collider> colliders) {
        if (colliders.Count == 0) {
            return;
        }

        if (!CachedSphereCollider.TryGet(out SphereCollider sphereCollider)) {
            return;
        }
        foreach (var collider in colliders) {
            sphereCollider.radius = jiggleSettings.GetRadius(normalizedIndex);
            if (sphereCollider.radius <= 0) {
                continue;
            }

            if (Physics.ComputePenetration(sphereCollider, workingPosition, Quaternion.identity,
                    collider, collider.transform.position, collider.transform.rotation,
                    out Vector3 dir, out float dist)) {
                workingPosition += dir * dist;
            }
        }
    }

    public void SignalWritePosition(double time) {
        particleSignal.SetPosition(workingPosition, time);
    }


    private Vector3 GetProjectedPosition() {
        Vector3 parentTransformPosition = parent.transform.position;
        return parent.transform.TransformPoint(parent.GetParentTransform().InverseTransformPoint(parentTransformPosition)*projectionAmount);
    }

    private Vector3 GetTransformPosition() {
        if (!hasTransform) {
            return GetProjectedPosition();
        } else {
            return transform.position;
        }
    }

    private Transform GetParentTransform() {
        if (parent != null) {
            return parent.transform;
        }
        return transform.parent;
    }

    private void CacheAnimationPosition() {
        if (!hasTransform) {
            targetAnimatedBoneSignal.SetPosition(GetProjectedPosition(), Time.timeAsDouble);
            return;
        }
        targetAnimatedBoneSignal.SetPosition(transform.position, Time.timeAsDouble);
        lastValidPoseBoneRotation = transform.localRotation;
        lastValidPoseBoneLocalPosition = transform.localPosition;
    }
    
    private Vector3 ConstrainLengthBackwards(Vector3 newPosition, float elasticity) {
        if (child == null) {
            return newPosition;
        }
        Vector3 diff = newPosition - child.workingPosition;
        Vector3 dir = diff.normalized;
        return Vector3.Lerp(newPosition, child.workingPosition + dir * child.GetLengthToParent(), elasticity);
    }
    
    private Vector3 ConstrainLength(Vector3 newPosition, float elasticity) {
        Vector3 diff = newPosition - parent.workingPosition;
        Vector3 dir = diff.normalized;
        return Vector3.Lerp(newPosition, parent.workingPosition + dir * GetLengthToParent(), elasticity);
    }

    public void MatchAnimationInstantly() {
        var time = Time.timeAsDouble;
        Vector3 position = GetTransformPosition();
        targetAnimatedBoneSignal.FlattenSignal(time, position);
        particleSignal.FlattenSignal(time, position);
    }

    /// <summary>
    /// Physically accurate teleportation, maintains the existing signals of motion and keeps their trajectories through a teleport. First call PrepareTeleport(), then move the character, then call FinishTeleport().
    /// Use MatchAnimationInstantly() instead if you don't want jiggles to be maintained through a teleport.
    /// </summary>
    public void PrepareTeleport() {
        preTeleportPosition = GetTransformPosition();
    }
    
    /// <summary>
    /// The companion function to PrepareTeleport, it discards all the movement that has happened since the call to PrepareTeleport, assuming that they've both been called on the same frame.
    /// </summary>
    public void FinishTeleport() {
        if (!preTeleportPosition.HasValue) {
            MatchAnimationInstantly();
            return;
        }

        var position = GetTransformPosition();
        Vector3 diff = position - preTeleportPosition.Value;
        targetAnimatedBoneSignal.FlattenSignal(Time.timeAsDouble, position);
        particleSignal.OffsetSignal(diff);
        workingPosition += diff;
    }

    private Vector3 ConstrainAngleBackward(Vector3 newPosition, float elasticity, float elasticitySoften) {
        if (child == null || child.child == null) {
            return newPosition;
        }
        Vector3 cToDTargetPose = child.child.currentFixedAnimatedBonePosition - child.currentFixedAnimatedBonePosition;
        Vector3 cToD = child.child.workingPosition - child.workingPosition;
        Quaternion neededRotation = Quaternion.FromToRotation(cToDTargetPose, cToD);
        Vector3 cToB = newPosition - child.workingPosition;
        Vector3 constraintTarget = neededRotation * cToB;

        Debug.DrawLine(newPosition, child.workingPosition + constraintTarget, Color.cyan);
        float error = Vector3.Distance(newPosition, child.workingPosition + constraintTarget);
        error /= child.GetLengthToParent();
        error = Mathf.Clamp01(error);
        error = Mathf.Pow(error, elasticitySoften * 2f);
        return Vector3.Lerp(newPosition, child.workingPosition + constraintTarget, elasticity * error);
    }

    private Vector3 ConstrainAngle(Vector3 newPosition, float elasticity, float elasticitySoften) {
        if (!hasTransform && projectionAmount == 0f) {
            return newPosition;
        }
        Vector3 parentParentPosition;
        Vector3 poseParentParent;
        if (parent.parent == null) {
            poseParentParent = parent.currentFixedAnimatedBonePosition + (parent.currentFixedAnimatedBonePosition - currentFixedAnimatedBonePosition);
            parentParentPosition = poseParentParent;
        } else {
            parentParentPosition = parent.parent.workingPosition;
            poseParentParent = parent.parent.currentFixedAnimatedBonePosition;
        }
        Vector3 parentAimTargetPose = parent.currentFixedAnimatedBonePosition - poseParentParent;
        Vector3 parentAim = parent.workingPosition - parentParentPosition;
        Quaternion TargetPoseToPose = Quaternion.FromToRotation(parentAimTargetPose, parentAim);
        Vector3 currentPose = currentFixedAnimatedBonePosition - poseParentParent;
        Vector3 constraintTarget = TargetPoseToPose * currentPose;
        float error = Vector3.Distance(newPosition, parentParentPosition + constraintTarget);
        error /= GetLengthToParent();
        error = Mathf.Clamp01(error);
        error = Mathf.Pow(error, elasticitySoften * 2f);
        return Vector3.Lerp(newPosition, parentParentPosition + constraintTarget, elasticity * error);
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
            Debug.DrawLine(workingPosition, parent.workingPosition, simulateColor, 0, false);
        }
        Debug.DrawLine(currentFixedAnimatedBonePosition, parent.currentFixedAnimatedBonePosition, targetColor, 0, false);
    }
    public Vector3 DeriveFinalSolvePosition(Vector3 offset) {
        extrapolatedPosition = offset+particleSignal.SamplePosition(Time.timeAsDouble);
        return extrapolatedPosition;
    }

    public Vector3 GetCachedSolvePosition() => extrapolatedPosition;

    public void PrepareBone() {
        // If bone is not animated, return to last unadulterated pose
        if (hasTransform) {
            if (boneRotationChangeCheck == transform.localRotation) {
                transform.localRotation = lastValidPoseBoneRotation;
            }
            if (bonePositionChangeCheck == transform.localPosition) {
                transform.localPosition = lastValidPoseBoneLocalPosition;
            }
        }
        CacheAnimationPosition();
    }

    public void OnDrawGizmos(JiggleSettingsBase jiggleSettings) {
        if (transform != null && child != null && child.transform != null) {
            Gizmos.DrawLine(transform.position, child.transform.position);
        }
        if (transform != null && child != null && child.transform == null) {
            Gizmos.DrawLine(transform.position, child.GetProjectedPosition());
        }
        if (transform != null && jiggleSettings != null) {
            Gizmos.DrawWireSphere(transform.position, jiggleSettings.GetRadius(normalizedIndex));
        }
        if (transform == null && jiggleSettings != null) {
            Gizmos.DrawWireSphere(GetProjectedPosition(), jiggleSettings.GetRadius(normalizedIndex));
        }
    }

    public void PoseBone(float blend) {
        if (child != null) {
            Vector3 positionBlend = Vector3.Lerp(targetAnimatedBoneSignal.SamplePosition(Time.timeAsDouble), extrapolatedPosition, blend);
            Vector3 childPositionBlend = Vector3.Lerp(child.targetAnimatedBoneSignal.SamplePosition(Time.timeAsDouble), child.extrapolatedPosition, blend);

            if (parent != null) {
                transform.position = positionBlend;
            }
            Vector3 childPosition = child.GetTransformPosition();
            Vector3 cachedAnimatedVector = childPosition - transform.position;
            Vector3 simulatedVector = childPositionBlend - positionBlend;
            Quaternion animPoseToPhysicsPose = Quaternion.FromToRotation(cachedAnimatedVector, simulatedVector);
            transform.rotation = animPoseToPhysicsPose * transform.rotation;
        }
        if (hasTransform) {
            boneRotationChangeCheck = transform.localRotation;
            bonePositionChangeCheck = transform.localPosition;
        }
    }
}

}