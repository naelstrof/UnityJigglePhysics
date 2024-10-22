using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace JigglePhysics {
    
public class JiggleRigBuilder : MonoBehaviour, IJiggleAdvancable {
    public const float VERLET_TIME_STEP = 0.02f;
    public const float MAX_CATCHUP_TIME = VERLET_TIME_STEP*4f;
    internal const float SETTLE_TIME = 0.2f;
    private const float SQUARED_VERLET_TIME_STEP = VERLET_TIME_STEP * VERLET_TIME_STEP;

    [Serializable]
    public class JiggleRig {
        [SerializeField][Tooltip("The root bone from which an individual JiggleRig will be constructed. The JiggleRig encompasses all children of the specified root.")][FormerlySerializedAs("target")]
        private Transform rootTransform;
        [Tooltip("The settings that the rig should update with, create them using the Create->JigglePhysics->Settings menu option.")]
        public JiggleSettingsBase jiggleSettings;
        [SerializeField][Tooltip("The list of transforms to ignore during the jiggle. Each bone listed will also ignore all the children of the specified bone.")]
        private List<Transform> ignoredTransforms;
        public List<Collider> colliders;
        private int boneCount;

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
        protected JiggleBone[] simulatedPoints;

        /// <summary>
        /// Matches the particle signal to the current pose, then undoes the pose such that it doesn't permanently deform the jiggle system. This is useful for confining the jiggles, like they got "grabbed".
        /// I would essentially use IK to set the bones to be grabbed. Then call this function so the virtual jiggle particles also move to the same location.
        /// It would need to be called every frame that the chain is grabbed.
        /// </summary>
        public void SampleAndReset() {
            for (int i = boneCount - 1; i >= 0; i--) {
                var simulatedPoint = simulatedPoints[i];
                simulatedPoint.SampleAndReset(simulatedPoints);
                simulatedPoints[i] = simulatedPoint;
            }
        }

        /// <summary>
        /// Samples the current pose for use later in stepping the simulation. This should be called fairly regularly. It creates the desired "target pose" that the simulation tries to match.
        /// </summary>
        public void ApplyValidPoseThenSampleTargetPose(double timeAsDouble) {
            for(int i=0;i<boneCount;i++) {
                var simulatedPoint = simulatedPoints[i];
                simulatedPoint.ApplyValidPoseThenSampleTargetPose(simulatedPoints, timeAsDouble);
                simulatedPoints[i] = simulatedPoint;
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
        public void StepSimulation(Vector3 rigPosition, JiggleRigLOD jiggleRigLOD, Vector3 wind, double time, Vector3 gravity) {
            //Assert.IsTrue(initialized, "JiggleRig was never initialized. Please call JiggleRig.Initialize() if you're going to manually timestep.");

            var data = jiggleRigLOD ? jiggleRigLOD.AdjustJiggleSettingsData(rigPosition, jiggleSettings.GetData()) : jiggleSettings.GetData();

            float squaredAngleElasticity = data.angleElasticity * data.angleElasticity;
            float squaredLengthElasticity = data.lengthElasticity * data.lengthElasticity;
            Vector3 acceleration = gravity * (data.gravityMultiplier * SQUARED_VERLET_TIME_STEP) + wind * (VERLET_TIME_STEP * data.airDrag);
            float airDrag = 1f - data.airDrag;
            float friction = 1f - data.friction;

            for (int i = 0; i < boneCount; i++) {
                var simulatedPoint = simulatedPoints[i];
                
                #region VerletIntegrate
                simulatedPoint.currentFixedAnimatedBonePosition = simulatedPoint.targetAnimatedBoneSignal.SamplePosition(time);
                if (simulatedPoint.parentID == null) {
                    simulatedPoint.workingPosition = simulatedPoint.currentFixedAnimatedBonePosition;
                    simulatedPoint.particleSignal.SetPosition(simulatedPoint.workingPosition, time);
                    simulatedPoints[i] = simulatedPoint;
                    continue;
                }

                var parentSimulatedPoint = simulatedPoints[simulatedPoint.parentID.Value];
                var lengthToParent = simulatedPoint.cachedLengthToParent;
                Vector3 newPosition = simulatedPoint.particleSignal.GetCurrent();
                Vector3 delta = newPosition - simulatedPoint.particleSignal.GetPrevious();
                Vector3 localSpaceVelocity = delta - (parentSimulatedPoint.particleSignal.GetCurrent() - parentSimulatedPoint.particleSignal.GetPrevious());
                Vector3 velocity = delta - localSpaceVelocity;
                simulatedPoint.workingPosition = newPosition + velocity * airDrag + localSpaceVelocity * friction + acceleration;
                #endregion

                #region ConstrainAngle
                if (simulatedPoint.shouldConfineAngle) {
                    Vector3 parentParentPosition;
                    Vector3 poseParentParent;
                    if (parentSimulatedPoint.parentID == null) {
                        poseParentParent = parentSimulatedPoint.currentFixedAnimatedBonePosition + (parentSimulatedPoint.currentFixedAnimatedBonePosition - simulatedPoint.currentFixedAnimatedBonePosition);
                        parentParentPosition = poseParentParent;
                    } else {
                        var parentParentSimulatedPoint = simulatedPoints[parentSimulatedPoint.parentID.Value];
                        parentParentPosition = parentParentSimulatedPoint.workingPosition;
                        poseParentParent = parentParentSimulatedPoint.currentFixedAnimatedBonePosition;
                    }
                    Vector3 parentAimTargetPose = parentSimulatedPoint.currentFixedAnimatedBonePosition - poseParentParent;
                    Vector3 parentAim = parentSimulatedPoint.workingPosition - parentParentPosition;
                    Quaternion targetPoseToPose = Quaternion.FromToRotation(parentAimTargetPose, parentAim);
                    Vector3 currentPose = simulatedPoint.currentFixedAnimatedBonePosition - poseParentParent;
                    Vector3 constraintTarget = targetPoseToPose * currentPose;
                    float error = Vector3.Distance(simulatedPoint.workingPosition, parentParentPosition + constraintTarget);
                    error /= lengthToParent;
                    error = Mathf.Clamp01(error);
                    error = Mathf.Pow(error, data.elasticitySoften * 2f);
                    simulatedPoint.workingPosition = Vector3.LerpUnclamped(simulatedPoint.workingPosition, parentParentPosition + constraintTarget, squaredAngleElasticity * error);
                }
                #endregion

                #region Collisions
                if (colliders.Count != 0 && CachedSphereCollider.TryGet(out SphereCollider sphereCollider)) {
                    foreach (var collider in colliders) {
                        sphereCollider.radius = jiggleSettings.GetRadius(simulatedPoint.normalizedIndex);
                        if (sphereCollider.radius <= 0) {
                            continue;
                        }

                        if (Physics.ComputePenetration(sphereCollider, simulatedPoint.workingPosition, Quaternion.identity, collider, collider.transform.position, collider.transform.rotation, out Vector3 dir, out float dist)) {
                            simulatedPoint.workingPosition += dir * dist;
                        }
                    }
                } else {
                    Vector3 diff = simulatedPoint.workingPosition - parentSimulatedPoint.workingPosition;
                    Vector3 dir = diff.normalized;
                    simulatedPoint.workingPosition = Vector3.LerpUnclamped(simulatedPoint.workingPosition, parentSimulatedPoint.workingPosition + dir * lengthToParent, squaredLengthElasticity);
                }
                #endregion

                simulatedPoint.particleSignal.SetPosition(simulatedPoint.workingPosition, time);
                simulatedPoints[i] = simulatedPoint;
            }
        }

        /// <summary>
        /// Creates the virtual particle tree that is used to simulate the jiggles!
        /// </summary>
        public void Initialize() {
            var jiggleBoneList = new List<JiggleBone>();
            //simulatedPoints = new List<JiggleBone>();
            if (rootTransform == null) {
                return;
            }

            CreateSimulatedPoints(jiggleBoneList, ignoredTransforms, rootTransform, null, null);
            for (int i = 0; i < jiggleBoneList.Count; i++) {
                var simulatedPoint = jiggleBoneList[i];
                simulatedPoint.CalculateNormalizedIndex(jiggleBoneList);
                jiggleBoneList[i] = simulatedPoint;
            }
            simulatedPoints = jiggleBoneList.ToArray();
            boneCount = simulatedPoints.Length;
        }

        /// <summary>
        /// Calculates where the virtual particles would be at this exact moment in time (the latest simulation state is in the past and must be extrapolated).
        /// You normally won't call this.
        /// </summary>
        internal void DeriveFinalSolve(double timeAsDouble) {
            Vector3 offset = simulatedPoints[0].DeriveFinalSolvePosition(Vector3.zero, timeAsDouble) - simulatedPoints[0].transform.position;
            for (int i = 0; i < boneCount; i++) {
                var simulatedPoint = simulatedPoints[i];
                simulatedPoint.DeriveFinalSolvePosition(-offset, timeAsDouble);
                simulatedPoints[i] = simulatedPoint;
            }
        }

        /// <summary>
        /// Attempts to pose the rig to the virtual particles. The current pose MUST be the last valid pose (set by ApplyValidPoseThenSampleTargetPose usually)
        /// </summary>
        /// <param name="debugDraw">If we should draw the target pose (blue) compared to the virtual particle pose (red).</param>
        public void Pose(bool debugDraw, double timeAsDouble) {
            DeriveFinalSolve(timeAsDouble);
            var blend = jiggleSettings.GetData().blend;
            
            for (int i = 0; i < boneCount; i++) {
                var simulatedPoint = simulatedPoints[i];
                simulatedPoint.PoseBonePreCache(simulatedPoints, blend);
                simulatedPoints[i] = simulatedPoint;
            }
            
            for (int i = 0; i < boneCount; i++) {
                var simulatedPoint = simulatedPoints[i];
                simulatedPoint.PoseBone(simulatedPoints, blend);
                if (debugDraw) {
                    simulatedPoint.DebugDraw(simulatedPoints, Color.red, Color.blue, true);
                }
                simulatedPoints[i] = simulatedPoint;
            }
        }

        /// <summary>
        /// Saves the current state of the particles and animations in preparation for a teleport. Move the rig, then do FinishTeleport().
        /// </summary>
        public void PrepareTeleport() {
            for (int i = 0; i < boneCount; i++) {
                var simulatedPoint = simulatedPoints[i];
                simulatedPoint.PrepareTeleport(simulatedPoints);
                simulatedPoints[i] = simulatedPoint;
            }
        }

        /// <summary>
        /// Offsets the jiggle signals from the position set with PrepareTeleport to the current position. This prevents jiggles from freaking out from a large movement.
        /// </summary>
        public void FinishTeleport() {
            for (int i = 0; i < boneCount; i++) {
                var simulatedPoint = simulatedPoints[i];
                simulatedPoint.FinishTeleport(simulatedPoints);
                simulatedPoints[i] = simulatedPoint;
            }
        }

        public void OnDrawGizmos() {
            if (!initialized) {
                Initialize();
            }

            simulatedPoints[0].OnDrawGizmos(simulatedPoints, jiggleSettings, true);
            for (int i = 1; i < boneCount; i++) {
                simulatedPoints[i].OnDrawGizmos(simulatedPoints, jiggleSettings);
            }
        }

        protected virtual void CreateSimulatedPoints(List<JiggleBone> outputPoints, ICollection<Transform> ignoredTransforms, Transform currentTransform, JiggleBone? parentJiggleBone, int? parentJiggleBoneID) {
            JiggleBone newJiggleBone = new JiggleBone(outputPoints, currentTransform, parentJiggleBone, parentJiggleBoneID);
            outputPoints.Add(newJiggleBone);
            var currentID = outputPoints.Count - 1;
            if (parentJiggleBoneID != null) {
                var parent = outputPoints[parentJiggleBoneID.Value];
                parent.childID = outputPoints.Count-1;
                outputPoints[parentJiggleBoneID.Value] = parent;
            }
            // Create an extra purely virtual point if we have no children.
            if (currentTransform.childCount == 0) {
                if (newJiggleBone.parentID == null) {
                    if (newJiggleBone.transform.parent == null) {
                        throw new UnityException("Can't have a singular jiggle bone with no parents. That doesn't even make sense!");
                    } else {
                        outputPoints.Add(new JiggleBone(outputPoints, null, newJiggleBone, currentID));
                        var parent = outputPoints[currentID];
                        parent.childID = outputPoints.Count-1;
                        outputPoints[currentID] = parent;
                        return;
                    }
                }
                outputPoints.Add(new JiggleBone(outputPoints, null, newJiggleBone, currentID));
                {
                    var parent = outputPoints[currentID];
                    parent.childID = outputPoints.Count-1;
                    outputPoints[currentID] = parent;
                }
                return;
            }
            for (int i = 0; i < currentTransform.childCount; i++) {
                if (ignoredTransforms.Contains(currentTransform.GetChild(i))) {
                    continue;
                }
                CreateSimulatedPoints(outputPoints, ignoredTransforms, currentTransform.GetChild(i), newJiggleBone, currentID);
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

    public virtual void Advance(float deltaTime, Vector3 gravity, double timeAsDouble) {
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
            rig.ApplyValidPoseThenSampleTargetPose(timeAsDouble);
        }
        accumulation = Math.Min(accumulation+deltaTime, MAX_CATCHUP_TIME);
        var position = transform.position;
        while (accumulation > VERLET_TIME_STEP) {
            accumulation -= VERLET_TIME_STEP;
            double time = timeAsDouble - accumulation;
            foreach(JiggleRig rig in jiggleRigs) {
                rig.StepSimulation(position, levelOfDetail, wind, time, gravity);
            }
        }
        
        foreach (JiggleRig rig in jiggleRigs) {
            rig.Pose(debugDraw, timeAsDouble);
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
            if (isActiveAndEnabled) {
                switch (jiggleUpdateMode) {
                    case JiggleUpdateMode.LateUpdate: JiggleRigLateUpdateHandler.AddJiggleRigAdvancable(this); break;
                    case JiggleUpdateMode.FixedUpdate: JiggleRigFixedUpdateHandler.AddJiggleRigAdvancable(this); break;
                    default: throw new ArgumentOutOfRangeException();
                }
                SetJiggleRigLOD(levelOfDetail);
            }
        } else {
            if (jiggleRigs == null) return;
            foreach (JiggleRig rig in jiggleRigs) {
                rig.Initialize();
            }
        }
    }
}

}