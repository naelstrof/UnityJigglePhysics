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
        if (!isActiveAndEnabled) {
            return;
        }
        jiggleRigData.OnDrawGizmos();
    }

}

}
