using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Elevator : MonoBehaviour {
    [SerializeField]
    private float multiplier = 3f;
    [SerializeField]
    private AnimationCurve curve;
    private Rigidbody body;
    void Start() {
        body = GetComponent<Rigidbody>();
    }
    void FixedUpdate() {
        float vel = curve.Differentiate(Mathf.Repeat(Time.time*0.25f, 1f));
        body.linearVelocity = -Vector3.up*vel*multiplier;
    }
    void OnValidate() {
        curve.preWrapMode = WrapMode.Loop;
        curve.postWrapMode = WrapMode.Loop;
    }
}
