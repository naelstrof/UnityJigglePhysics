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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init() {
        if (_sphereCollider != null) {
            if (Application.isPlaying) {
                Object.Destroy(_sphereCollider);
            } else {
                Object.DestroyImmediate(_sphereCollider);
            }
        }
        _hasSphere = false;
        _sphereCollider = null;
    }
    public static void EnableSphereCollider() {
        if (TryGet(out SphereCollider collider)) {
            collider.enabled = true;
        }
    }

    public static void DisableSphereCollider() {
        if (TryGet(out SphereCollider collider)) {
            collider.enabled = false;
        }
    }

    public static bool TryGet(out SphereCollider collider) {
        if (_hasSphere) {
            collider = _sphereCollider;
            return true;
        }

        GameObject obj = null;
        try {
            obj = new GameObject("JiggleBoneSphereCollider", typeof(SphereCollider), typeof(DestroyListener)) {
                hideFlags = HideFlags.DontSave
            };
            Object.DontDestroyOnLoad(obj);

            if (!obj.TryGetComponent(out _sphereCollider)) {
                throw new UnityException("This should never happen...");
            }
            collider = _sphereCollider;
            collider.enabled = false;
            _hasSphere = true;
            return true;
        } catch {
            // Something went wrong! Try to clean up and try again next frame. Better throwing an expensive exception than spawning spheres every frame.
            if (obj) {
                if (Application.isPlaying) {
                    Object.Destroy(obj);
                } else {
                    Object.DestroyImmediate(obj);
                }
            }
            _hasSphere = false;
            collider = null;
            throw;
        }
    }
}
}