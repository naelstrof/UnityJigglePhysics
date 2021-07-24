using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysicsExample {
    public class FPSLimiter : MonoBehaviour {
        public void SetFPS(float fps) {
            Application.targetFrameRate = (int)fps;
        }
    }
}
