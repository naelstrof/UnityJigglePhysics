using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {
public abstract class JiggleSettingsBase : ScriptableObject {
    public abstract JiggleSettingsData GetData();
    public virtual float GetRadius(float normalizedIndex) {
        return 0f;
    }
}

}