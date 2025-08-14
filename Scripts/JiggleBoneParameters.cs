using System;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

[Serializable]
public struct JiggleBoneParameters {
    public float rootElasticity;
    public float angleElasticity;
    public bool angleLimited;
    public float angleLimit;
    public float angleLimitSoften;
    public float lengthElasticity;
    public float elasticitySoften;
    public float gravityMultiplier;
    public float blend;
    public float airDrag;
    public float drag;
    public float collisionRadius;
}

[Serializable]
public struct JiggleBoneInputParameters {
    public bool advancedToggle;
    public bool collisionToggle;
    public bool angleLimitToggle;
    public float stiffness;
    public AnimationCurve stiffnessCurve;
    public float soften;
    public float angleLimit;
    public AnimationCurve angleLimitCurve;
    public float angleLimitSoften;
    public float rootStretch;
    public float stretch;
    public AnimationCurve stretchCurve;
    public float drag;
    public AnimationCurve dragCurve;
    public float airDrag;
    public AnimationCurve airDragCurve;
    public float gravity;
    public AnimationCurve gravityCurve;
    public float collisionRadius;
    public AnimationCurve collisionRadiusCurve;
    public float blend;

    public JiggleBoneParameters ToJiggleBoneParameters(float normalizedDistanceFromRoot) {
        return new JiggleBoneParameters {
            rootElasticity = advancedToggle ? 1f - rootStretch : 0f,
            angleElasticity = Mathf.Pow(stiffness * stiffnessCurve.Evaluate(normalizedDistanceFromRoot), 2f),
            lengthElasticity = advancedToggle
                ? Mathf.Pow(1f - stretch * stretchCurve.Evaluate(normalizedDistanceFromRoot), 2f)
                : 0f,
            elasticitySoften = advancedToggle ? Mathf.Pow(soften, 2f) : 0f,
            gravityMultiplier = gravity * gravityCurve.Evaluate(normalizedDistanceFromRoot),
            angleLimited = angleLimitToggle,
            angleLimit = angleLimit * angleLimitCurve.Evaluate(normalizedDistanceFromRoot),
            angleLimitSoften = angleLimitSoften,
            blend = 1f,
            drag = drag * dragCurve.Evaluate(normalizedDistanceFromRoot),
            airDrag = airDrag * airDragCurve.Evaluate(normalizedDistanceFromRoot),
            collisionRadius = collisionRadius * collisionRadiusCurve.Evaluate(normalizedDistanceFromRoot)
        };
    }
}

}
