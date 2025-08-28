using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PositionRecording", menuName = "Data/PositionRecording", order = 1)]
public class PositionRecording : ScriptableObject {
    [SerializeField] public AnimationCurve curveX;
    [SerializeField] public AnimationCurve curveY;
    [SerializeField] public AnimationCurve curveZ;

    [SerializeField]
    public Vector3 offset;

    public void Clear() {
        curveX = new AnimationCurve();
        curveY = new AnimationCurve();
        curveZ = new AnimationCurve();
    }
    
    public void Record(float time, Vector3 position) {
        curveX.AddKey(time, position.x);
        curveY.AddKey(time, position.y);
        curveZ.AddKey(time, position.z);
    }

    public Vector3 GetPosition(float time) {
        return new Vector3(curveX.Evaluate(time), curveY.Evaluate(time), curveZ.Evaluate(time)) + offset;
    }

    private void CalculateRange(AnimationCurve curve, out float keyMin, out float keyMax) {
        float min = float.MaxValue;
        float max = float.MinValue;
        foreach (var key in curve.keys) {
            if (key.value < min) min = key.value;
            if (key.value > max) max = key.value;
        }
        keyMin = min;
        keyMax = max;
    }

    public void CalculateOffset() {
        CalculateRange(curveX, out float minX, out float maxX);
        CalculateRange(curveY, out float minY, out float maxY);
        CalculateRange(curveZ, out float minZ, out float maxZ);
        offset = new Vector3(-(minX + maxX) * 0.5f, -(minY + maxY) * 0.5f, -(minZ + maxZ) * 0.5f);
    }
}
