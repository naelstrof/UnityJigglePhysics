using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {
    public class JiggleSurfaceApproximator : MonoBehaviour {
        public Transform outputTransform;
        public SkinnedMeshRenderer targetRenderer;
        public Transform basisTransform;
        public JiggleSoftbody softbodyPhysics;
        private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
        public Color softbodySurfaceColor = new Color(0.213f,0,0,0);
        [System.Serializable]
        public class TransformBlendshapePair {
            public Transform approximateTransform;
            public string blendshapeName;
            [HideInInspector]
            public int blendshapeID;
        }
        public List<TransformBlendshapePair> blendshapePairs;
        private void Start() {
            foreach (var p in blendshapePairs) {
                p.blendshapeID = targetRenderer.sharedMesh.GetBlendShapeIndex(p.blendshapeName);
            }
        }
        void LateUpdate() {
            Vector3 offset = Vector3.zero;
            //Quaternion rOffset = Quaternion.identity;
            foreach(var p in blendshapePairs) {
                Vector3 approx = Vector3.LerpUnclamped(basisTransform.position, p.approximateTransform.position, targetRenderer.GetBlendShapeWeight(p.blendshapeID) / 100f);
                offset += approx-basisTransform.position;
                //Quaternion rApprox = Quaternion.Inverse(basisTransform.rotation) * Quaternion.LerpUnclamped(basisTransform.rotation, p.approximateTransform.rotation, targetRenderer.GetBlendShapeWeight(p.blendshapeID) / 100f);
                //rOffset = rApprox * rOffset;
            }
            outputTransform.position = softbodyPhysics.TransformPoint(basisTransform.position + offset, softbodySurfaceColor);
            outputTransform.rotation = basisTransform.rotation;
        }
    }
}