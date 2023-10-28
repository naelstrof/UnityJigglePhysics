using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

[CreateAssetMenu(fileName = "JiggleSettingsBlend", menuName = "JigglePhysics/Blend Settings", order = 1)]
// This is used to blend other jiggle settings together.
public class JiggleSettingsBlend : JiggleSettingsBase {
    [Tooltip("The list of jiggle settings to blend between.")]
    public List<JiggleSettings> blendSettings;
    [Range(0f,1f)][Tooltip("A value from 0 to 1 that linearly blends between all of the blendSettings.")]
    public float normalizedBlend;

    public override JiggleSettingsData GetData() {
        int settingsCountSpace = blendSettings.Count - 1;
        float normalizedBlendClamp = Mathf.Clamp01(normalizedBlend);
        int targetA = Mathf.Clamp(Mathf.FloorToInt(normalizedBlendClamp*settingsCountSpace), 0,settingsCountSpace);
        int targetB = Mathf.Clamp(Mathf.FloorToInt(normalizedBlendClamp*settingsCountSpace)+1, 0,settingsCountSpace);
        return JiggleSettingsData.Lerp(
            blendSettings[targetA].GetData(),
            blendSettings[targetB].GetData(), 
            Mathf.Clamp01(normalizedBlendClamp*settingsCountSpace-targetA)
            );
    }
    public override float GetRadius(float normalizedIndex) {
        float normalizedBlendClamp = Mathf.Clamp01(normalizedBlend);
        int targetA = Mathf.FloorToInt(normalizedBlendClamp*blendSettings.Count);
        int targetB = Mathf.FloorToInt(normalizedBlendClamp*blendSettings.Count)+1;
        return Mathf.Lerp(blendSettings[Mathf.Clamp(targetA,0,blendSettings.Count-1)].GetRadius(normalizedIndex),
                          blendSettings[Mathf.Clamp(targetB,0,blendSettings.Count-1)].GetRadius(normalizedIndex), Mathf.Clamp01(normalizedBlendClamp*blendSettings.Count-targetA));
    }
}

}
