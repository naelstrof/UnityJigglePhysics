using System;
using UnityEngine;

namespace JigglePhysics {

// Uses Verlet to resolve constraints easily 
public partial class JiggleBone {
    public readonly bool hasTransform;
    public readonly PositionSignal targetAnimatedBoneSignal;
    public Vector3 currentFixedAnimatedBonePosition;

    public readonly JiggleBone parent;
    private JiggleBone child;
    private Quaternion boneRotationChangeCheck;
    private Vector3 bonePositionChangeCheck;
    private Quaternion lastValidPoseBoneRotation;
    public float projectionAmount;

    private Vector3 lastValidPoseBoneLocalPosition;
    public float normalizedIndex;

    public readonly Transform transform;

    public readonly PositionSignal particleSignal;
    public Vector3 workingPosition;
    private Vector3? preTeleportPosition;
    private Vector3 extrapolatedPosition;
    public readonly bool shouldConfineAngle;
    
    public float GetLengthToParent() {
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
        shouldConfineAngle = hasTransform || this.projectionAmount != 0;
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