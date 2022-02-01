using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreastSlider : MonoBehaviour {
    [SerializeField]
    private AnimationCurve flatCurve;
    [SerializeField]
    private AnimationCurve bigCurve;
    [Range(0f,1f)] [SerializeField]
    private float boobSize;
    [SerializeField]
    private string blendshapeFlat;
    [SerializeField]
    private string blendshapeBig;
    [SerializeField]
    private List<SkinnedMeshRenderer> targetRenderers;
    [SerializeField]
    private JigglePhysics.JiggleRigBuilder rigTarget;
    [SerializeField]
    private JigglePhysics.JiggleSettingsBlend boobBlend;
    void Start() {
        // Instantiate our blend, so that we can individually adjust blend values.
        boobBlend = JigglePhysics.JiggleSettingsBlend.Instantiate(boobBlend);
        foreach(JigglePhysics.JiggleRigBuilder.JiggleRig rig in rigTarget.jiggleRigs) {
            // Lazily detect which rig is a breast jiggle rig.
            if (rig.rootTransform.name.Contains("Breast")) {
                rig.jiggleSettings = boobBlend;
            }
        }
    }
    void Update() {
        boobBlend.normalizedBlend = boobSize;
        foreach(SkinnedMeshRenderer renderer in targetRenderers) {
            renderer.SetBlendShapeWeight(renderer.sharedMesh.GetBlendShapeIndex(blendshapeFlat), flatCurve.Evaluate(boobSize)*100f);
            renderer.SetBlendShapeWeight(renderer.sharedMesh.GetBlendShapeIndex(blendshapeBig), bigCurve.Evaluate(boobSize)*100f);
        }
    }
}
