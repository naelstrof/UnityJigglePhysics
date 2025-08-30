using System;
using UnityEngine;

public class BreastDemoMovement : MonoBehaviour {

    [SerializeField] Transform animationTarget;
    [SerializeField] Transform animationTargetCollider;
    [SerializeField] AnimationCurve rotationX;
    [SerializeField] AnimationCurve rotationY;
    [SerializeField] AnimationCurve rotationZ;
    [SerializeField] AnimationCurve colliderPositionX;
    [SerializeField] AnimationCurve colliderPositionY;

    Quaternion initialRotation;
    Vector3 initialColliderPosition;
    
    void Start() {
        initialRotation = animationTarget.transform.rotation;
        initialColliderPosition = animationTargetCollider.position;
    }

    void Update() {
        var t = Mathf.Repeat(Time.timeSinceLevelLoad*0.7f, 10f);
        animationTarget.rotation = 
            initialRotation
            * Quaternion.Euler(rotationX.Evaluate(t) * 90f,0f,rotationZ.Evaluate(t) * 90f)
            * Quaternion.Euler(0f,rotationY.Evaluate(t) * 90f,0f);
        animationTargetCollider.position = 
            initialColliderPosition
            + Vector3.right * colliderPositionX.Evaluate(t)
            + Vector3.up * colliderPositionY.Evaluate(t);
    }
    
}
