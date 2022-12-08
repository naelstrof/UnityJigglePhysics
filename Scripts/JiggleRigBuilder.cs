using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace JigglePhysics {

public class JiggleRigBuilder : MonoBehaviour {

    [System.Serializable]
    private class JiggleRig {
        [Tooltip("The root bone from which an individual JiggleRig will be constructed. The JiggleRig encompasses all children of the specified root.")]
        public Transform rootTransform;
        [Tooltip("The settings that the rig should update with, create them using the Create->JigglePhysics->Settings menu option.")]
        public JiggleSettingsBase jiggleSettings;
        [Tooltip("The list of transforms to ignore during the jiggle. Each bone listed will also ignore all the children of the specified bone.")]
        public List<Transform> ignoredTransforms;
        
        [HideInInspector]
        public List<JiggleBone> simulatedPoints;
    }
    [Tooltip("Enables interpolation for the simulation, this should be enabled unless you *really* need the simulation to only update on FixedUpdate.")]
    public bool interpolate = true;

    [SerializeField]
    private List<JiggleRig> jiggleRigs;

    [Tooltip("An air force that is applied to the entire rig, this is useful to plug in some wind volumes from external sources.")]
    public Vector3 wind;
    [Tooltip("Draws some simple lines to show what the simulation is doing. Generally this should be disabled.")]
    [SerializeField] private bool debugDraw;
    private const float smoothing = 1f;
    private Dictionary<Transform, JiggleRig> jiggleRigLookup;

    private double accumulation;
    private void Awake() {
        accumulation = 0f;
        // When created via AddComponent, jiggleRigs will be null...
        jiggleRigLookup ??= new Dictionary<Transform, JiggleRig>();
        jiggleRigLookup.Clear();
        jiggleRigs ??= new List<JiggleRig>();
        foreach(JiggleRig rig in jiggleRigs) {
            try {
                jiggleRigLookup.Add(rig.rootTransform, rig);
            } catch (ArgumentException e) {
                throw new UnityException("JiggleRig was added to transform where one already exists!");
            }
            if (rig.jiggleSettings is JiggleSettingsBlend) {
                rig.jiggleSettings = Instantiate(rig.jiggleSettings);
            }
            rig.simulatedPoints = new List<JiggleBone>();
            CreateSimulatedPoints(rig, rig.rootTransform, null);
        }
    }

    public void AddJiggleRig(Transform rootTransform, JiggleSettingsBase jiggleSettings, ICollection<Transform> ignoredTransforms = null) {
        jiggleRigLookup ??= new Dictionary<Transform, JiggleRig>();
        jiggleRigs ??= new List<JiggleRig>();
        
        JiggleRig rig = new JiggleRig() {
            rootTransform = rootTransform,
            ignoredTransforms = ignoredTransforms == null ? new List<Transform> () : new List<Transform>(ignoredTransforms),
            jiggleSettings = (jiggleSettings is JiggleSettingsBlend) ? Instantiate(jiggleSettings) : jiggleSettings,
            simulatedPoints = new List<JiggleBone>()
        };
        try {
            jiggleRigLookup.Add(rootTransform, rig);
        } catch (ArgumentException e) {
            throw new UnityException("JiggleRig was added to transform where one already exists!");
        }
        jiggleRigs.Add(rig);
        CreateSimulatedPoints(rig, rig.rootTransform, null);
    }

    public void SetJiggleSettingsNormalizedBlend(Transform targetRootTransform, float normalizedBlend) {
        if (!jiggleRigLookup.ContainsKey(targetRootTransform)) {
            throw new UnityException($"No JiggleRig was found on the bone {targetRootTransform}");
        }
        JiggleRig rig = jiggleRigLookup[targetRootTransform];
        if (rig.jiggleSettings is not JiggleSettingsBlend blend) {
            throw new UnityException($"Attempted to change normalizedBlend of JiggleRig's JiggleSettingsBlend, when the actual settings type was {rig.jiggleSettings.GetType()}");
        }
        blend.normalizedBlend = normalizedBlend;
    }

    private void LateUpdate() {
        if (!interpolate) {
            return;
        }
        
        foreach(JiggleRig rig in jiggleRigs) {
            foreach (JiggleBone simulatedPoint in rig.simulatedPoints) {
                simulatedPoint.PrepareBone();
            }
        }

        accumulation = System.Math.Min(accumulation+Time.deltaTime, Time.fixedDeltaTime*4f);
        while (accumulation > Time.fixedDeltaTime) {
            accumulation -= Time.fixedDeltaTime;
            double time = Time.timeAsDouble - accumulation;
            foreach(JiggleRig rig in jiggleRigs) {
                foreach (JiggleBone simulatedPoint in rig.simulatedPoints) { 
                    simulatedPoint.Simulate(rig.jiggleSettings, wind, time);
                }
            }
        }

        foreach (JiggleRig rig in jiggleRigs) {
            Vector3 virtualPosition = rig.simulatedPoints[0].DeriveFinalSolvePosition(Vector3.zero, smoothing);
            Vector3 offset = rig.simulatedPoints[0].transform.position - virtualPosition;
            foreach (JiggleBone simulatedPoint in rig.simulatedPoints) {
                simulatedPoint.DeriveFinalSolvePosition(offset, smoothing);
            }
        }

        foreach (JiggleRig rig in jiggleRigs) {
            foreach (JiggleBone simulatedPoint in rig.simulatedPoints) {
                simulatedPoint.PoseBone( rig.jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Blend));
                if (debugDraw) {
                    simulatedPoint.DebugDraw(Color.red, Color.blue, true);
                }
            }
        }
    }

    private void FixedUpdate() {
        if (interpolate) {
            return;
        }
        foreach(JiggleRig rig in jiggleRigs) {
            foreach (JiggleBone simulatedPoint in rig.simulatedPoints) {
                simulatedPoint.PrepareBone();
            }
        }

        foreach(JiggleRig rig in jiggleRigs) {
            foreach (JiggleBone simulatedPoint in rig.simulatedPoints) { 
                simulatedPoint.Simulate(rig.jiggleSettings, wind, Time.time);
            }
        }
        
        foreach (JiggleRig rig in jiggleRigs) {
            Vector3 virtualPosition = rig.simulatedPoints[0].DeriveFinalSolvePosition(Vector3.zero, smoothing);
            Vector3 offset = rig.simulatedPoints[0].transform.position - virtualPosition;
            foreach (JiggleBone simulatedPoint in rig.simulatedPoints) {
                simulatedPoint.DeriveFinalSolvePosition(offset, smoothing);
            }
        }

        foreach (JiggleRig rig in jiggleRigs) {
            foreach (JiggleBone simulatedPoint in rig.simulatedPoints) {
                simulatedPoint.PoseBone( rig.jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Blend));
                if (debugDraw) {
                    simulatedPoint.DebugDraw(Color.red, Color.blue, true);
                }
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
            float length = 0.1f;
            if (parentJiggleBone.parent != null) {
                length = Vector3.Distance(parentJiggleBone.transform.position, parentJiggleBone.parent.transform.position);
            }
            rig.simulatedPoints.Add(new JiggleBone(null, newJiggleBone, currentTransform.position + projectedForward*length));
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