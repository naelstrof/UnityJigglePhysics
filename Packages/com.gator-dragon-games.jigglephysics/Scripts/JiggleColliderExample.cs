using System;
using GatorDragonGames.JigglePhysics;
using UnityEngine;

public class JiggleColliderExample : MonoBehaviour {
    [SerializeField]
    private JiggleColliderSerializable jiggleCollider;
    private void OnEnable() {
        JigglePhysics.AddJiggleCollider(jiggleCollider);
    }
    private void OnDisable() {
        JigglePhysics.RemoveJiggleCollider(jiggleCollider);
    }

    private void OnDrawGizmos() {
        jiggleCollider.OnDrawGizmos();
    }
}
