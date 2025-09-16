using System;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

[Serializable]
public struct JigglePointParameters {
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
    public float ignoreRootMotion;
    public float collisionRadius;
}

[Serializable]
public struct JiggleTreeCurvedFloat {
    public float value;
    public bool curveEnabled;
    public AnimationCurve curve;
    public float Evaluate(float normalizedDistanceFromRoot) {
        return curveEnabled ? value * curve.Evaluate(normalizedDistanceFromRoot) : value;
    }

    public JiggleTreeCurvedFloat(float value) {
        this.value = value;
        curve = AnimationCurve.Constant(0f,1f,1f);
        curveEnabled = false;
    }
}

[Serializable]
public struct JiggleTreeInputParameters {
    public bool advancedToggle;
    public bool collisionToggle;
    public bool angleLimitToggle;
    public JiggleTreeCurvedFloat stiffness;
    public float soften;
    public JiggleTreeCurvedFloat angleLimit;
    public float angleLimitSoften;
    public float rootStretch;
    public float ignoreRootMotion;
    public JiggleTreeCurvedFloat stretch;
    public JiggleTreeCurvedFloat drag;
    public JiggleTreeCurvedFloat airDrag;
    public JiggleTreeCurvedFloat gravity;
    public JiggleTreeCurvedFloat collisionRadius;
    public float blend;

    private float ValidateFloat(float value, float defaultValue) {
        return float.IsNaN(value) ? defaultValue : Mathf.Clamp01(value);
    }

    public JigglePointParameters ToJigglePointParameters(float normalizedDistanceFromRoot) {
        var collisionRadiusValue = (collisionToggle && advancedToggle) ? collisionRadius.Evaluate(normalizedDistanceFromRoot) : 0f;
        var gravityValue = gravity.Evaluate(normalizedDistanceFromRoot);
        return new JigglePointParameters {
            rootElasticity = Mathf.Clamp(ValidateFloat(advancedToggle ? 1f - rootStretch : 1f, 0f), 0f, 0.9f),
            angleElasticity = ValidateFloat(Mathf.Pow(stiffness.Evaluate(normalizedDistanceFromRoot), 2f), 0.8f),
            lengthElasticity = ValidateFloat(advancedToggle ? Mathf.Pow(1f - stretch.Evaluate(normalizedDistanceFromRoot), 2f) : 1f, 0f),
            elasticitySoften = ValidateFloat(advancedToggle ? Mathf.Pow(soften, 2f) : 0f, 0f),
            ignoreRootMotion = ValidateFloat(advancedToggle ? ignoreRootMotion : 0f, 0f),
            gravityMultiplier = float.IsNaN(gravityValue) ? 0f : gravityValue,
            angleLimited = angleLimitToggle,
            angleLimit = ValidateFloat(angleLimit.Evaluate(normalizedDistanceFromRoot), 0f),
            angleLimitSoften = ValidateFloat(angleLimitSoften, 0f),
            blend = 1f,
            drag = ValidateFloat(drag.Evaluate(normalizedDistanceFromRoot), 0.1f),
            airDrag = ValidateFloat(airDrag.Evaluate(normalizedDistanceFromRoot), 0f),
            collisionRadius = float.IsNaN(collisionRadiusValue) ? 0f : collisionRadiusValue,
        };
    }

    public static JiggleTreeInputParameters Default() {
        return new JiggleTreeInputParameters() {
            stiffness = new JiggleTreeCurvedFloat(0.8f),
            angleLimit = new JiggleTreeCurvedFloat(0.5f),
            stretch = new JiggleTreeCurvedFloat(0.1f),
            rootStretch = 0f,
            drag = new JiggleTreeCurvedFloat(0.1f),
            airDrag = new JiggleTreeCurvedFloat(0f),
            ignoreRootMotion = 0f,
            gravity = new JiggleTreeCurvedFloat(1f),
            collisionRadius = new JiggleTreeCurvedFloat(0.1f),
        };
    }

    public void OnValidate() {
        collisionRadius.value = Mathf.Max(0f, collisionRadius.value);
        stiffness.value = Mathf.Clamp01(stiffness.value);
        angleLimit.value = Mathf.Clamp01(angleLimit.value);
        drag.value = Mathf.Clamp01(drag.value);
        airDrag.value = Mathf.Clamp01(airDrag.value);
        ignoreRootMotion = Mathf.Clamp01(ignoreRootMotion);
        stretch.value = Mathf.Clamp01(stretch.value);
        soften = Mathf.Clamp01(soften);
        blend = Mathf.Clamp01(blend);
    }
}

}
