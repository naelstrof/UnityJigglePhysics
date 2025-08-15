using Unity.Mathematics;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

[System.Serializable]
public struct JiggleColliderSerializable {
    public Transform transform;
    public JiggleCollider collider;
}

[System.Serializable]
public struct JiggleCollider {
    public enum JiggleColliderType {
        Sphere,
        Capsule,
        Plane
    }

    public JiggleColliderType type;
    
    public float radius;
    public float length;
    
    [HideInInspector] public float4x4 localToWorldMatrix;
}

}