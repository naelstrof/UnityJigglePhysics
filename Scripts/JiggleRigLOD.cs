using UnityEngine;

namespace JigglePhysics {
    public abstract class JiggleRigLOD : ScriptableObject {
        public abstract bool CheckActive(Vector3 position);
        public abstract JiggleSettingsData AdjustJiggleSettingsData(Vector3 position, JiggleSettingsData data);
    }
}