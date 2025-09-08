using System;
using GatorDragonGames.JigglePhysics;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {
public class JiggleColliderExample : MonoBehaviour {
    [SerializeField] private JiggleColliderSerializable jiggleCollider;

    private void OnEnable() {
        JigglePhysics.AddJiggleCollider(jiggleCollider);
    }

    private void OnDisable() {
        JigglePhysics.RemoveJiggleCollider(jiggleCollider);
    }

    private void OnDrawGizmos() {
        jiggleCollider.OnDrawGizmosSelected();
    }
}
}
