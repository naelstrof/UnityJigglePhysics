using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JiggleRigBuilder : MonoBehaviour {
    [System.Serializable]
    public class JiggleRig {
        public Transform rootTransform;
        public JiggleSettings jiggleSettings;
        public List<Transform> ignoredTransforms;

        [HideInInspector]
        public List<JiggleBone> simulatedPoints;
    }
    public List<JiggleRig> jiggleRigs;

    private void Awake() {
        foreach(JiggleRig rig in jiggleRigs) {
            rig.simulatedPoints = new List<JiggleBone>();
            CreateSimulatedPoints(rig, rig.rootTransform, null);
        }
    }
    private void LateUpdate() {
        foreach(JiggleRig rig in jiggleRigs) {
            foreach (JiggleBone simulatedPoint in rig.simulatedPoints) {
                simulatedPoint.PrepareBone();
            }
            foreach (JiggleBone simulatedPoint in rig.simulatedPoints) {
                simulatedPoint.PoseBone(rig.jiggleSettings.blend);
                //simulatedPoint.DebugDraw(Color.green, true);
            }
        }
    }

    private void FixedUpdate() {
        foreach(JiggleRig rig in jiggleRigs) {
            foreach (JiggleBone simulatedPoint in rig.simulatedPoints) { 
                simulatedPoint.Simulate(rig.jiggleSettings);
            }
        }
    }

    private void CreateSimulatedPoints(JiggleRig rig, Transform currentTransform, JiggleBone parentJiggleBone) {
        JiggleBone newJiggleBone = new JiggleBone(currentTransform, parentJiggleBone, currentTransform.position);
        rig.simulatedPoints.Add(newJiggleBone);
        // Create an extra purely virtual point if we have no children.
        if (currentTransform.childCount == 0) {
            if (newJiggleBone.parent == null) {
                throw new UnityException("Can't have a singular jiggle bone with no parents. That doesn't even make sense!");
            }
            Vector3 projectedForward = (currentTransform.position - parentJiggleBone.transform.position).normalized;
            rig.simulatedPoints.Add(new JiggleBone(null, newJiggleBone, currentTransform.position + projectedForward*parentJiggleBone.lengthToParent));
            return;
        }
        for (int i = 0; i < currentTransform.childCount; i++) {
            if (rig.ignoredTransforms.Contains(currentTransform.GetChild(i))) {
                continue;
            }
            CreateSimulatedPoints(rig,currentTransform.GetChild(i), newJiggleBone);
        }
    }
}
