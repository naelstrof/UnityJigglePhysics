using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JiggleRig : MonoBehaviour {

    private List<SimulatedPoint> simulatedPoints;

    private void Awake() {
        simulatedPoints = new List<SimulatedPoint>();
        CreateSimulatedPoints(transform, null);
    }
    private void Update() {
        foreach (SimulatedPoint simulatedPoint in simulatedPoints) {
            simulatedPoint.DebugDraw(Color.green, true);
        }
    }

    private void FixedUpdate() {
        foreach (SimulatedPoint simulatedPoint in simulatedPoints) {
            if (simulatedPoint.parent == null) {
                simulatedPoint.SnapTo(transform);
            } else {
                simulatedPoint.StepPhysics(Time.deltaTime);
                simulatedPoint.ConstrainLength();
            }
            simulatedPoint.DebugDraw(Color.black, false);
        }
    }

    private void CreateSimulatedPoints(Transform currentTransform, SimulatedPoint parentSimulatedPoint) {
        SimulatedPoint currentSimulatedPoint = new SimulatedPoint(parentSimulatedPoint, currentTransform.position);
        simulatedPoints.Add(currentSimulatedPoint);
        for (int i = 0; i < currentTransform.childCount; i++) {
            CreateSimulatedPoints(currentTransform.GetChild(i), currentSimulatedPoint);
        }
    }

}
