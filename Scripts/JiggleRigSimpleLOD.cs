#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace JigglePhysics {
    public class JiggleRigSimpleLOD : JiggleRigLOD {

        [Tooltip("Distance to disable the jiggle rig.")]
        [SerializeField] float distance = 20f;
        [Tooltip("Distance past distance from which it blends out rather than instantly disabling.")]
        [SerializeField] float blend = 5f;
        
        private static Camera currentCamera;


        private bool TryGetCamera(out Camera camera) {
            #if UNITY_EDITOR
            if (EditorWindow.focusedWindow is SceneView view) {
                camera = view.camera;
                return camera;
            }
            #endif
            if (!currentCamera || !currentCamera.CompareTag("MainCamera")) {
                currentCamera = Camera.main;
            }
            camera = currentCamera;
            return currentCamera;
        }
        protected override bool CheckActive() {
            if (!TryGetCamera(out Camera camera)) {
                return false;
            }

            var position = transform.position;
            var currentBlend = (Vector3.Distance(camera.transform.position, position) - distance + blend) / blend;
            currentBlend = Mathf.Clamp01(1f-currentBlend);
            foreach (var jiggle in jiggles) {
                jiggle.blend = currentBlend;
            }
            return Vector3.Distance(camera.transform.position, position) < distance;
        }

    }

}