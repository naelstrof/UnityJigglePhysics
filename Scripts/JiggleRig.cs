using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JiggleRig : MonoBehaviour {

    [SerializeField]
    private List<Transform> ignoredTransforms;
    [SerializeField]
    private JiggleSettings jiggleSettings;
    private List<JiggleBone> simulatedPoints;

    private void Awake() {
        simulatedPoints = new List<JiggleBone>();
        CreateSimulatedPoints(transform, null);
    }
    private void LateUpdate() {
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.PrepareBone();
        }
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.PoseBone(jiggleSettings.blend);
        }
    }

    private void FixedUpdate() {
        foreach (JiggleBone simulatedPoint in simulatedPoints) { 
            simulatedPoint.Simulate(jiggleSettings);
            //simulatedPoint.DebugDraw(Color.black, false);
        }
    }

    private void CreateSimulatedPoints(Transform currentTransform, JiggleBone parentJiggleBone) {
        JiggleBone newJiggleBone = new JiggleBone(currentTransform, parentJiggleBone, currentTransform.position);
        simulatedPoints.Add(newJiggleBone);
        // Create an extra purely virtual point if we have no children.
        if (currentTransform.childCount == 0) {
            if (newJiggleBone.parent == null) {
                throw new UnityException("Can't have a singular jiggle bone with no parents. That doesn't even make sense!");
            }
            Vector3 projectedForward = (currentTransform.position - parentJiggleBone.transform.position).normalized;
            simulatedPoints.Add(new JiggleBone(null, newJiggleBone, currentTransform.position + projectedForward*parentJiggleBone.lengthToParent));
            return;
        }
        for (int i = 0; i < currentTransform.childCount; i++) {
            if (ignoredTransforms.Contains(currentTransform.GetChild(i))) {
                continue;
            }
            CreateSimulatedPoints(currentTransform.GetChild(i), newJiggleBone);
        }
    }

}
