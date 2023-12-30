using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {
public static class CachedSphereCollider {
    private class DestroyListener : MonoBehaviour {
        void OnDestroy() {
            _hasSphere = false;
        }
    }
    private static bool _hasSphere = false;
    private static SphereCollider _sphereCollider;

    public static void StartPass() {
        if (TryGet(out SphereCollider collider)) {
            collider.enabled = true;
        }
    }

    public static void FinishedPass() {
        if (TryGet(out SphereCollider collider)) {
            collider.enabled = false;
        }
    }

    public static bool TryGet(out SphereCollider collider) {
        if (_hasSphere) {
            collider = _sphereCollider;
            return true;
        }
        try {
            var obj = new GameObject("JiggleBoneSphereCollider", typeof(SphereCollider), typeof(DestroyListener)) {
                hideFlags = HideFlags.HideAndDontSave
            };
            if (Application.isPlaying) {
                Object.DontDestroyOnLoad(obj);
            }

            _sphereCollider = obj.GetComponent<SphereCollider>();
            collider = _sphereCollider;
            collider.enabled = false;
            _hasSphere = true;
            return true;
        } catch {
            // Something went wrong! Try to clean up and try again next frame. Better throwing an expensive exception than spawning spheres every frame.
            if (_sphereCollider != null) {
                if (Application.isPlaying) {
                    Object.Destroy(_sphereCollider.gameObject);
                } else {
                    Object.DestroyImmediate(_sphereCollider.gameObject);
                }
            }
            _hasSphere = false;
            collider = null;
            throw;
        }
    }
}
}