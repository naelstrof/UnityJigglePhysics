using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace JigglePhysics {

// Uses Verlet to resolve constraints easily 
public struct JiggleBone {
    public readonly bool hasTransform;
    public readonly PositionSignal targetAnimatedBoneSignal;
    public Vector3 currentFixedAnimatedBonePosition;

    public readonly int? parentID;
    public int? childID;
    
    public Quaternion boneRotationChangeCheck;
    public Vector3 bonePositionChangeCheck;
    public Quaternion lastValidPoseBoneRotation;
    public float projectionAmount;

    public Vector3 lastValidPoseBoneLocalPosition;
    public float normalizedIndex;

    public readonly Transform transform;

    public readonly PositionSignal particleSignal;
    public Vector3 workingPosition;
    public Vector3? preTeleportPosition;
    public Vector3 extrapolatedPosition;
    public readonly bool shouldConfineAngle;
    
    public float GetLengthToParent(List<JiggleBone> bones) {
        if (parentID == null) {
            return 0.1f;
        }
        return Vector3.Distance(currentFixedAnimatedBonePosition, bones[parentID.Value].currentFixedAnimatedBonePosition);
    }
    
    public JiggleBone(List<JiggleBone> bones, Transform transform, JiggleBone? parent, int? parentID, float projectionAmount = 1f) {
        this.transform = transform;
        this.projectionAmount = projectionAmount;

        Vector3 position;
        if (transform != null) {
            lastValidPoseBoneRotation = transform.localRotation;
            lastValidPoseBoneLocalPosition = transform.localPosition;
            position = transform.position;
        } else {
            Assert.IsTrue(parent != null, "Jiggle bones without a transform MUST have a parent...");
            Vector3 parentTransformPosition = parent.Value.transform.position;
            position = parent.Value.transform.TransformPoint(parent.Value.GetParentTransform(bones).InverseTransformPoint(parentTransformPosition)*projectionAmount);
        }

        targetAnimatedBoneSignal = new PositionSignal(position, Time.timeAsDouble);
        particleSignal = new PositionSignal(position, Time.timeAsDouble);

        hasTransform = transform != null;
        shouldConfineAngle = hasTransform || this.projectionAmount != 0;
        
        currentFixedAnimatedBonePosition = default;
        boneRotationChangeCheck = default;
        bonePositionChangeCheck = default;
        lastValidPoseBoneRotation = default;
        lastValidPoseBoneLocalPosition = default;
        normalizedIndex = 0;
        workingPosition = default;
        preTeleportPosition = null;
        extrapolatedPosition = default;
        this.parentID = parentID;
        childID = null;
    }

    public void CalculateNormalizedIndex(List<JiggleBone> bones) {
        int distanceToRoot = 0;
        int? test = parentID;
        while (test != null) {
            test = bones[test.Value].parentID;
            distanceToRoot++;
        }

        int distanceToChild = 0;
        test = childID;
        while (test != null) {
            test = bones[test.Value].childID;
            distanceToChild++;
        }

        int max = distanceToRoot + distanceToChild;
        float frac = (float)distanceToRoot / max;
        normalizedIndex = frac;
    }

    private Vector3 GetProjectedPosition(List<JiggleBone> bones) {
        Vector3 parentTransformPosition = bones[parentID.Value].transform.position;
        return bones[parentID.Value].transform.TransformPoint(bones[parentID.Value].GetParentTransform(bones).InverseTransformPoint(parentTransformPosition)*projectionAmount);
    }

    private Vector3 GetTransformPosition(List<JiggleBone> bones) {
        if (!hasTransform) {
            return GetProjectedPosition(bones);
        } else {
            return transform.position;
        }
    }

    private Transform GetParentTransform(List<JiggleBone> bones) {
        if (parentID != null) {
            return bones[parentID.Value].transform;
        }
        return transform.parent;
    }
    
    /// <summary>
    /// Matches the particle signal to the current pose, then undoes the pose. This is useful for confining the jiggles, like they got "grabbed".
    /// I would essentially use IK to set the bones to be grabbed. Then call this function so the virtual jiggle particles also move to the same location.
    /// It would need to be called every frame that the chain is grabbed.
    /// </summary>
    [Obsolete("Please use SetTargetAndResetToLastValidPose() instead.")]
    public void SampleAndReset(List<JiggleBone> bones) {
        var time = Time.timeAsDouble;
        Vector3 position = GetTransformPosition(bones);
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
    public void SetTargetAndResetToLastValidPose(List<JiggleBone> bones) {
        var time = Time.timeAsDouble;
        Vector3 position = GetTransformPosition(bones);
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
    public void MatchAnimationInstantly(List<JiggleBone> bones) {
        var time = Time.timeAsDouble;
        Vector3 position = GetTransformPosition(bones);
        targetAnimatedBoneSignal.FlattenSignal(time, position);
        particleSignal.FlattenSignal(time, position);
    }

    /// <summary>
    /// Physically accurate teleportation, maintains the existing signals of motion and keeps their trajectories through a teleport. First call PrepareTeleport(), then move the character, then call FinishTeleport().
    /// Use MatchAnimationInstantly() instead if you don't want jiggles to be maintained through a teleport.
    /// </summary>
    public void PrepareTeleport(List<JiggleBone> bones) {
        preTeleportPosition = GetTransformPosition(bones);
    }
    
    /// <summary>
    /// The companion function to PrepareTeleport, it discards all the movement that has happened since the call to PrepareTeleport, assuming that they've both been called on the same frame.
    /// </summary>
    public void FinishTeleport(List<JiggleBone> bones) {
        if (!preTeleportPosition.HasValue) {
            MatchAnimationInstantly(bones);
            return;
        }

        var position = GetTransformPosition(bones);
        Vector3 diff = position - preTeleportPosition.Value;
        targetAnimatedBoneSignal.FlattenSignal(Time.timeAsDouble, position);
        particleSignal.OffsetSignal(diff);
        workingPosition += diff;
    }

    public void DebugDraw(List<JiggleBone> bones, Color simulateColor, Color targetColor, bool interpolated) {
        if (parentID == null) return;
        if (interpolated) {
            Debug.DrawLine(extrapolatedPosition, bones[parentID.Value].extrapolatedPosition, simulateColor, 0, false);
        } else {
            Debug.DrawLine(workingPosition, bones[parentID.Value].workingPosition, simulateColor, 0, false);
        }
        Debug.DrawLine(currentFixedAnimatedBonePosition, bones[parentID.Value].currentFixedAnimatedBonePosition, targetColor, 0, false);
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
    public void ApplyValidPoseThenSampleTargetPose(List<JiggleBone> bones) {
        if (!hasTransform) {
            targetAnimatedBoneSignal.SetPosition(GetProjectedPosition(bones), Time.timeAsDouble);
            return;
        }
        if (boneRotationChangeCheck == transform.localRotation && bonePositionChangeCheck == transform.localPosition) {
            transform.SetLocalPositionAndRotation(lastValidPoseBoneLocalPosition, lastValidPoseBoneRotation);
        }
        targetAnimatedBoneSignal.SetPosition(transform.position, Time.timeAsDouble);
        
        lastValidPoseBoneRotation = transform.localRotation;
        lastValidPoseBoneLocalPosition = transform.localPosition;
    }
    
    public void OnDrawGizmos(List<JiggleBone> bones, JiggleSettingsBase jiggleSettings, bool isRoot = false) {
        var time = Time.timeAsDouble;
        Vector3 pos = (isRoot || !Application.isPlaying) ? (hasTransform ? transform.position : GetProjectedPosition(bones)) : particleSignal.SamplePosition(time-JiggleRigBuilder.VERLET_TIME_STEP);
        if (childID != null) {
            Gizmos.DrawLine(pos, !Application.isPlaying ? (bones[childID.Value].hasTransform ? bones[childID.Value].transform.position : bones[childID.Value].GetProjectedPosition(bones)) : bones[childID.Value].particleSignal.SamplePosition(time-JiggleRigBuilder.VERLET_TIME_STEP));
        }
        if (jiggleSettings != null) {
            Gizmos.DrawWireSphere(pos, jiggleSettings.GetRadius(normalizedIndex));
        }
    }

    /// <summary>
    /// Uses the data from DeriveFinalPosition() to actually generate a pose.
    /// </summary>
    /// <param name="blend"></param>
    public void PoseBone(List<JiggleBone> bones, float blend) {
        if (childID != null) {
            var position = transform.position;
            Vector3 positionBlend = Vector3.Lerp(position, extrapolatedPosition, blend);
            Vector3 childPositionBlend = Vector3.Lerp(bones[childID.Value].GetTransformPosition(bones), bones[childID.Value].extrapolatedPosition, blend);

            if (parentID != null) {
                transform.position = positionBlend;
            }
            Vector3 childPosition = bones[childID.Value].GetTransformPosition(bones);
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