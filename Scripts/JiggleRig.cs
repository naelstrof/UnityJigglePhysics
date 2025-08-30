using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GatorDragonGames.JigglePhysics {

public class JiggleRig : MonoBehaviour {
    [SerializeField] private JiggleRigData jiggleRigData;
    [SerializeField, Tooltip("Whether to check if parameters have been changed each frame.")] private bool animatedParameters = false;
    
    private static List<JigglePointParameters> parametersCache;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        parametersCache = new();
    }

    private void OnEnable() {
        jiggleRigData.OnEnable();
    }
    
    private void OnDisable() {
        jiggleRigData.OnDisable();
    }

    private void LateUpdate() {
        if (animatedParameters) {
            jiggleRigData.UpdateParameters(parametersCache);
        }
    }

    void OnValidate() {
        parametersCache ??= new();
        if (!jiggleRigData.hasSerializedData) {
            jiggleRigData = JiggleRigData.Default();
        }
        jiggleRigData.OnValidate();
        jiggleRigData.UpdateParameters(parametersCache);
    }

    private void OnDrawGizmos() {
        if (!isActiveAndEnabled) {
            return;
        }
        jiggleRigData.OnDrawGizmos();
    }

}

}
