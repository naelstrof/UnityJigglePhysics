using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

public class MonoBehaviorHider {
    public class JiggleRigLODRenderComponent : MonoBehaviour {
        public Action<bool> VisibilityChange;
        public void OnBecameInvisible() {
            VisibilityChange?.Invoke(false);
        }

        public void OnBecameVisible() {
            VisibilityChange?.Invoke(true);
        }

        private void OnDisable() {
            VisibilityChange?.Invoke(false);
        }
    }
}

}
