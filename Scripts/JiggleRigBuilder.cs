using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JigglePhysics {

public class JiggleRigBuilder : MonoBehaviour {

    [Serializable]
    public class JiggleRig {
        [SerializeField][Tooltip("The root bone from which an individual JiggleRig will be constructed. The JiggleRig encompasses all children of the specified root.")]
        private Transform rootTransform;
        [Tooltip("The settings that the rig should update with, create them using the Create->JigglePhysics->Settings menu option.")]
        public JiggleSettingsBase jiggleSettings;
        [SerializeField][Tooltip("The list of transforms to ignore during the jiggle. Each bone listed will also ignore all the children of the specified bone.")]
        private List<Transform> ignoredTransforms;
        public List<Collider> colliders;
        private JiggleSettingsData data;
        
        private bool initialized;

        public Transform GetRootTransform() => rootTransform;
        public JiggleRig(Transform rootTransform, JiggleSettingsBase jiggleSettings,
            ICollection<Transform> ignoredTransforms) {
            this.rootTransform = rootTransform;
            this.jiggleSettings = jiggleSettings;
            this.ignoredTransforms = new List<Transform>(ignoredTransforms);
            Initialize();
        }

        [HideInInspector]
        private List<JiggleBone> simulatedPoints;

        public void PrepareBone() {
            if (!initialized) {
                throw new UnityException( "JiggleRig was never initialized. Please call JiggleRig.Initialize() if you're going to manually timestep.");
            }

            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.PrepareBone();
            }

            data = jiggleSettings.GetData();
        }

        public void FirstPass(Vector3 wind, double time) {
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.FirstPass(data, wind, time);
            }
        }
        public void SecondPass() {
            for (int i=simulatedPoints.Count-1;i>=0;i--) {
                simulatedPoints[i].SecondPass(data);
            }
        }
        public void ThirdPass() {
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.ThirdPass(data);
            }
        }
        
        public void FinalPass(double time) {
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.FinalPass(jiggleSettings, time, colliders);
            }
        }

        public void Initialize() {
            simulatedPoints = new List<JiggleBone>();
            if (rootTransform == null) {
                return;
            }

            CreateSimulatedPoints(simulatedPoints, ignoredTransforms, rootTransform, null);
            foreach (var simulatedPoint in simulatedPoints) {
                simulatedPoint.CalculateNormalizedIndex();
            }
            initialized = true;
        }

        public void DeriveFinalSolve() {
            Vector3 virtualPosition = simulatedPoints[0].DeriveFinalSolvePosition(Vector3.zero, smoothing);
            Vector3 offset = simulatedPoints[0].transform.position - virtualPosition;
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.DeriveFinalSolvePosition(offset, smoothing);
            }
        }

        public void Pose(bool debugDraw) {
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.PoseBone( jiggleSettings.GetData().blend);
                if (debugDraw) {
                    simulatedPoint.DebugDraw(Color.red, Color.blue, true);
                }
            }
        }

        public void PrepareTeleport() {
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.PrepareTeleport();
            }
        }

        public void FinishTeleport() {
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.FinishTeleport();
            }
        }

        public void OnDrawGizmos() {
            if (!initialized || simulatedPoints == null) {
                Initialize();
            }
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.OnDrawGizmos(jiggleSettings);
            }
        }

        private static void CreateSimulatedPoints(ICollection<JiggleBone> outputPoints, ICollection<Transform> ignoredTransforms, Transform currentTransform, JiggleBone parentJiggleBone) {
            JiggleBone newJiggleBone = new JiggleBone(currentTransform, parentJiggleBone, currentTransform.position);
            outputPoints.Add(newJiggleBone);
            // Create an extra purely virtual point if we have no children.
            if (currentTransform.childCount == 0) {
                if (newJiggleBone.parent == null) {
                    if (newJiggleBone.transform.parent == null) {
                        throw new UnityException("Can't have a singular jiggle bone with no parents. That doesn't even make sense!");
                    } else {
                        float lengthToParent = Vector3.Distance(currentTransform.position, newJiggleBone.transform.parent.position);
                        Vector3 projectedForwardReal = (currentTransform.position - newJiggleBone.transform.parent.position).normalized;
                        outputPoints.Add(new JiggleBone(null, newJiggleBone, currentTransform.position + projectedForwardReal*lengthToParent));
                        return;
                    }
                }
                Vector3 projectedForward = (currentTransform.position - parentJiggleBone.transform.position).normalized;
                float length = 0.1f;
                if (parentJiggleBone.parent != null) {
                    length = Vector3.Distance(parentJiggleBone.transform.position, parentJiggleBone.parent.transform.position);
                }
                outputPoints.Add(new JiggleBone(null, newJiggleBone, currentTransform.position + projectedForward*length));
                return;
            }
            for (int i = 0; i < currentTransform.childCount; i++) {
                if (ignoredTransforms.Contains(currentTransform.GetChild(i))) {
                    continue;
                }
                CreateSimulatedPoints(outputPoints, ignoredTransforms, currentTransform.GetChild(i), newJiggleBone);
            }
        }
    }
    [Tooltip("Enables interpolation for the simulation, this should be enabled unless you *really* need the simulation to only update on FixedUpdate.")]
    public bool interpolate = true;

    public List<JiggleRig> jiggleRigs;

    [Tooltip("An air force that is applied to the entire rig, this is useful to plug in some wind volumes from external sources.")]
    public Vector3 wind;
    [Tooltip("Draws some simple lines to show what the simulation is doing. Generally this should be disabled.")]
    [SerializeField] private bool debugDraw;
    private const float smoothing = 1f;

    private double accumulation;
    private void Awake() {
        Initialize();
    }

    public void Initialize() {
        accumulation = 0f;
        jiggleRigs ??= new List<JiggleRig>();
        foreach(JiggleRig rig in jiggleRigs) {
            rig.Initialize();
        }
    }

    public void Advance(float deltaTime) {
        foreach(JiggleRig rig in jiggleRigs) {
            rig.PrepareBone();
        }

        accumulation = Math.Min(accumulation+deltaTime, Time.fixedDeltaTime*4f);
        while (accumulation > Time.fixedDeltaTime) {
            accumulation -= Time.fixedDeltaTime;
            double time = Time.timeAsDouble - accumulation;
            foreach(JiggleRig rig in jiggleRigs) {
                rig.FirstPass(wind, time);
            }
            foreach (JiggleRig rig in jiggleRigs) {
                rig.SecondPass();
            }
            foreach (JiggleRig rig in jiggleRigs) {
                rig.ThirdPass();
            }
            foreach (JiggleRig rig in jiggleRigs) {
                rig.FinalPass(time);
            }
        }

        foreach (JiggleRig rig in jiggleRigs) {
            rig.DeriveFinalSolve();
        }

        foreach (JiggleRig rig in jiggleRigs) {
            rig.Pose(debugDraw);
        }
    }

    public JiggleRig GetJiggleRig(Transform rootTransform) {
        foreach (var rig in jiggleRigs) {
            if (rig.GetRootTransform() == rootTransform) {
                return rig;
            }
        }
        return null;
    }
    private void LateUpdate() {
        if (!interpolate) {
            return;
        }
        Advance(Time.deltaTime);
    }

    private void FixedUpdate() {
        if (interpolate) {
            return;
        }
        Advance(Time.deltaTime);
    }

    public void PrepareTeleport() {
        foreach (JiggleRig rig in jiggleRigs) {
            rig.PrepareTeleport();
        }
    }
    
    public void FinishTeleport() {
        foreach (JiggleRig rig in jiggleRigs) {
            rig.FinishTeleport();
        }
    }

    private void OnDrawGizmos() {
        if (jiggleRigs == null) {
            return;
        }
        foreach (var rig in jiggleRigs) {
            rig.OnDrawGizmos();
        }
    }

    private void OnValidate() {
        if (Application.isPlaying) return;
        foreach (JiggleRig rig in jiggleRigs) {
            rig.Initialize();
        }
    }
}

}