using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

    public class JiggleRigLOD : ScriptableObject {
        public virtual bool CheckActive(Vector3 position) => true;
        public virtual JiggleSettingsData AdjustJiggleSettingsData(Vector3 position, JiggleSettingsData data) => data;
    }

}