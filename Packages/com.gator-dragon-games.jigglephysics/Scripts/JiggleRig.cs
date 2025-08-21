using System;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GatorDragonGames.JigglePhysics {

public class JiggleRig : MonoBehaviour {
    [SerializeField] private JiggleRigData[] jiggleRigs;

    private void OnEnable() {
        var rigCount = jiggleRigs.Length;
        for (int i = 0; i < rigCount; i++) {
            jiggleRigs[i].OnEnable();
        }
    }
    
    private void OnDisable() {
        var rigCount = jiggleRigs.Length;
        for (int i = 0; i < rigCount; i++) {
            jiggleRigs[i].OnDisable();
        }
    }

    void OnValidate() {
        if (jiggleRigs == null || jiggleRigs.Length == 0) {
            return;
        }
        var rigCount = jiggleRigs.Length;
        for (int i = 0; i < rigCount; i++) {
            if (!jiggleRigs[i].hasSerializedData) {
                jiggleRigs[i] = JiggleRigData.Default();
            }
            jiggleRigs[i].OnValidate();
        }
    }

#if UNITY_EDITOR
    public void OnSceneGUI() {
        if (jiggleRigs == null) {
            return;
        }
        
        var cam = SceneView.lastActiveSceneView.camera;
        var rigCount = jiggleRigs.Length;
        for (int i = 0; i < rigCount; i++) {
            jiggleRigs[i].OnSceneGUI(cam);
        }
    }
#endif

}

}
