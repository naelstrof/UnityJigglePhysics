using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JigglePhysics {

// Uses Verlet to resolve constraints easily 
public class JiggleBone {

    private static class CachedSphereCollider {
        private class DestroyListener : MonoBehaviour {
            void OnDestroy() {
                _hasSphere = false;
            }
        }
        private static int remainingBuilders;
        private static bool _hasSphere = false;
        private static SphereCollider _sphereCollider;

        public static void StartPass() {
            if (remainingBuilders == 0 && TryGet(out SphereCollider collider)) {
                collider.enabled = true;
            }
            remainingBuilders++;
            
        }

        public static void FinishedPass() {
            remainingBuilders--;
            if (remainingBuilders == 0 && TryGet(out SphereCollider collider)) {
                collider.enabled = false;
            }
        }
        
        public static bool TryGet(out SphereCollider collider) {
            if (_hasSphere) {
                collider = _sphereCollider;
                return true;
            }
            try {
                var obj = new GameObject("JiggleBoneSphereCollider", new []{typeof(SphereCollider), typeof(DestroyListener)})
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                if (Application.isPlaying) {
                    Object.DontDestroyOnLoad(obj);
                }

                _sphereCollider = obj.GetComponent<SphereCollider>();
                collider = _sphereCollider;
                collider.enabled = false;
                _hasSphere = true;
                return true;
            } catch {
                _hasSphere = false;
                collider = null;
                throw;
            }
        }
    }

    
    private PositionSignal targetAnimatedBoneSignal;
    private Vector3 currentFixedAnimatedBonePosition;

    public JiggleBone parent;
    public JiggleBone child;
    private Quaternion boneRotationChangeCheck;
    private Vector3 bonePositionChangeCheck;
    public Quaternion lastValidPoseBoneRotation;

    private Vector3 lastValidPoseBoneLocalPosition;
    private float normalizedIndex;
    //public Vector3 targetAnimatedBonePosition;


    public Transform transform;

    private PositionSignal particleSignal;
    private Vector3 workingPosition;
    public Vector3 preTeleportPosition;

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
    
    public JiggleBone(Transform transform, JiggleBone parent, Vector3 position) {
        this.transform = transform;
        this.parent = parent;
        if (transform != null) {
            lastValidPoseBoneRotation = transform.localRotation;
            lastValidPoseBoneLocalPosition = transform.localPosition;
        }
        targetAnimatedBoneSignal = new PositionSignal(position, Time.timeAsDouble);
        particleSignal = new PositionSignal(position, Time.timeAsDouble);

        if (parent == null) {
            return;
        }

        //previousLocalPosition = parent.transform.InverseTransformPoint(previousPosition);
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
        CachedSphereCollider.StartPass();
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
        CachedSphereCollider.FinishedPass();
    }

    public void SignalWritePosition(double time) {
        particleSignal.SetPosition(workingPosition, time);
    }


    private Vector3 GetProjectedPosition() {
        if (transform != null) {
            throw new UnityException("Tried to get a projected position of a jigglebone that doesn't need to project!");
        }
        Vector3 parentTransformPosition = parent.transform.position;
        if (parent.parent != null) {
            //Vector3 projectedForward = (parentTransformPosition - parent.parent.transform.position).normalized;
            Vector3 pos = parent.transform.TransformPoint( parent.parent.transform.InverseTransformPoint(parentTransformPosition));
            return pos;
        } else {
            // parent.transform.parent is guaranteed to exist here, unless the user is jiggling a single bone by itself (which throws an exception).
            //Vector3 projectedForward = (parentTransformPosition - parent.transform.parent.position).normalized;
            Vector3 pos = parent.transform.TransformPoint(parent.transform.parent.InverseTransformPoint(parentTransformPosition));
            return pos;
        }
    }

    public void CacheAnimationPosition() {
        if (transform == null) {
            targetAnimatedBoneSignal.SetPosition(GetProjectedPosition(), Time.timeAsDouble);
            return;
        }
        targetAnimatedBoneSignal.SetPosition(transform.position, Time.timeAsDouble);
        lastValidPoseBoneRotation = transform.localRotation;
        lastValidPoseBoneLocalPosition = transform.localPosition;
    }
    
    public Vector3 ConstrainLengthBackwards(Vector3 newPosition, float elasticity) {
        if (child == null) {
            return newPosition;
        }
        Vector3 diff = newPosition - child.workingPosition;
        Vector3 dir = diff.normalized;
        return Vector3.Lerp(newPosition, child.workingPosition + dir * child.GetLengthToParent(), elasticity);
    }
    
    public Vector3 ConstrainLength(Vector3 newPosition, float elasticity) {
        Vector3 diff = newPosition - parent.workingPosition;
        Vector3 dir = diff.normalized;
        return Vector3.Lerp(newPosition, parent.workingPosition + dir * GetLengthToParent(), elasticity);
    }

    public void PrepareTeleport() {
        throw new NotImplementedException();
        if (transform == null) {
            Vector3 parentTransformPosition = parent.transform.position;
            if (parent.parent != null) {
                preTeleportPosition = parent.transform.TransformPoint( parent.parent.transform.InverseTransformPoint(parentTransformPosition));
            } else {
                preTeleportPosition = parent.transform.TransformPoint(parent.transform.parent.InverseTransformPoint(parentTransformPosition));
            }
            return;
        }
        preTeleportPosition = transform.position;
    }
    
    public void FinishTeleport() {
        throw new NotImplementedException();
        Vector3 teleportedPosition;
        if (transform == null) {
            Vector3 parentTransformPosition = parent.transform.position;
            if (parent.parent != null) {
                teleportedPosition = parent.transform.TransformPoint( parent.parent.transform.InverseTransformPoint(parentTransformPosition));
            } else {
                teleportedPosition = parent.transform.TransformPoint(parent.transform.parent.InverseTransformPoint(parentTransformPosition));
            }
        } else {
            teleportedPosition = transform.position;
        }
        Vector3 diff = teleportedPosition - preTeleportPosition;
        //lastTargetAnimatedBoneFrame = new PositionFrame(lastTargetAnimatedBoneFrame.position + diff, lastTargetAnimatedBoneFrame.time);
        //currentTargetAnimatedBoneFrame = new PositionFrame(currentTargetAnimatedBoneFrame.position + diff, currentTargetAnimatedBoneFrame.time);
        //position += diff;
        //previousPosition += diff;
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
    public Vector3 DeriveFinalSolvePosition() {
        extrapolatedPosition = particleSignal.SamplePosition(Time.timeAsDouble);
        return extrapolatedPosition;
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