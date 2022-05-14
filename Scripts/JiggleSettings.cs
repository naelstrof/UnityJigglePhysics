using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace JigglePhysics {

[CreateAssetMenu(fileName = "JiggleSettings", menuName = "JigglePhysics/Settings", order = 1)]
public class JiggleSettings : JiggleSettingsBase {
    [SerializeField] [Range(0f,2f)] [Tooltip("How much gravity to apply to the simulation, it is a multiplier of the Physics.gravity setting.")]
    private float gravityMultiplier = 1f;
    [SerializeField] [Range(0f,1f)] [Tooltip("How much mechanical friction to apply, this is specifically how quickly oscillations come to rest.")]
    private float friction = 0.4f;
    [SerializeField] [Range(0f,1f)] [Tooltip("How much air friction to apply, this is how much jiggled objects should get dragged behind during movement. Or how *thick* the air is.")]
    private float airFriction = 0.1f;
    [SerializeField] [Range(0f,1f)] [Tooltip("How much of the simulation should be expressed. A value of 0 would make the jiggle have zero effect. A value of 1 gives the full movement as intended.")]
    private float blend = 1f;
    [SerializeField] [Range(0f,1f)] [Tooltip("How much angular force to apply in order to push the jiggled object back to rest.")]
    private float angleElasticity = 0.4f;
    [SerializeField] [Range(0f,1f)] [Tooltip("How much to allow free bone motion before engaging elasticity.")]
    private float elasticitySoften = 0f;
    [SerializeField] [Range(0f,1f)] [Tooltip("How much linear force to apply in order to keep the jiggled object at the correct length. Squash and stretch!")]
    private float lengthElasticity = 0.4f;
    public override float GetParameter(JiggleSettingParameter parameter) {
        switch(parameter) {
            case JiggleSettingParameter.Gravity: return gravityMultiplier;
            case JiggleSettingParameter.Friction: return friction;
            case JiggleSettingParameter.AirFriction: return airFriction;
            case JiggleSettingParameter.Blend: return blend;
            case JiggleSettingParameter.AngleElasticity: return angleElasticity;
            case JiggleSettingParameter.ElasticitySoften: return elasticitySoften;
            case JiggleSettingParameter.LengthElasticity: return lengthElasticity;
            default: throw new UnityException("Invalid Jiggle Setting Parameter:"+parameter);
        }
    }
    public void SetParameter(JiggleSettingParameter parameter, float value) {
        switch(parameter) {
            case JiggleSettingParameter.Gravity: gravityMultiplier = value; break;
            case JiggleSettingParameter.Friction: friction = value; break;
            case JiggleSettingParameter.AirFriction: airFriction = value; break;
            case JiggleSettingParameter.Blend: blend = value; break;
            case JiggleSettingParameter.AngleElasticity: angleElasticity = value; break;
            case JiggleSettingParameter.ElasticitySoften: elasticitySoften = value; break;
            case JiggleSettingParameter.LengthElasticity: lengthElasticity = value; break;
            default: throw new UnityException("Invalid Jiggle Setting Parameter:"+parameter);
        }
    }
}

}