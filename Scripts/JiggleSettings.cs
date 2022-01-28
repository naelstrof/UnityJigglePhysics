using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "JiggleSettings", menuName = "JiggleRig/Settings", order = 1)]
public class JiggleSettings : JiggleSettingsBase {
    [Range(0f,1f)] [SerializeField] private float gravityMultiplier = 1f;
    [Range(0f,1f)] [SerializeField] private float friction = 0.5f;
    [Range(0f,1f)] [SerializeField] private float airFriction = 0.5f;
    [Range(0f,1f)] [SerializeField] private float blend = 1f;
    [Range(0f,1f)] [SerializeField] private float angleElasticity = 0.5f;
    [Range(0f,1f)] [SerializeField] private float lengthElasticity = 0.5f;
    public override float GetParameter(JiggleSettingParameter parameter) {
        switch(parameter) {
            case JiggleSettingParameter.Gravity: return gravityMultiplier;
            case JiggleSettingParameter.Friction: return friction;
            case JiggleSettingParameter.AirFriction: return airFriction;
            case JiggleSettingParameter.Blend: return blend;
            case JiggleSettingParameter.AngleElasticity: return angleElasticity;
            case JiggleSettingParameter.LengthElasticity: return lengthElasticity;
            default: throw new UnityException("Invalid Jiggle Setting Parameter:"+parameter);
        }
    }
}