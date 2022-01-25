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

    private List<SimulatedPoint> simulatedPoints;

    private void Awake() {
        simulatedPoints = new List<SimulatedPoint>();
        CreateSimulatedPoints(transform, null);
    }
    private void LateUpdate() {
        foreach (SimulatedPoint simulatedPoint in simulatedPoints) {
            if (simulatedPoint.cachedLocalBoneRotation == simulatedPoint.transform.localRotation) {
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
            simulatedPoint.cachedLocalBoneRotation = simulatedPoint.transform.localRotation;
        }
    }

    private void FixedUpdate() {
        foreach (SimulatedPoint simulatedPoint in simulatedPoints) {
            if (simulatedPoint.parent == null) {
                simulatedPoint.SnapTo(transform);
            } else {
                simulatedPoint.StepPhysics(Time.deltaTime, gravityMultiplier, friction, inertness);
                simulatedPoint.ConstrainAngle();
                simulatedPoint.ConstrainLength();
            }
            simulatedPoint.DebugDraw(Color.black, false);
        }

    }

    private void CreateSimulatedPoints(Transform currentTransform, SimulatedPoint parentSimulatedPoint) {
        SimulatedPoint currentSimulatedPoint = new SimulatedPoint(currentTransform, parentSimulatedPoint, currentTransform.position);
        simulatedPoints.Add(currentSimulatedPoint);
        for (int i = 0; i < currentTransform.childCount; i++) {
            if (ignoredTransforms.Contains(currentTransform.GetChild(i))) {
                continue;
            }
            CreateSimulatedPoints(currentTransform.GetChild(i), currentSimulatedPoint);
        }
    }

}
