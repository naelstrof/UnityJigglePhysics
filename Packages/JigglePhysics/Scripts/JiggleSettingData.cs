using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

public struct JiggleSettingsData {
    private float _friction;
    public float friction {
        get => _friction;
        set {
            _friction = value;
            frictionOneMinus = 1f - _friction;
        }
    }

    private float _airDrag;
    public float airDrag {
        get => _airDrag;
        set {
            _airDrag = value;
            airDragOneMinus = 1f - _airDrag;
        }
    }

    private float _angleElasticity;

    public float angleElasticity {
        get => _angleElasticity;
        set {
            _angleElasticity = value;
            squaredAngleElasticity = _angleElasticity * _angleElasticity;
        }
    }

    private float _lengthElasticity;

    public float lengthElasticity {
        get => _lengthElasticity;
        set {
            _lengthElasticity = value;
            squaredLengthElasticity = _lengthElasticity * _lengthElasticity;
        }
    }
    
    private float _elasticitySoften;

    public float elasticitySoften {
        get => _elasticitySoften;
        set {
            _elasticitySoften = value;
            doubleElasticitySoften = _elasticitySoften * 2f;
        }
    }
    
    public float gravityMultiplier;
    public float blend;
    public float radiusMultiplier;

    public float squaredAngleElasticity;
    public float squaredLengthElasticity;
    public float airDragOneMinus;
    public float frictionOneMinus;
    public float doubleElasticitySoften;
    
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

}
