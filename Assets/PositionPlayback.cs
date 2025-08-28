using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionPlayback : MonoBehaviour {
    [SerializeField] private PositionRecording recording;
    private float length;
    private float startTime;
    void OnEnable() {
        float min = float.MaxValue;
        float max = float.MaxValue;
        foreach (var key in recording.curveX.keys) {
            if (key.time < min) min = key.time;
            if (key.time > max) max = key.time;
        }
        startTime = min;
        length = max - min;
    }
    private void Update() {
        transform.position = recording.GetPosition(Mathf.Repeat(Time.time, length)+startTime);
    }
}
