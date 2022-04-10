using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

public class JiggleRigBuilder : MonoBehaviour {
    [System.Serializable]
    public class JiggleRig {
        [Tooltip("The root bone from which an individual JiggleRig will be constructed. The JiggleRig encompasses all children of the specified root.")]
        public Transform rootTransform;
        [Tooltip("The settings that the rig should update with, create them using the Create->JigglePhysics->Settings menu option.")]
        public JiggleSettingsBase jiggleSettings;
        [Tooltip("The list of transforms to ignore during the jiggle. Each bone listed will also ignore all the children of the specified bone.")]
        public List<Transform> ignoredTransforms;

        [HideInInspector]
        public List<JiggleBone> simulatedPoints;
    }
    [SerializeField] [Tooltip("Enables interpolation for the simulation, this should be enabled unless you *really* need the simulation to only update on FixedUpdate.")]
    private bool interpolate = true;
    public List<JiggleRig> jiggleRigs;
    [Tooltip("Draws some simple lines to show what the simulation is doing. Generally this should be disabled.")]
    [SerializeField] private bool debugDraw;

    private void Awake() {
        foreach(JiggleRig rig in jiggleRigs) {
            rig.simulatedPoints = new List<JiggleBone>();
            CreateSimulatedPoints(rig, rig.rootTransform, null);
        }
    }
    private void LateUpdate() {
        foreach(JiggleRig rig in jiggleRigs) {
            foreach (JiggleBone simulatedPoint in rig.simulatedPoints) {
                simulatedPoint.PrepareBone(interpolate);
            }
            foreach (JiggleBone simulatedPoint in rig.simulatedPoints) {
                simulatedPoint.PoseBone(rig.jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Blend));
                if (debugDraw) {
                    simulatedPoint.DebugDraw(Color.red, true);
                    //simulatedPoint.DebugDraw(Color.black, false);
                }
            }
        }
    }

    private void FixedUpdate() {
        foreach(JiggleRig rig in jiggleRigs) {
            foreach (JiggleBone simulatedPoint in rig.simulatedPoints) { 
                simulatedPoint.Simulate(rig.jiggleSettings, rig.simulatedPoints[0]);
            }
        }
    }

    private void CreateSimulatedPoints(JiggleRig rig, Transform currentTransform, JiggleBone parentJiggleBone) {
        JiggleBone newJiggleBone = new JiggleBone(currentTransform, parentJiggleBone, currentTransform.position);
        rig.simulatedPoints.Add(newJiggleBone);
        // Create an extra purely virtual point if we have no children.
        if (currentTransform.childCount == 0) {
            if (newJiggleBone.parent == null) {
                if (newJiggleBone.transform.parent == null) {
                    throw new UnityException("Can't have a singular jiggle bone with no parents. That doesn't even make sense!");
                } else {
                    float lengthToParent = Vector3.Distance(currentTransform.position, newJiggleBone.transform.parent.position);
                    Vector3 projectedForwardReal = (currentTransform.position - newJiggleBone.transform.parent.position).normalized;
                    rig.simulatedPoints.Add(new JiggleBone(null, newJiggleBone, currentTransform.position + projectedForwardReal*lengthToParent));
                    return;
                }
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

}