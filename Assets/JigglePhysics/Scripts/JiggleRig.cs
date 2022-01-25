using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JiggleRig : MonoBehaviour {

    //[System.Serializable] private List<Transform> ignoredTransforms;

    private List<SimulatedPoint> simulatedPoints;

    private void Awake() {
        simulatedPoints = new List<SimulatedPoint>();
        CreateSimulatedPoints(transform, null);
    }
    private void LateUpdate() {
        foreach (SimulatedPoint simulatedPoint in simulatedPoints) {
            simulatedPoint.CacheAnimationPosition();
        }
        foreach (SimulatedPoint simulatedPoint in simulatedPoints) {
            simulatedPoint.DebugDraw(Color.green, true);
            if (simulatedPoint.child != null) {
                Vector3 cachedAnimatedVector = simulatedPoint.child.cachedAnimatedPosition - simulatedPoint.cachedAnimatedPosition;
                Vector3 simulatedVector = simulatedPoint.child.position - simulatedPoint.position;
                Quaternion animPoseToPhysicsPose = Quaternion.FromToRotation(cachedAnimatedVector, simulatedVector);
                simulatedPoint.transform.rotation = animPoseToPhysicsPose * simulatedPoint.cachedBoneRotation;
            }
        }
    }

    private void FixedUpdate() {
        foreach (SimulatedPoint simulatedPoint in simulatedPoints) {
            if (simulatedPoint.parent == null) {
                simulatedPoint.SnapTo(transform);
            } else {
                simulatedPoint.StepPhysics(Time.deltaTime);
                simulatedPoint.ConstrainLength();
                simulatedPoint.ConstrainAngle();
            }
            simulatedPoint.DebugDraw(Color.black, false);
        }
    }

    private void CreateSimulatedPoints(Transform currentTransform, SimulatedPoint parentSimulatedPoint) {
        SimulatedPoint currentSimulatedPoint = new SimulatedPoint(currentTransform, parentSimulatedPoint, currentTransform.position);
        simulatedPoints.Add(currentSimulatedPoint);
        for (int i = 0; i < currentTransform.childCount; i++) {
            CreateSimulatedPoints(currentTransform.GetChild(i), currentSimulatedPoint);
        }
    }

}
