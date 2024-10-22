using System;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

public class JiggleSkin : MonoBehaviour, IJiggleAdvancable {
    [Serializable]
    public class JiggleZone : JiggleRigBuilder.JiggleRig {
        [Tooltip("How large of a radius the zone should effect, in target-space meters. (Scaling the target will effect the radius.)")]
        public float radius;
        public JiggleZone(Transform rootTransform, JiggleSettingsBase jiggleSettings, ICollection<Transform> ignoredTransforms, ICollection<Collider> colliders) : base(rootTransform, jiggleSettings, ignoredTransforms, colliders) { }
        protected override void CreateSimulatedPoints(List<JiggleBone> outputPoints, ICollection<Transform> ignoredTransforms, Transform currentTransform, JiggleBone? parentJiggleBone, int? parentID) {
            //base.CreateSimulatedPoints(outputPoints, ignoredTransforms, currentTransform, parentJiggleBone);
            var parent = new JiggleBone(outputPoints, currentTransform, null, null) {
                childID = 1,
            };
            outputPoints.Add(parent);
            outputPoints.Add(new JiggleBone(outputPoints, null, parent,0, 0f));
        }
        public void DebugDraw() {
            Debug.DrawLine(GetPointSolve(), GetRootTransform().position, Color.cyan, 0, false);
        }
        public Vector3 GetPointSolve() => simulatedPoints[1].GetCachedSolvePosition();
        public void OnDrawGizmosSelected() {
            if (GetRootTransform() == null) {
                return;
            }
            Gizmos.color = new Color(0.1f,0.1f,0.8f,0.5f);
            Gizmos.DrawWireSphere(GetRootTransform().position, radius*GetRootTransform().lossyScale.x);
        }
    }
    [SerializeField] [Tooltip("Enables interpolation for the simulation, this should be LateUpdate unless you *really* need the simulation to only update on FixedUpdate.")]
    private JiggleUpdateMode jiggleUpdateMode = JiggleUpdateMode.LateUpdate;
    public List<JiggleZone> jiggleZones;
    [SerializeField] [Tooltip("The list of skins to send the deformation data too, they should have JiggleSkin-compatible materials!")]
    public List<SkinnedMeshRenderer> targetSkins;
    [Tooltip("An air force that is applied to the entire rig, this is useful to plug in some wind volumes from external sources.")]
    public Vector3 wind;

    private bool hasLevelOfDetail;
    [SerializeField] [Tooltip("Level of detail manager. This system will control how the jiggle skin saves performance cost.")]
    private JiggleRigLOD levelOfDetail;
    
    [SerializeField] [Tooltip("Draws some simple lines to show what the simulation is doing. Generally this should be disabled.")]
    private bool debugDraw = false;

    private float settleTimer;

    private bool wasLODActive = true;
    private double accumulation;
    private MaterialPropertyBlock materialPropertyBlock;

    private List<Vector4> packedVectors;
    private int jiggleInfoNameID;

    public void PrepareTeleport() {
        foreach (var zone in jiggleZones) {
            zone.PrepareTeleport();
        }
    }
    
    public void FinishTeleport() {
        foreach (var zone in jiggleZones) {
            zone.FinishTeleport();
        }
    }
    
    public void SetJiggleRigLOD(JiggleRigLOD lod) {
        levelOfDetail = lod;
        hasLevelOfDetail = levelOfDetail;
    }

    private void Awake() {
        Initialize();
    }

    private void OnEnable() {
        switch (jiggleUpdateMode) {
            case JiggleUpdateMode.LateUpdate: JiggleRigLateUpdateHandler.AddJiggleRigAdvancable(this); break;
            case JiggleUpdateMode.FixedUpdate: JiggleRigFixedUpdateHandler.AddJiggleRigAdvancable(this); break;
            default: throw new ArgumentOutOfRangeException();
        }

        if (settleTimer >= JiggleRigBuilder.SETTLE_TIME) {
            FinishTeleport();
        }
    }
    
    private void OnDisable() {
        switch (jiggleUpdateMode) {
            case JiggleUpdateMode.LateUpdate: JiggleRigLateUpdateHandler.RemoveJiggleRigAdvancable(this); break;
            case JiggleUpdateMode.FixedUpdate: JiggleRigFixedUpdateHandler.RemoveJiggleRigAdvancable(this); break;
            default: throw new ArgumentOutOfRangeException();
        }
        PrepareTeleport();
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

    public void Initialize() {
        accumulation = 0f;
        jiggleZones ??= new List<JiggleZone>();
        foreach( JiggleZone zone in jiggleZones) {
            zone.Initialize();
        }
        jiggleInfoNameID = Shader.PropertyToID("_JiggleInfos");
        packedVectors = new List<Vector4>();
        settleTimer = 0f;
        hasLevelOfDetail = levelOfDetail;
        materialPropertyBlock = new MaterialPropertyBlock();
    }

    public JiggleZone GetJiggleZone(Transform target) {
        foreach (var jiggleZone in jiggleZones) {
            if (jiggleZone.GetRootTransform() == target) {
                return jiggleZone;
            }
        }

        return null;
    }

    public JiggleUpdateMode GetJiggleUpdateMode() {
        return jiggleUpdateMode;
    }

    public void Advance(float deltaTime, Vector3 gravity) {
        if (settleTimer < JiggleRigBuilder.SETTLE_TIME) {
            settleTimer += deltaTime;
            if (settleTimer >= JiggleRigBuilder.SETTLE_TIME) {
                FinishTeleport();
            }
            return;
        }
        
        if (hasLevelOfDetail && !levelOfDetail.CheckActive(transform.position)) {
            if (wasLODActive) PrepareTeleport();
            wasLODActive = false;
            return;
        }
        if (!wasLODActive) FinishTeleport();
        
        
        foreach (JiggleZone zone in jiggleZones) {
            zone.ApplyValidPoseThenSampleTargetPose();
        }
        accumulation = Math.Min(accumulation+deltaTime, JiggleRigBuilder.MAX_CATCHUP_TIME);
        var position = transform.position;
        while (accumulation > JiggleRigBuilder.VERLET_TIME_STEP) {
            accumulation -= JiggleRigBuilder.VERLET_TIME_STEP;
            double time = Time.timeAsDouble - accumulation;
            foreach( JiggleZone zone in jiggleZones) {
                zone.StepSimulation(position, levelOfDetail, wind, time, gravity);
            }
        }
        foreach( JiggleZone zone in jiggleZones) {
            zone.DeriveFinalSolve();
        }
        UpdateMesh();
        if (!debugDraw) return;
        foreach( JiggleZone zone in jiggleZones) {
            zone.DebugDraw();
        }
    }

    private void UpdateMesh() {
        // Pack the data
        packedVectors.Clear();
        foreach( var targetSkin in targetSkins) {
            foreach (var zone in jiggleZones) {
                Vector3 targetPointSkinSpace = targetSkin.rootBone.InverseTransformPoint(zone.GetRootTransform().position);
                Vector3 verletPointSkinSpace = targetSkin.rootBone.InverseTransformPoint(zone.GetPointSolve());
                packedVectors.Add(new Vector4(targetPointSkinSpace.x, targetPointSkinSpace.y, targetPointSkinSpace.z,
                    zone.radius * zone.GetRootTransform().lossyScale.x));
                packedVectors.Add(new Vector4(verletPointSkinSpace.x, verletPointSkinSpace.y, verletPointSkinSpace.z,
                    zone.jiggleSettings.GetData().blend));
            }
        }
        for(int i=packedVectors.Count;i<16;i++) {
            packedVectors.Add(Vector4.zero);
        }

        // Send the data
        foreach(SkinnedMeshRenderer targetSkin in targetSkins) {
            targetSkin.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetVectorArray(jiggleInfoNameID, packedVectors);
            targetSkin.SetPropertyBlock(materialPropertyBlock);
        }
    }
    
    void OnValidate() {
        if (Application.isPlaying) {
            JiggleRigLateUpdateHandler.RemoveJiggleRigAdvancable(this);
            JiggleRigFixedUpdateHandler.RemoveJiggleRigAdvancable(this);
            if (isActiveAndEnabled) {
                switch (jiggleUpdateMode) {
                    case JiggleUpdateMode.LateUpdate: JiggleRigLateUpdateHandler.AddJiggleRigAdvancable(this); break;
                    case JiggleUpdateMode.FixedUpdate: JiggleRigFixedUpdateHandler.AddJiggleRigAdvancable(this); break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
            SetJiggleRigLOD(levelOfDetail);
        }
        
        if (jiggleZones == null) {
            return;
        }
        for(int i=jiggleZones.Count-1;i>8;i--) {
            jiggleZones.RemoveAt(i);
        }
    }
    void OnDrawGizmosSelected() {
        if (jiggleZones == null) {
            return;
        }
        Gizmos.color = new Color(0.1f,0.1f,0.8f,0.5f);
        foreach(JiggleZone zone in jiggleZones) {
            zone.OnDrawGizmosSelected();
        }
    }
    // CPU version of the skin transformation, untested, can be useful in reconstructing the deformation on the cpu.
    public Vector3 ApplyJiggle(Vector3 toPoint, float blend) {
        Vector3 result = toPoint;
        foreach( JiggleZone zone in jiggleZones) {
            zone.DeriveFinalSolve();
            Vector3 targetPointSkinSpace = targetSkins[0].rootBone.InverseTransformPoint(zone.GetRootTransform().position);
            Vector3 verletPointSkinSpace = targetSkins[0].rootBone.InverseTransformPoint(zone.GetPointSolve());
            Vector3 diff = verletPointSkinSpace - targetPointSkinSpace;
            float dist = Vector3.Distance(targetPointSkinSpace, targetSkins[0].rootBone.InverseTransformPoint(toPoint));
            float multi = 1f-Mathf.SmoothStep(0,zone.radius*zone.GetRootTransform().lossyScale.x,dist);
            result += targetSkins[0].rootBone.TransformVector(diff) * zone.jiggleSettings.GetData().blend * blend * multi;
        }
        return result;
    }
}

}