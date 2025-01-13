using System;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

internal class JiggleRigHandler<T> : MonoBehaviour where T : MonoBehaviour {
    private static T instance;

    protected static List<IJiggleAdvancable> jiggleRigs;
    protected static HashSet<IJiggleAdvancable> invalidAdvancables;

    protected void CommitRemovalOfInvalidAdvancables() {
        if (invalidAdvancables.Count == 0) {
            return;
        }
        jiggleRigs.RemoveAll(IsInvalid);
    }

    private bool IsInvalid(IJiggleAdvancable jiggleRig) {
        return invalidAdvancables.Contains(jiggleRig);
    }

    private static void CreateInstanceIfNeeded() {
        if (instance) {
            return;
        }

        jiggleRigs ??= new List<IJiggleAdvancable>();
        invalidAdvancables ??= new HashSet<IJiggleAdvancable>();

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
        jiggleRigs = null;
        invalidAdvancables = null;
        if (Application.isPlaying) {
            Destroy(instance.gameObject);
        } else {
            DestroyImmediate(instance.gameObject);
        }
        instance = null;
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

    private void OnDisable() {
        if (instance == this) {
            instance = null;
        }
    }

    private void OnEnable() {
        if (instance != this && instance != null) {
            if (Application.isPlaying) {
                Destroy(gameObject);
            } else {
                DestroyImmediate(gameObject);
            }
        }
    }
}

}
