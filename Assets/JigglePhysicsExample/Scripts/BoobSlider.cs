using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoobSlider : MonoBehaviour {
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
    private JiggleRigBuilder rigTarget;
    [SerializeField]
    private JiggleSettingsBlend boobBlend;
    void Start() {
        boobBlend = JiggleSettingsBlend.Instantiate(boobBlend);
        foreach(JiggleRigBuilder.JiggleRig rig in rigTarget.jiggleRigs) {
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
