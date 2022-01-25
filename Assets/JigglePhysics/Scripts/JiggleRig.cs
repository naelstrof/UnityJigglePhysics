using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JiggleRig : MonoBehaviour {

    private Transform rootTransform;
    private List<SimulatedPoint> simulatedPoints;

    private void Awake() {
        simulatedPoints = new List<SimulatedPoint>();
        CreateSimulatedPoints(transform, null);
    }

    private void FixedUpdate() {
        foreach (SimulatedPoint simulatedPoint in simulatedPoints) {
            simulatedPoint.StepPhysics(Time.deltaTime);
            simulatedPoint.DebugDraw();
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
