using System;
using System.Drawing.Drawing2D;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct JiggleBoneParameters {
    public float rootElasticity;
    public float angleElasticity;
    public float angleLimit;
    public float lengthElasticity;
    public float elasticitySoften;
    public float gravityMultiplier;
    public float blend;
    public float airDrag;
    public float drag;
}

[Serializable]
public struct JiggleBoneInputParameters {
    public bool advancedToggle;
    public float stiffness;
    public float soften;
    public float angleLimit;
    public float rootStretch;
    public float stretch;
    public float drag;
    public float airDrag;
    public float gravity;
    public float blend;
    
    public JiggleBoneParameters ToJiggleBoneParameters() {
        return new JiggleBoneParameters {
            rootElasticity = advancedToggle?1f-rootStretch:0f,
            angleElasticity = Mathf.Pow(stiffness, 2f),
            lengthElasticity = advancedToggle?Mathf.Pow(1f-stretch,2f):0f,
            elasticitySoften = advancedToggle?Mathf.Pow(soften,2f):0f,
            gravityMultiplier = gravity,
            angleLimit = angleLimit,
            blend = 1f,
            drag = drag,
            airDrag = airDrag
        };
    }
}
