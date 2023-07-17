using System.Collections;
using System.Collections.Generic;
using JigglePhysics;
using UnityEngine;

public class BreastSlider : MonoBehaviour {
    [Header("This is a demo on how you can use Blends to configure breast physics per character")]
    [Range(0f,1f)] [SerializeField]
    private float boobSize;
    
    [Header("Breast configuration")]
    [SerializeField]
    private AnimationCurve bigCurve;
    [SerializeField]
    private string blendshapeBig;
    [SerializeField]
    private List<SkinnedMeshRenderer> targetRenderers;
    [SerializeField]
    private JigglePhysics.JiggleRigBuilder rigTarget;
    [SerializeField]
    private JigglePhysics.JiggleSkin skinTarget;
    [SerializeField]
    private List<Transform> jiggleSkinTargets;
    [SerializeField]
    private List<Transform> jiggleRigTargets;
    void Update() {
        foreach(SkinnedMeshRenderer renderer in targetRenderers) {
            renderer.SetBlendShapeWeight(renderer.sharedMesh.GetBlendShapeIndex(blendshapeBig), bigCurve.Evaluate(boobSize)*100f);
        }
        foreach (var breast in jiggleSkinTargets) {
            ((JiggleSettingsBlend)skinTarget.GetJiggleZone(breast).jiggleSettings).normalizedBlend = boobSize;
        }
        foreach (var breast in jiggleRigTargets) {
            ((JiggleSettingsBlend)rigTarget.GetJiggleRig(breast).jiggleSettings).normalizedBlend = boobSize;
        }
    }
}
