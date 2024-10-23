#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace JigglePhysics {
    public class JiggleRigRendererLOD : JiggleRigLOD {

        [Tooltip("Distance to disable the jiggle rig.")]
        [SerializeField] float distance = 20f;
        [Tooltip("Distance past distance from which it blends out rather than instantly disabling.")]
        [SerializeField] float blend = 5f;
        
        private static Camera currentCamera;
        
        public bool[] Visible;
        public bool LastVisiblity;
        public int VisibleCount;
        protected override void Awake() {
            base.Awake();
            MonoBehaviorHider.JiggleRigLODRenderComponent jiggleRigVisibleFlag = null;
            var renderers = GetComponentsInChildren<Renderer>();
            VisibleCount = renderers.Length;
            Visible = new bool[VisibleCount];
            for (int i = 0; i < VisibleCount; i++) {
                Renderer renderer = renderers[i];
                if (!renderer) continue;
                if (!renderer.TryGetComponent(out jiggleRigVisibleFlag)) {
                    jiggleRigVisibleFlag = renderer.gameObject.AddComponent<MonoBehaviorHider.JiggleRigLODRenderComponent>();
                }
                Visible[i] = renderer.isVisible;
                var index = i;
                jiggleRigVisibleFlag.VisibilityChange += (visible) => {
                    // Check if the index is out of bounds
                    if (index < 0 || index >= Visible.Length) {
                        Debug.LogError("Index out of bounds: " + index + ". Valid range is 0 to " + (Visible.Length - 1));
                        return;
                    }
                    // Update the visibility at the specified index
                    Visible[index] = visible;
                    // Re-evaluate visibility
                    RevalulateVisiblity();
                };
            }
            RevalulateVisiblity();
        }
        private void RevalulateVisiblity() {
            for (int visibleIndex = 0; visibleIndex < VisibleCount; visibleIndex++) {
                if (Visible[visibleIndex]) {
                    LastVisiblity = true;
                    return;
                }
            }
            LastVisiblity = false;
        }

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
            if (LastVisiblity == false) {
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