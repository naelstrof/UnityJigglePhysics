using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace JigglePhysics {
    
public class JiggleRigBuilder : MonoBehaviour, IJiggleAdvancable {
    public const float VERLET_TIME_STEP = 0.02f;
    public const float MAX_CATCHUP_TIME = VERLET_TIME_STEP*4f;
    internal const float SETTLE_TIME = 0.2f;

    [Serializable]
    public class JiggleRig {
        [SerializeField][Tooltip("The root bone from which an individual JiggleRig will be constructed. The JiggleRig encompasses all children of the specified root.")][FormerlySerializedAs("target")]
        private Transform rootTransform;
        [Tooltip("The settings that the rig should update with, create them using the Create->JigglePhysics->Settings menu option.")]
        public JiggleSettingsBase jiggleSettings;
        [SerializeField][Tooltip("The list of transforms to ignore during the jiggle. Each bone listed will also ignore all the children of the specified bone.")]
        private List<Transform> ignoredTransforms;
        public List<Collider> colliders;

        private bool initialized => simulatedPoints != null;

        public Transform GetRootTransform() => rootTransform;
        
        /// <summary>
        /// Constructs a realtime JiggleRig
        /// </summary>
        /// <param name="rootTransform">The root transform of the rig</param>
        /// <param name="jiggleSettings">Which jiggle settings to use. You can instantiate one and use SetData() to make one on the fly.</param>
        /// <param name="ignoredTransforms">Transforms to be ignored when constructing the jiggle tree.</param>
        /// <param name="colliders">Colliders that the jiggle rig should collide with.</param>
        public JiggleRig(Transform rootTransform, JiggleSettingsBase jiggleSettings,
            ICollection<Transform> ignoredTransforms, ICollection<Collider> colliders) {
            // TODO: This class should handle changing settings at runtime (including instantiating only if needed) (follow sharedMaterial functionality)
            this.rootTransform = rootTransform;
            this.jiggleSettings = jiggleSettings;
            this.ignoredTransforms = new List<Transform>(ignoredTransforms);
            this.colliders = new List<Collider>(colliders);
            Initialize();
        }

        private bool NeedsCollisions => colliders.Count != 0;

        [HideInInspector]
        protected List<JiggleBone> simulatedPoints;

        /// <summary>
        /// Matches the particle signal to the current pose, then undoes the pose such that it doesn't permanently deform the jiggle system. This is useful for confining the jiggles, like they got "grabbed".
        /// I would essentially use IK to set the bones to be grabbed. Then call this function so the virtual jiggle particles also move to the same location.
        /// It would need to be called every frame that the chain is grabbed.
        /// </summary>
        public void SampleAndReset() {
            for (int i = simulatedPoints.Count - 1; i >= 0; i--) {
                simulatedPoints[i].SampleAndReset();
            }
        }

        /// <summary>
        /// Samples the current pose for use later in stepping the simulation. This should be called fairly regularly. It creates the desired "target pose" that the simulation tries to match.
        /// </summary>
        public void ApplyValidPoseThenSampleTargetPose() {
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.ApplyValidPoseThenSampleTargetPose();
            }
        }
        

        /// <summary>
        /// Consumes time into "ticks", from which a verlet simulation is done to simulate the jiggles. Ensure that you've called SampleTargetPose and ApplyTargetPose before this function.
        /// Use Pose to actually display the simulation.
        /// </summary>
        /// <param name="rigPosition">The general position of the rig, used for LOD swapping.</param>
        /// <param name="jiggleRigLOD">The JiggleRigLOD object used for disabling the rig if its too far. Can be null.</param>
        /// <param name="wind">How much wind to apply to every particle.</param>
        /// <param name="time">How much time to simulate forward by.</param>
        public void StepSimulation(Vector3 rigPosition, JiggleRigLOD jiggleRigLOD, Vector3 wind, double time) {
            Assert.IsTrue(initialized, "JiggleRig was never initialized. Please call JiggleRig.Initialize() if you're going to manually timestep.");

            var data = jiggleSettings.GetData();
            data = jiggleRigLOD ? jiggleRigLOD.AdjustJiggleSettingsData(rigPosition, data):data;
            
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.VerletPass(data, wind, time);
            }

            if (NeedsCollisions) {
                for (int i = simulatedPoints.Count - 1; i >= 0; i--) {
                    simulatedPoints[i].CollisionPreparePass(data);
                }
            }
            
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.SecondPass(jiggleSettings, data, colliders, time);
            }
        }

        /// <summary>
        /// Creates the virtual particle tree that is used to simulate the jiggles!
        /// </summary>
        public void Initialize() {
            simulatedPoints = new List<JiggleBone>();
            if (rootTransform == null) {
                return;
            }

            CreateSimulatedPoints(simulatedPoints, ignoredTransforms, rootTransform, null);
            foreach (var simulatedPoint in simulatedPoints) {
                simulatedPoint.CalculateNormalizedIndex();
            }
        }

        /// <summary>
        /// Calculates where the virtual particles would be at this exact moment in time (the latest simulation state is in the past and must be extrapolated).
        /// You normally won't call this.
        /// </summary>
        internal void DeriveFinalSolve() {
            Vector3 offset = simulatedPoints[0].DeriveFinalSolvePosition(Vector3.zero) - simulatedPoints[0].transform.position;
            for (int i = 0; i < simulatedPoints.Count; i++) {
                simulatedPoints[i].DeriveFinalSolvePosition(-offset);
            }
        }

        /// <summary>
        /// Attempts to pose the rig to the virtual particles. The current pose MUST be the last valid pose (set by ApplyValidPoseThenSampleTargetPose usually)
        /// </summary>
        /// <param name="debugDraw">If we should draw the target pose (blue) compared to the virtual particle pose (red).</param>
        public void Pose(bool debugDraw) {
            DeriveFinalSolve();
            var blend = jiggleSettings.GetData().blend;
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.PoseBone(blend);
                if (debugDraw) {
                    simulatedPoint.DebugDraw(Color.red, Color.blue, true);
                }
            }
        }

        /// <summary>
        /// Saves the current state of the particles and animations in preparation for a teleport. Move the rig, then do FinishTeleport().
        /// </summary>
        public void PrepareTeleport() {
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.PrepareTeleport();
            }
        }

        /// <summary>
        /// Offsets the jiggle signals from the position set with PrepareTeleport to the current position. This prevents jiggles from freaking out from a large movement.
        /// </summary>
        public void FinishTeleport() {
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.FinishTeleport();
            }
        }

        public void OnDrawGizmos() {
            if (!initialized) {
                Initialize();
            }

            simulatedPoints[0].OnDrawGizmos(jiggleSettings, true);
            for (int i = 1; i < simulatedPoints.Count; i++) {
                simulatedPoints[i].OnDrawGizmos(jiggleSettings);
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
    }
    [Tooltip("Enables interpolation for the simulation, this should be set to LateUpdate unless you *really* need the simulation to only update on FixedUpdate.")]
    [SerializeField]
    private JiggleUpdateMode jiggleUpdateMode = JiggleUpdateMode.LateUpdate;

    public List<JiggleRig> jiggleRigs;

    [Tooltip("An air force that is applied to the entire rig, this is useful to plug in some wind volumes from external sources.")]
    public Vector3 wind;
    [Tooltip("Level of detail manager. This system will control how the jiggle rig saves performance cost.")]
    [SerializeField]
    private JiggleRigLOD levelOfDetail;
    private bool hasLevelOfDetail;
    
    [Tooltip("Draws some simple lines to show what the simulation is doing. Generally this should be disabled.")]
    [SerializeField] private bool debugDraw;

    public void SetJiggleRigLOD(JiggleRigLOD lod) {
        levelOfDetail = lod;
        hasLevelOfDetail = levelOfDetail;
    }
    public void SetJiggleUpdateMode(JiggleUpdateMode mode) {
        switch (jiggleUpdateMode) {
            case JiggleUpdateMode.LateUpdate: JiggleRigLateUpdateHandler.RemoveJiggleRigAdvancable(this); break;
            case JiggleUpdateMode.FixedUpdate: JiggleRigFixedUpdateHandler.RemoveJiggleRigAdvancable(this); break;
            default: throw new ArgumentOutOfRangeException();
        }
        jiggleUpdateMode = mode;
        switch (jiggleUpdateMode) {
            case JiggleUpdateMode.LateUpdate: JiggleRigLateUpdateHandler.AddJiggleRigAdvancable(this); break;
            case JiggleUpdateMode.FixedUpdate: JiggleRigFixedUpdateHandler.AddJiggleRigAdvancable(this); break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
    
    private double accumulation;
    private float settleTimer;
    private bool wasLODActive = true;
    private void Awake() {
        Initialize();
    }
    void OnEnable() {
        switch (jiggleUpdateMode) {
            case JiggleUpdateMode.LateUpdate: JiggleRigLateUpdateHandler.AddJiggleRigAdvancable(this); break;
            case JiggleUpdateMode.FixedUpdate: JiggleRigFixedUpdateHandler.AddJiggleRigAdvancable(this); break;
            default: throw new ArgumentOutOfRangeException();
        }

        if (settleTimer > SETTLE_TIME) {
            FinishTeleport();
        }
    }
    void OnDisable() {
        switch (jiggleUpdateMode) {
            case JiggleUpdateMode.LateUpdate: JiggleRigLateUpdateHandler.RemoveJiggleRigAdvancable(this); break;
            case JiggleUpdateMode.FixedUpdate: JiggleRigFixedUpdateHandler.RemoveJiggleRigAdvancable(this); break;
            default: throw new ArgumentOutOfRangeException();
        }
        PrepareTeleport();
    }

    public void Initialize() {
        accumulation = 0f;
        jiggleRigs ??= new List<JiggleRig>();
        foreach(JiggleRig rig in jiggleRigs) {
            rig.Initialize();
        }
        hasLevelOfDetail = levelOfDetail;
        settleTimer = 0f;
    }

    public JiggleUpdateMode GetJiggleUpdateMode() {
        return jiggleUpdateMode;
    }

    public virtual void Advance(float deltaTime) {
        #region Settling on spawn, to prevent instant posing jiggles.

        if (settleTimer < SETTLE_TIME) {
            settleTimer += deltaTime;
            if (settleTimer >= SETTLE_TIME) {
                FinishTeleport();
            }
            return;
        }

        #endregion

        #region Level of detail handling

        if (hasLevelOfDetail && !levelOfDetail.CheckActive(transform.position)) {
            if (wasLODActive) PrepareTeleport();
            wasLODActive = false;
            return;
        }
        if (!wasLODActive) FinishTeleport();

        #endregion

        foreach (JiggleRig rig in jiggleRigs) {
            rig.ApplyValidPoseThenSampleTargetPose();
        }
        accumulation = Math.Min(accumulation+deltaTime, MAX_CATCHUP_TIME);
        var position = transform.position;
        while (accumulation > VERLET_TIME_STEP) {
            accumulation -= VERLET_TIME_STEP;
            double time = Time.timeAsDouble - accumulation;
            foreach(JiggleRig rig in jiggleRigs) {
                rig.StepSimulation(position, levelOfDetail, wind, time);
            }
        }
        
        foreach (JiggleRig rig in jiggleRigs) {
            rig.Pose(debugDraw);
        }
        wasLODActive = true;
    }

    public JiggleRig GetJiggleRig(Transform rootTransform) {
        foreach (var rig in jiggleRigs) {
            if (rig.GetRootTransform() == rootTransform) {
                return rig;
            }
        }
        return null;
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
        if (jiggleRigs == null || !enabled) {
            return;
        }
        foreach (var rig in jiggleRigs) {
            rig.OnDrawGizmos();
        }
    }

    private void OnValidate() {
        if (Application.isPlaying) {
            JiggleRigLateUpdateHandler.RemoveJiggleRigAdvancable(this);
            JiggleRigFixedUpdateHandler.RemoveJiggleRigAdvancable(this);
            switch (jiggleUpdateMode) {
                case JiggleUpdateMode.LateUpdate: JiggleRigLateUpdateHandler.AddJiggleRigAdvancable(this); break;
                case JiggleUpdateMode.FixedUpdate: JiggleRigFixedUpdateHandler.AddJiggleRigAdvancable(this); break;
                default: throw new ArgumentOutOfRangeException();
            }
            SetJiggleRigLOD(levelOfDetail);
        } else {
            if (jiggleRigs == null) return;
            foreach (JiggleRig rig in jiggleRigs) {
                rig.Initialize();
            }
        }
    }
}

}