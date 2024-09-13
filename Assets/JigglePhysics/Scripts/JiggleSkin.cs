using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace JigglePhysics {

public class JiggleSkin : MonoBehaviour {
    [Serializable]
    public class JiggleZone : JiggleRigBuilder.JiggleRig {
        [Tooltip("How large of a radius the zone should effect, in target-space meters. (Scaling the target will effect the radius.)")]
        public float radius;
        public JiggleZone(Transform rootTransform, JiggleSettingsBase jiggleSettings, ICollection<Transform> ignoredTransforms, ICollection<Collider> colliders) : base(rootTransform, jiggleSettings, ignoredTransforms, colliders) { }
        protected override void CreateSimulatedPoints(ICollection<JiggleBone> outputPoints, ICollection<Transform> ignoredTransforms, Transform currentTransform, JiggleBone parentJiggleBone) {
            //base.CreateSimulatedPoints(outputPoints, ignoredTransforms, currentTransform, parentJiggleBone);
            var parent = new JiggleBone(currentTransform, null);
            outputPoints.Add(parent);
            outputPoints.Add(new JiggleBone(null, parent,0f));
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
    [Tooltip("Enables interpolation for the simulation, this should be enabled unless you *really* need the simulation to only update on FixedUpdate.")]
    public bool interpolate = true;
    public List<JiggleZone> jiggleZones;
    [SerializeField] [Tooltip("The list of skins to send the deformation data too, they should have JiggleSkin-compatible materials!")]
    public List<SkinnedMeshRenderer> targetSkins;
    [Tooltip("An air force that is applied to the entire rig, this is useful to plug in some wind volumes from external sources.")]
    public Vector3 wind;
    [Tooltip("Level of detail manager. This system will control how the jiggle skin saves performance cost.")]
    public JiggleRigLOD levelOfDetail;
    [SerializeField] [Tooltip("Draws some simple lines to show what the simulation is doing. Generally this should be disabled.")]
    private bool debugDraw = false;

    private bool wasLODActive = true;
    private double accumulation;

    [Tooltip("An event that occurs after the Jiggle Skin is done moving the skin for the frame.")]
    public UnityAction FinishedPass;

    private List<Material> targetMaterials;
    private List<Vector4> packedVectors;
    private int jiggleInfoNameID;
    private bool dirtyFromEnable;

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

    private void OnEnable() {
        Initialize();
        dirtyFromEnable = true;
        CachedSphereCollider.AddSkin(this);
    }
    
    private void OnDisable() {
        PrepareTeleport();
        CachedSphereCollider.RemoveSkin(this);
    }

    public void Initialize() {
        accumulation = 0f;
        jiggleZones ??= new List<JiggleZone>();
        foreach( JiggleZone zone in jiggleZones) {
            zone.Initialize();
        }
        targetMaterials = new List<Material>();
        jiggleInfoNameID = Shader.PropertyToID("_JiggleInfos");
        packedVectors = new List<Vector4>();
    }

    public JiggleZone GetJiggleZone(Transform target) {
        foreach (var jiggleZone in jiggleZones) {
            if (jiggleZone.GetRootTransform() == target) {
                return jiggleZone;
            }
        }

        return null;
    }

    public void Advance(float deltaTime) {
        if (levelOfDetail!=null && !levelOfDetail.CheckActive(transform.position)) {
            if (wasLODActive) PrepareTeleport();
            CachedSphereCollider.StartPass();
            CachedSphereCollider.FinishedPass();
            wasLODActive = false;
            return;
        }
        if (!wasLODActive) FinishTeleport();
        CachedSphereCollider.StartPass();
        foreach (JiggleZone zone in jiggleZones) {
            zone.PrepareBone(transform.position, levelOfDetail);
        }
        
        if (dirtyFromEnable) {
            foreach (var rig in jiggleZones) {
                rig.FinishTeleport();
            }
            dirtyFromEnable = false;
        }
        
        accumulation = Math.Min(accumulation+deltaTime, JiggleRigBuilder.MAX_CATCHUP_TIME);
        while (accumulation > JiggleRigBuilder.VERLET_TIME_STEP) {
            accumulation -= JiggleRigBuilder.VERLET_TIME_STEP;
            double time = Time.timeAsDouble - accumulation;
            foreach( JiggleZone zone in jiggleZones) {
                zone.Update(wind, time);
            }
        }
        foreach( JiggleZone zone in jiggleZones) {
            zone.DeriveFinalSolve();
        }
        UpdateMesh();
        CachedSphereCollider.FinishedPass();
        if (!debugDraw) return;
        foreach( JiggleZone zone in jiggleZones) {
            zone.DebugDraw();
        }

        FinishedPass?.Invoke();
    }

    private void LateUpdate() {
        if (!interpolate) {
            return;
        }
        Advance(Time.deltaTime);
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
            targetSkin.GetMaterials(targetMaterials);
            foreach(Material m in targetMaterials) {
                m.SetVectorArray(jiggleInfoNameID, packedVectors);
            }
        }
    }

    private void FixedUpdate() {
        if (interpolate) {
            return;
        }
        Advance(Time.deltaTime);
    }
    
    void OnValidate() {
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
            result += targetSkins[0].rootBone.TransformVector(diff) * zone.jiggleSettings.GetData().blend * blend;
        }
        return result;
    }
}

}