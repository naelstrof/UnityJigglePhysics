using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JiggleRig : MonoBehaviour {

    [SerializeField]
    private List<Transform> ignoredTransforms;

    [Range(0f,1f)] [SerializeField]
    float gravityMultiplier = 1f;

    [Range(0f,1f)] [SerializeField]
    float friction = 0.5f;

    [Range(0f,1f)] [SerializeField]
    float inertness = 0.5f;

    [Range(0f,1f)] [SerializeField]
    float blend = 1f;

    [Range(0f,1f)] [SerializeField]
    float elasticity = 0.5f;

    private List<SimulatedPoint> simulatedPoints;

    private void Awake() {
        simulatedPoints = new List<SimulatedPoint>();
        CreateSimulatedPoints(transform, null);
    }
    private void LateUpdate() {
        foreach (SimulatedPoint simulatedPoint in simulatedPoints) {
            if (simulatedPoint.transform != null && simulatedPoint.cachedLocalBoneRotation == simulatedPoint.transform.localRotation) {
                simulatedPoint.transform.localRotation = simulatedPoint.cachedInitialLocalBoneRotation;
            }
            simulatedPoint.CacheAnimationPosition();
        }
        foreach (SimulatedPoint simulatedPoint in simulatedPoints) {
            simulatedPoint.DebugDraw(Color.green, true);
            if (simulatedPoint.child != null) {
                Vector3 cachedAnimatedVector = simulatedPoint.child.cachedAnimatedPosition - simulatedPoint.cachedAnimatedPosition;
                Vector3 simulatedVector = simulatedPoint.child.interpolatedPosition - simulatedPoint.interpolatedPosition;
                Quaternion animPoseToPhysicsPose = Quaternion.FromToRotation(cachedAnimatedVector, simulatedVector);
                animPoseToPhysicsPose = Quaternion.Lerp(Quaternion.identity, animPoseToPhysicsPose, blend);
                simulatedPoint.transform.rotation = animPoseToPhysicsPose * simulatedPoint.cachedBoneRotation;
            }
            if (simulatedPoint.transform != null) {
                simulatedPoint.cachedLocalBoneRotation = simulatedPoint.transform.localRotation;
            }
        }
    }

    private void FixedUpdate() {
        foreach (SimulatedPoint simulatedPoint in simulatedPoints) {
            if (simulatedPoint.parent == null) {
                simulatedPoint.SnapTo(transform);
            } else {
                simulatedPoint.StepPhysics(Time.deltaTime, gravityMultiplier, friction, inertness);
                simulatedPoint.ConstrainAngle(elasticity*elasticity);
                simulatedPoint.ConstrainLength();
            }
            //simulatedPoint.DebugDraw(Color.black, false);
        }

    }

    private void CreateSimulatedPoints(Transform currentTransform, SimulatedPoint parentSimulatedPoint) {
        SimulatedPoint currentSimulatedPoint = new SimulatedPoint(currentTransform, parentSimulatedPoint, currentTransform.position);
        simulatedPoints.Add(currentSimulatedPoint);
        // Create an extra purely virtual point if we have no children.
        if (currentTransform.childCount == 0) {
            if (currentSimulatedPoint.parent == null) {
                throw new UnityException("Can't have a singular jiggle bone with no parents. That doesn't even make sense!");
            }
            Vector3 projectedForward = (currentTransform.position - parentSimulatedPoint.transform.position).normalized;
            simulatedPoints.Add(new SimulatedPoint(null, currentSimulatedPoint, currentTransform.position + projectedForward*parentSimulatedPoint.lengthToParent));
            return;
        }
        for (int i = 0; i < currentTransform.childCount; i++) {
            if (ignoredTransforms.Contains(currentTransform.GetChild(i))) {
                continue;
            }
            CreateSimulatedPoints(currentTransform.GetChild(i), currentSimulatedPoint);
        }
    }

}
