using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

public class JiggleSkin : MonoBehaviour {
    [System.Serializable]
    public class JiggleZone {
        [Tooltip("The transform from which the zone effects, this is used as the 'center'.")]
        public Transform target;
        [Tooltip("How large of a radius the zone should effect, in target-space meters. (Scaling the target will effect the radius.)")]
        public float radius;
        [Tooltip("The settings that the skin should update with, create them using the Create->JigglePhysics->Settings menu option.")]
        public JiggleSettingsBase jiggleSettings;
        [HideInInspector]
        public JigglePoint simulatedPoint;
    }
    [SerializeField] [Tooltip("Enables interpolation for the simulation, this should be enabled unless you *really* need the simulation to only update on FixedUpdate.")]
    private bool interpolate = true;
    public List<JiggleZone> jiggleZones;
    [SerializeField] [Tooltip("The list of skins to send the deformation data too, they should have JiggleSkin-compatible materials!")]
    public List<SkinnedMeshRenderer> targetSkins;
    [SerializeField] [Tooltip("Draws some simple lines to show what the simulation is doing. Generally this should be disabled.")]
    private bool debugDraw = false;

    private List<Material> targetMaterials;
    private List<Vector4> packedVectors;
    private int jiggleInfoNameID;
    void Start() {
        foreach( JiggleZone zone in jiggleZones) {
            zone.simulatedPoint = new JigglePoint(zone.target);
        }
        targetMaterials = new List<Material>();
        jiggleInfoNameID = Shader.PropertyToID("_JiggleInfos");
        packedVectors = new List<Vector4>();
    }
    private void LateUpdate() {
        // Pack the data
        packedVectors.Clear();
        foreach( JiggleZone zone in jiggleZones) {
            Vector3 targetPointSkinSpace = targetSkins[0].rootBone.InverseTransformPoint(zone.target.position);
            Vector3 verletPointSkinSpace = targetSkins[0].rootBone.InverseTransformPoint(interpolate ? zone.simulatedPoint.interpolatedPosition : zone.simulatedPoint.position);
            packedVectors.Add(new Vector4(targetPointSkinSpace.x, targetPointSkinSpace.y, targetPointSkinSpace.z, zone.radius*zone.target.lossyScale.x));
            packedVectors.Add(new Vector4(verletPointSkinSpace.x, verletPointSkinSpace.y, verletPointSkinSpace.z, zone.jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Blend)));
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

        // Debug draw stuff
        if (debugDraw) {
            foreach( JiggleZone zone in jiggleZones) {
                zone.simulatedPoint.DebugDraw(Color.red, true);
            }
        }
    }
    private void FixedUpdate() {
        foreach( JiggleZone zone in jiggleZones) {
            zone.simulatedPoint.PrepareSimulate();
            zone.simulatedPoint.Simulate(zone.jiggleSettings);
        }
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
            if (zone.target == null) {
                continue;
            }
            Gizmos.DrawWireSphere(zone.target.position, zone.radius*zone.target.lossyScale.x);
        }
    }
    // CPU version of the skin transformation, untested, can be useful in reconstructing the deformation on the cpu.
    public Vector3 ApplyJiggle(Vector3 toPoint, float blend) {
        Vector3 result = toPoint;
        foreach( JiggleZone zone in jiggleZones) {
            Vector3 targetPointSkinSpace = targetSkins[0].rootBone.InverseTransformPoint(zone.target.position);
            Vector3 verletPointSkinSpace = targetSkins[0].rootBone.InverseTransformPoint(zone.simulatedPoint.interpolatedPosition);
            Vector3 diff = verletPointSkinSpace - targetPointSkinSpace;
            float dist = Vector3.Distance(targetPointSkinSpace, targetSkins[0].rootBone.InverseTransformPoint(toPoint));
            float multi = 1f-Mathf.SmoothStep(0,zone.radius*zone.target.lossyScale.x,dist);
            result += targetSkins[0].rootBone.TransformVector(diff) * zone.jiggleSettings.GetParameter(JiggleSettingsBase.JiggleSettingParameter.Blend) * blend;
        }
        return result;
    }
}

}