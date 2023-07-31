using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {
public class JiggleSettingsBase : ScriptableObject {
    public enum JiggleSettingParameter {
        Gravity = 0,
        Friction,
        AirFriction,
        Blend,
        AngleElasticity,
        ElasticitySoften,
        LengthElasticity,
        RadiusMultiplier,
    }
    public virtual float GetParameter(JiggleSettingParameter parameter) {
        return 0f;
    }
    public virtual float GetRadius(float normalizedIndex) {
        return 0f;
    }
}

}