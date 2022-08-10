using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LateUpdateScript : MonoBehaviour {
    [SerializeField, Range(0,80)]
    private float amplitude;
    [SerializeField, Range(0,10)]
    private float freq;
    void LateUpdate() {
        transform.position = Vector3.forward * (Mathf.PerlinNoise(Time.time*freq, 0f) * amplitude);
    }
}
