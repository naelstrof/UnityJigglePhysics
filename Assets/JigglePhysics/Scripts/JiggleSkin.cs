using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

public class JiggleSkin : MonoBehaviour {
    [Serializable]
    public class JiggleZone {
        [SerializeField][Tooltip("The transform from which the zone effects, this is used as the 'center'.")]
        private Transform target;
        [Tooltip("How large of a radius the zone should effect, in target-space meters. (Scaling the target will effect the radius.)")]
        public float radius;
        [Tooltip("The settings that the skin should update with, create them using the Create->JigglePhysics->Settings menu option.")]
        public JiggleSettingsBase jiggleSettings;
        
        [HideInInspector]
        private JigglePoint simulatedPoint;

        private bool initialized;

        public void PrepareSimulate() {
            if (!initialized) {
                throw new UnityException( "JiggleZone wasn't initialized, please call JiggleSkin.Initialize() or JiggleZone.Awake() before manually timestepping.");
            }

            simulatedPoint.PrepareSimulate();
        }

        public void Initialize() {
            simulatedPoint = new JigglePoint(target);
            initialized = true;
        }

        public Transform GetTargetBone() => target;

        public void Simulate(Vector3 wind, double time) {
            simulatedPoint.Simulate(jiggleSettings.GetData(), wind, time);
        }
        public void DeriveFinalSolve(float smoothing) {
            simulatedPoint.DeriveFinalSolvePosition(smoothing);
        }

        public void DebugDraw() {
            simulatedPoint.DebugDraw(Color.red);
        }

        public float GetLossyScale() => target.lossyScale.x;

        public Vector3 GetPosition() => target.position;
        public Vector3 GetSolve() => simulatedPoint.extrapolatedPosition;

        public void OnDrawGizmosSelected() {
            if (target == null) {
                return;
            }
            Gizmos.color = new Color(0.1f,0.1f,0.8f,0.5f);
            Gizmos.DrawWireSphere(target.position, radius*target.lossyScale.x);
        }
    }
    [Tooltip("Enables interpolation for the simulation, this should be enabled unless you *really* need the simulation to only update on FixedUpdate.")]
    public bool interpolate = true;
    public List<JiggleZone> jiggleZones;
    [SerializeField] [Tooltip("The list of skins to send the deformation data too, they should have JiggleSkin-compatible materials!")]
    public List<SkinnedMeshRenderer> targetSkins;
    [Tooltip("An air force that is applied to the entire rig, this is useful to plug in some wind volumes from external sources.")]
    public Vector3 wind;
    [SerializeField] [Tooltip("Draws some simple lines to show what the simulation is doing. Generally this should be disabled.")]
    private bool debugDraw = false;
    private double accumulation;

    private List<Material> targetMaterials;
    private List<Vector4> packedVectors;
    private int jiggleInfoNameID;
    private const float smoothing = 1f;
    void Awake() {
        Initialize();
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
            if (jiggleZone.GetTargetBone() == target) {
                return jiggleZone;
            }
        }

        return null;
    }

    public void Advance(float deltaTime) {
        foreach (JiggleZone zone in jiggleZones) {
            zone.PrepareSimulate();
        }
        accumulation = Math.Min(accumulation+deltaTime, Time.fixedDeltaTime*4f);
        while (accumulation > Time.fixedDeltaTime) {
            accumulation -= Time.fixedDeltaTime;
            double time = Time.timeAsDouble - accumulation;
            foreach( JiggleZone zone in jiggleZones) {
                zone.Simulate(wind, time);
            }
        }
        
        foreach( JiggleZone zone in jiggleZones) {
            zone.DeriveFinalSolve(smoothing);
        }
        UpdateMesh();
        if (!debugDraw) return;
        foreach( JiggleZone zone in jiggleZones) {
            zone.DebugDraw();
        }
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
                Vector3 targetPointSkinSpace = targetSkin.rootBone.InverseTransformPoint(zone.GetPosition());
                Vector3 verletPointSkinSpace = targetSkin.rootBone.InverseTransformPoint(zone.GetSolve());
                packedVectors.Add(new Vector4(targetPointSkinSpace.x, targetPointSkinSpace.y, targetPointSkinSpace.z,
                    zone.radius * zone.GetLossyScale()));
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
            zone.DeriveFinalSolve(smoothing);
            Vector3 targetPointSkinSpace = targetSkins[0].rootBone.InverseTransformPoint(zone.GetPosition());
            Vector3 verletPointSkinSpace = targetSkins[0].rootBone.InverseTransformPoint(zone.GetSolve());
            Vector3 diff = verletPointSkinSpace - targetPointSkinSpace;
            float dist = Vector3.Distance(targetPointSkinSpace, targetSkins[0].rootBone.InverseTransformPoint(toPoint));
            float multi = 1f-Mathf.SmoothStep(0,zone.radius*zone.GetLossyScale(),dist);
            result += targetSkins[0].rootBone.TransformVector(diff) * zone.jiggleSettings.GetData().blend * blend;
        }
        return result;
    }
}

}