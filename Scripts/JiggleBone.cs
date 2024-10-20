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
    private Vector3 cachedPosition;
    
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
            particleSignal.GetCurrent(), particleSignal.GetPrevious(), localSpaceVelocity, JiggleRigBuilder.VERLET_TIME_STEP,
            jiggleSettings.gravityMultiplier,
            jiggleSettings.friction,
            jiggleSettings.airDrag
        );
        workingPosition += wind * (JiggleRigBuilder.VERLET_TIME_STEP * jiggleSettings.airDrag);
    }

    public void CollisionPreparePass(JiggleSettingsData jiggleSettings) {
        workingPosition = ConstrainLengthBackwards(workingPosition, jiggleSettings.lengthElasticity*jiggleSettings.lengthElasticity*0.5f);
    }
    
    public void SecondPass(JiggleSettingsBase jiggleSettings, JiggleSettingsData jiggleData, List<Collider> colliders, double time) {
        if (parent == null) {
            return;
        }
        workingPosition = ConstrainAngle(workingPosition, jiggleData.angleElasticity*jiggleData.angleElasticity, jiggleData.elasticitySoften); 
        workingPosition = ConstrainLength(workingPosition, jiggleData.lengthElasticity*jiggleData.lengthElasticity);
        
        if (colliders.Count != 0 && CachedSphereCollider.TryGet(out SphereCollider sphereCollider)) {
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

    /// <summary>
    /// Matches the particle signal to the current pose, then undoes the pose. This is useful for confining the jiggles, like they got "grabbed".
    /// I would essentially use IK to set the bones to be grabbed. Then call this function so the virtual jiggle particles also move to the same location.
    /// It would need to be called every frame that the chain is grabbed.
    /// </summary>
    [Obsolete("Please use SetTargetAndResetToLastValidPose() instead.")]
    public void SampleAndReset() {
        var time = Time.timeAsDouble;
        Vector3 position = GetTransformPosition();
        particleSignal.FlattenSignal(time, position);
        if (!hasTransform) return;
        transform.localPosition = bonePositionChangeCheck;
        transform.localRotation = boneRotationChangeCheck;
    }

    /// <summary>
    /// Matches the particle signal to the current pose, then undoes the pose. This is useful for confining the jiggles, like they got "grabbed".
    /// I would essentially use IK to set the bones to be grabbed. Then call this function so the virtual jiggle particles also move to the same location.
    /// It would need to be called every frame that the chain is grabbed.
    /// </summary>
    public void SetTargetAndResetToLastValidPose() {
        var time = Time.timeAsDouble;
        Vector3 position = GetTransformPosition();
        particleSignal.FlattenSignal(time, position);
        if (!hasTransform) return;
        transform.localPosition = bonePositionChangeCheck;
        transform.localRotation = boneRotationChangeCheck;
    }

    /// <summary>
    /// Matches the particle signal, and the animation signal to the current pose. This eliminates all jiggles, effectively a teleport and velocity reset.
    /// You should call ApplyTargetPose first if you want to fully reset the character.
    /// This is mostly for teleportation or one-frame pose changes.
    /// </summary>
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
    
    /// <summary>
    /// Extrapolates the particle signal from the past, to the current time. This must be done right before Pose.
    /// </summary>
    public Vector3 DeriveFinalSolvePosition(Vector3 offset) {
        extrapolatedPosition = particleSignal.SamplePosition(Time.timeAsDouble-JiggleRigBuilder.VERLET_TIME_STEP) + offset;
        return extrapolatedPosition;
    }

    public Vector3 GetCachedSolvePosition() => extrapolatedPosition;

    /// <summary>
    /// Brings the transforms back to their last-known valid state. Valid meaning unjiggled. Either sourced from spawn, Animator, or by manual position sets by the user.
    /// Then samples the current pose for use later in stepping the simulation. This should be called at the beginning of every frame that Pose is called. It creates the desired "target pose" that the simulation tries to match.
    /// </summary>
    public void ApplyValidPoseThenSampleTargetPose() {
        if (!hasTransform) {
            targetAnimatedBoneSignal.SetPosition(GetProjectedPosition(), Time.timeAsDouble);
            return;
        }
        if (boneRotationChangeCheck == transform.localRotation && bonePositionChangeCheck == transform.localPosition) {
            transform.SetLocalPositionAndRotation(lastValidPoseBoneLocalPosition, lastValidPoseBoneRotation);
        }
        targetAnimatedBoneSignal.SetPosition(transform.position, Time.timeAsDouble);
        
        lastValidPoseBoneRotation = transform.localRotation;
        lastValidPoseBoneLocalPosition = transform.localPosition;
    }
    
    public void OnDrawGizmos(JiggleSettingsBase jiggleSettings, bool isRoot = false) {
        var time = Time.timeAsDouble;
        Vector3 pos = (isRoot || !Application.isPlaying) ? (hasTransform ? transform.position : GetProjectedPosition()) : particleSignal.SamplePosition(time-JiggleRigBuilder.VERLET_TIME_STEP);
        if (child != null) {
            Gizmos.DrawLine(pos, !Application.isPlaying ? (child.hasTransform ? child.transform.position : child.GetProjectedPosition()) : child.particleSignal.SamplePosition(time-JiggleRigBuilder.VERLET_TIME_STEP));
        }
        if (jiggleSettings != null) {
            Gizmos.DrawWireSphere(pos, jiggleSettings.GetRadius(normalizedIndex));
        }
    }

    /// <summary>
    /// Uses the data from DeriveFinalPosition() to actually generate a pose.
    /// </summary>
    /// <param name="blend"></param>
    public void PoseBone(float blend) {
        if (child != null) {
            var position = transform.position;
            Vector3 positionBlend = Vector3.Lerp(position, extrapolatedPosition, blend);
            Vector3 childPositionBlend = Vector3.Lerp(child.GetTransformPosition(), child.extrapolatedPosition, blend);

            if (parent != null) {
                transform.position = positionBlend;
            }
            Vector3 childPosition = child.GetTransformPosition();
            Vector3 cachedAnimatedVector = childPosition - position;
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