using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class PositionRecord : MonoBehaviour {
    [SerializeField] private PositionRecording recording;

    private void OnEnable() {
        recording.Clear();
    }

    private void Update() {
        recording.Record(Time.timeSinceLevelLoad, transform.position);
    }

    private void OnDisable() {
        recording.CalculateOffset();
        #if UNITY_EDITOR
        EditorUtility.SetDirty(recording);
        #endif
    }
}
