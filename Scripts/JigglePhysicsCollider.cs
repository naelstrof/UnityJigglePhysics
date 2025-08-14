using System;
using UnityEngine;

public class JigglePhysicsCollider : MonoBehaviour {
    private int id;
    private void OnEnable() {
        id = JiggleTreeUtility.AddSphere(transform);
    }
    private void OnDisable() {
        //JiggleTreeUtility.RemoveSphere(id);
    }
}
