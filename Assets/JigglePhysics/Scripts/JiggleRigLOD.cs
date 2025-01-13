using System;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {
    [DisallowMultipleComponent]
    public abstract class JiggleRigLOD : MonoBehaviour {
        protected List<IJiggleBlendable> jiggles;
        protected bool wasActive = true;

        protected virtual void Awake() {
            jiggles = new List<IJiggleBlendable>();
        }

        public virtual void AddTrackedJiggleRig(IJiggleBlendable blendable) {
            if (jiggles.Contains(blendable)) return;
            jiggles.Add(blendable);
            if (blendable.enabled != wasActive) {
                blendable.enabled = wasActive;
            }
        }
        public virtual void RemoveTrackedJiggleRig(IJiggleBlendable blendable) {
            if (!jiggles.Contains(blendable)) return;
            jiggles.Remove(blendable);
        }

        private void Update() {
            if (!CheckActive()) {
                if (wasActive) {
                    foreach (var jiggle in jiggles) {
                        jiggle.enabled = false;
                    }
                }
                wasActive = false;
                return;
            }
            if (!wasActive) {
                foreach (var jiggle in jiggles) {
                    jiggle.enabled = true;
                }
            }
            wasActive = true;
        }
        
        protected abstract bool CheckActive();

        private void OnDisable() {
            foreach (var jiggle in jiggles) {
                jiggle.enabled = false;
            }
        }
    }
}