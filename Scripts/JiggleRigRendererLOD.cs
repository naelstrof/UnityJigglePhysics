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
        
        private bool[] visible;
        private bool lastVisibility;
        private int visibleCount;
        
        protected override void Awake() {
            base.Awake();
            MonoBehaviorHider.JiggleRigLODRenderComponent jiggleRigVisibleFlag = null;
            var renderers = GetComponentsInChildren<Renderer>();
            visibleCount = renderers.Length;
            visible = new bool[visibleCount];
            for (int i = 0; i < visibleCount; i++) {
                Renderer renderer = renderers[i];
                if (!renderer) continue;
                if (!renderer.TryGetComponent(out jiggleRigVisibleFlag)) {
                    jiggleRigVisibleFlag = renderer.gameObject.AddComponent<MonoBehaviorHider.JiggleRigLODRenderComponent>();
                }
                visible[i] = renderer.isVisible;
                var index = i;
                jiggleRigVisibleFlag.VisibilityChange += (visible) => {
                    // Check if the index is out of bounds
                    if (index < 0 || index >= this.visible.Length) {
                        Debug.LogError("Index out of bounds: " + index + ". Valid range is 0 to " + (this.visible.Length - 1));
                        return;
                    }
                    // Update the visibility at the specified index
                    this.visible[index] = visible;
                    // Re-evaluate visibility
                    RevalulateVisiblity();
                };
            }
            RevalulateVisiblity();
        }
        private void RevalulateVisiblity() {
            for (int visibleIndex = 0; visibleIndex < visibleCount; visibleIndex++) {
                if (visible[visibleIndex]) {
                    lastVisibility = true;
                    return;
                }
            }
            lastVisibility = false;
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
            if (lastVisibility == false) {
                return false;
            }
            if (!TryGetCamera(out Camera camera)) {
                return false;
            }

            var position = transform.position;
            var cameraDistance = Vector3.Distance(camera.transform.position, position);
            var currentBlend = (cameraDistance - distance + blend) / blend;
            currentBlend = Mathf.Clamp01(1f-currentBlend);
            foreach (var jiggle in jiggles) {
                jiggle.blend = currentBlend;
            }
            return cameraDistance < distance;
        }

    }

}