using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace JigglePhysics {
    
[DefaultExecutionOrder(200)]
public class JiggleRigBuilder : MonoBehaviour {
    public static float maxCatchupTime => Time.fixedDeltaTime*4;

    //[Tooltip("Enables interpolation for the simulation, this should be enabled unless you *really* need the simulation to only update on FixedUpdate.")]
    //public bool interpolate = true;
    [SerializeField][Tooltip("The root bone from which an individual JiggleRig will be constructed. The JiggleRig encompasses all children of the specified root.")][FormerlySerializedAs("target")]
    private Transform rootTransform;
    [Tooltip("The settings that the rig should update with, create them using the Create->JigglePhysics->Settings menu option.")]
    public JiggleSettingsBase jiggleSettings;
    [SerializeField][Tooltip("The list of transforms to ignore during the jiggle. Each bone listed will also ignore all the children of the specified bone.")]
    private List<Transform> ignoredTransforms;
    public List<Collider> colliders;

    [Tooltip("An air force that is applied to the entire rig, this is useful to plug in some wind volumes from external sources.")]
    public Vector3 wind;
    [Tooltip("Level of detail manager. This system will control how the jiggle rig saves performance cost.")]
    public JiggleRigLOD levelOfDetail;
    [Tooltip("Draws some simple lines to show what the simulation is doing. Generally this should be disabled.")]
    [SerializeField] private bool debugDraw;
    private JiggleSettingsData data;
    
    private bool initialized;

    public Transform GetRootTransform() => rootTransform;

    private bool NeedsCollisions => colliders.Count != 0;

    [HideInInspector]
    protected List<JiggleBone> simulatedPoints;

    public void PrepareBone(Vector3 position, JiggleRigLOD jiggleRigLOD) {
        if (!initialized) {
            throw new UnityException( "JiggleRig was never initialized. Please call JiggleRig.Initialize() if you're going to manually timestep.");
        }

        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.PrepareBone();
        }

        data = jiggleSettings.GetData();
        data = jiggleRigLOD!=null?jiggleRigLOD.AdjustJiggleSettingsData(position, data):data;
    }

    public void MatchAnimationInstantly() {
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.MatchAnimationInstantly();
        }
    }

    public void UpdateJiggle(Vector3 wind, double time) {
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.VerletPass(data, wind, time);
        }

        if (NeedsCollisions) {
            for (int i = simulatedPoints.Count - 1; i >= 0; i--) {
                simulatedPoints[i].CollisionPreparePass(data);
            }
        }
        
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.ConstraintPass(data);
        }
        
        if (NeedsCollisions) {
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.CollisionPass(jiggleSettings, colliders);
            }
        }
        
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.SignalWritePosition(time);
        }
    }


    public void DeriveFinalSolve() {
        Vector3 virtualPosition = simulatedPoints[0].DeriveFinalSolvePosition(Vector3.zero);
        Vector3 offset = simulatedPoints[0].transform.position - virtualPosition;
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.DeriveFinalSolvePosition(offset);
        }
    }

    public void Pose(bool debugDraw) {
        DeriveFinalSolve();
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.PoseBone(data.blend);
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

    protected virtual void CreateSimulatedPoints(ICollection<JiggleBone> outputPoints, ICollection<Transform> ignoredTransforms, Transform currentTransform, JiggleBone parentJiggleBone) {
        JiggleBone newJiggleBone = new JiggleBone(currentTransform, parentJiggleBone);
        outputPoints.Add(newJiggleBone);
        // Create an extra purely virtual point if we have no children.
        if (currentTransform.childCount == 0) {
            if (newJiggleBone.parent == null) {
                if (newJiggleBone.transform.parent == null) {
                    throw new UnityException("Can't have a singular jiggle bone with no parents. That doesn't even make sense!");
                } else {
                    outputPoints.Add(new JiggleBone(null, newJiggleBone));
                    return;
                }
            }
            outputPoints.Add(new JiggleBone(null, newJiggleBone));
            return;
        }
        for (int i = 0; i < currentTransform.childCount; i++) {
            if (ignoredTransforms.Contains(currentTransform.GetChild(i))) {
                continue;
            }
            CreateSimulatedPoints(outputPoints, ignoredTransforms, currentTransform.GetChild(i), newJiggleBone);
        }
    }

    private double accumulation;
    private bool dirtyFromEnable = false;
    private bool wasLODActive = true;
    private void Awake() {
        Initialize();
    }
    void OnEnable() {
        JiggleRigHandler.AddBuilder(this);
        dirtyFromEnable = true;
    }
    void OnDisable() {
        JiggleRigHandler.RemoveBuilder(this);
        PrepareTeleport();
    }

    public void Initialize() {
        accumulation = 0f;
        //jiggleRigs ??= new List<JiggleRig>();
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

    public virtual void Advance(float deltaTime) {
        if (levelOfDetail!=null && !levelOfDetail.CheckActive(transform.position)) {
            if (wasLODActive) PrepareTeleport();
            wasLODActive = false;
            return;
        }
        if (!wasLODActive) FinishTeleport();
        PrepareBone(transform.position, levelOfDetail);
        
        if (dirtyFromEnable) {
            FinishTeleport();
            dirtyFromEnable = false;
        }

        accumulation = Math.Min(accumulation+deltaTime, maxCatchupTime);
        while (accumulation > Time.fixedDeltaTime) {
            accumulation -= Time.fixedDeltaTime;
            double time = Time.timeAsDouble - accumulation;
            UpdateJiggle(wind, time);
        }
        
        Pose(debugDraw);
        wasLODActive = true;
    }

    //private void LateUpdate() {
        //if (!interpolate) {
            //return;
        //}
        //Advance(Time.deltaTime);
    //}

    private void OnDrawGizmos() {
        if (!initialized || simulatedPoints == null) {
            Initialize();
        }
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.OnDrawGizmos(jiggleSettings);
        }
    }

    private void OnValidate() {
        if (rootTransform == null) {
            rootTransform = transform;
        }
        if (Application.isPlaying) return;
        Initialize();
    }
}

}