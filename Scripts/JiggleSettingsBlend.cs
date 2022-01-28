using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "JiggleSettingsBlend", menuName = "JiggleRig/Blend Settings", order = 1)]
public class JiggleSettingsBlend : JiggleSettingsBase {
    public List<JiggleSettings> blendSettings;
    [Range(0f,1f)]
    public float normalizedBlend;
    public override float GetParameter(JiggleSettingParameter parameter) {
        float normalizedBlendClamp = Mathf.Clamp01(normalizedBlend);
        int targetA = Mathf.FloorToInt(normalizedBlendClamp*blendSettings.Count);
        int targetB = Mathf.FloorToInt(normalizedBlendClamp*blendSettings.Count)+1;
        return Mathf.Lerp(blendSettings[Mathf.Clamp(targetA,0,blendSettings.Count-1)].GetParameter(parameter),
                          blendSettings[Mathf.Clamp(targetB,0,blendSettings.Count-1)].GetParameter(parameter), Mathf.Clamp01(normalizedBlendClamp*blendSettings.Count-targetA));
    }
}
