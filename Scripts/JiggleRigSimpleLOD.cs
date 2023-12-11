using System;
using System.Collections;
using System.Collections.Generic;
using JigglePhysics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace JigglePhysics {

    [CreateAssetMenu(fileName = "JiggleRigSimpleLOD", menuName = "JigglePhysics/JiggleRigSimpleLOD", order = 1)]
    public class JiggleRigSimpleLOD : JiggleRigLOD {

        [Tooltip("Distance to disable the jiggle rig")]
        [SerializeField] float distance;
        [Tooltip("Level of detail manager. This system will control how the jiggle rig saves performance cost.")]
        [SerializeField] float blend;
        [NonSerialized] private Camera currentCamera;

        private bool TryGetCamera(out Camera camera) {
            #if UNITY_EDITOR
            if (EditorWindow.focusedWindow is SceneView view) {
                camera = view.camera;
                return camera != null;
            }
            #endif
            if (currentCamera == null || !currentCamera.CompareTag("MainCamera")) {
                currentCamera = Camera.main;
            }
            camera = currentCamera;
            return currentCamera != null;
        }

        [NonSerialized] Transform cameraTransform;

        public override bool CheckActive(Vector3 position) {
            if (!TryGetCamera(out Camera camera)) {
                return false;
            }
            return Vector3.Distance(camera.transform.position, position) < distance;
        }

        public override JiggleSettingsData AdjustJiggleSettingsData(Vector3 position, JiggleSettingsData data) {
            if (!TryGetCamera(out Camera camera)) {
                return data;
            }
            
            var currentBlend = (Vector3.Distance(camera.transform.position, position) - distance + blend) / blend;
            currentBlend = Mathf.Clamp01(1f-currentBlend);
            data.blend = currentBlend;
            return data;
        }

    }

}