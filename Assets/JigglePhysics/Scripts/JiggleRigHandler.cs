using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

internal class JiggleRigHandler<T> : MonoBehaviour where T : MonoBehaviour {
    private static T instance;

    protected static List<IJiggleAdvancable> jiggleRigs = new();

    private static void CreateInstanceIfNeeded() {
        if (instance) {
            return;
        }
        
        var obj = new GameObject("JiggleRigHandler", typeof(T)) {
            hideFlags = HideFlags.DontSave
        };
        if (!obj.TryGetComponent(out instance)) {
            throw new UnityException("Should never happen!");
        }
        DontDestroyOnLoad(obj);
    }

    private static void RemoveInstanceIfNeeded() {
        if (jiggleRigs.Count != 0) return;
        if (!instance) return;
        if (Application.isPlaying) {
            Destroy(instance.gameObject);
        } else {
            DestroyImmediate(instance.gameObject);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        jiggleRigs.Clear();
    }

    internal static void AddJiggleRigAdvancable(IJiggleAdvancable advancable) {
        CreateInstanceIfNeeded();
        if (jiggleRigs.Contains(advancable)) {
            return;
        }
        jiggleRigs.Add(advancable);
    }

    internal static void RemoveJiggleRigAdvancable(IJiggleAdvancable advancable) {
        if (!jiggleRigs.Contains(advancable)) {
            RemoveInstanceIfNeeded();
            return;
        }
        jiggleRigs.Remove(advancable);
        RemoveInstanceIfNeeded();
    }

    private void OnDestroy() {
        if (instance == this) {
            instance = null;
        }
    }
}

}
