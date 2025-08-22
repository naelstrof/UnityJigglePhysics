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
    public JiggleTreeCurvedFloat stretch;
    public JiggleTreeCurvedFloat drag;
    public JiggleTreeCurvedFloat airDrag;
    public JiggleTreeCurvedFloat gravity;
    public JiggleTreeCurvedFloat collisionRadius;
    public float blend;

    public JigglePointParameters ToJigglePointParameters(float normalizedDistanceFromRoot, float lossyScaleCached, float lossyScaleReal) {
        float scaleCorrection = lossyScaleCached*(1f/(lossyScaleReal*lossyScaleReal));
        
        return new JigglePointParameters {
            rootElasticity = advancedToggle ? 1f - rootStretch : 0f,
            angleElasticity = Mathf.Pow(stiffness.Evaluate(normalizedDistanceFromRoot), 2f),
            lengthElasticity = advancedToggle
                ? Mathf.Pow(1f - stretch.Evaluate(normalizedDistanceFromRoot), 2f)
                : 1f,
            elasticitySoften = advancedToggle ? Mathf.Pow(soften, 2f) : 0f,
            gravityMultiplier = gravity.Evaluate(normalizedDistanceFromRoot),
            angleLimited = angleLimitToggle,
            angleLimit = angleLimit.Evaluate(normalizedDistanceFromRoot),
            angleLimitSoften = angleLimitSoften,
            blend = 1f,
            drag = drag.Evaluate(normalizedDistanceFromRoot),
            airDrag = airDrag.Evaluate(normalizedDistanceFromRoot),
            collisionRadius = collisionToggle ? collisionRadius.Evaluate(normalizedDistanceFromRoot) * scaleCorrection : 0f,
        };
    }

    public static JiggleTreeInputParameters Default() {
        return new JiggleTreeInputParameters() {
            stiffness = new JiggleTreeCurvedFloat(0.8f),
            angleLimit = new JiggleTreeCurvedFloat(0.5f),
            stretch = new JiggleTreeCurvedFloat(0.1f),
            rootStretch = 0.1f,
            drag = new JiggleTreeCurvedFloat(0.1f),
            airDrag = new JiggleTreeCurvedFloat(0f),
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
        stretch.value = Mathf.Clamp01(stretch.value);
        soften = Mathf.Clamp01(soften);
        blend = Mathf.Clamp01(blend);
    }
}

}
