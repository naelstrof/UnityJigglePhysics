using System;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GatorDragonGames.JigglePhysics {

public class JiggleRig : MonoBehaviour {
    [SerializeField] private JiggleRigData jiggleRigData;

    private void OnEnable() {
        jiggleRigData.OnEnable();
    }
    
    private void OnDisable() {
        jiggleRigData.OnDisable();
    }

    void OnValidate() {
        if (!jiggleRigData.hasSerializedData) {
            jiggleRigData = JiggleRigData.Default();
        }
        jiggleRigData.OnValidate();
    }

    private void OnDrawGizmos() {
        jiggleRigData.OnDrawGizmos();
    }

#if UNITY_EDITOR
    public void OnSceneGUI() {
        var cam = SceneView.lastActiveSceneView.camera;
        jiggleRigData.OnSceneGUI(cam);
    }
#endif

}

}
