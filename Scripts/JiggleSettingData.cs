using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct JiggleSettingsData {
    public float gravityMultiplier;
    public float friction;
    public float angleElasticity;
    public float blend;
    public float airDrag;
    public float lengthElasticity;
    public float elasticitySoften;
    public float radiusMultiplier;

    public static JiggleSettingsData Lerp(JiggleSettingsData a, JiggleSettingsData b, float t) {
        return new JiggleSettingsData {
            gravityMultiplier = Mathf.Lerp(a.gravityMultiplier, b.gravityMultiplier, t),
            friction = Mathf.Lerp(a.friction, b.friction, t),
            angleElasticity = Mathf.Lerp(a.angleElasticity, b.angleElasticity, t),
            blend = Mathf.Lerp(a.blend, b.blend, t),
            airDrag = Mathf.Lerp(a.airDrag, b.airDrag, t),
            lengthElasticity = Mathf.Lerp(a.lengthElasticity, b.lengthElasticity, t),
            elasticitySoften = Mathf.Lerp(a.elasticitySoften, b.elasticitySoften, t),
            radiusMultiplier = Mathf.Lerp(a.radiusMultiplier, b.radiusMultiplier, t),
        };
    }
}
