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
        //Capsule,
        //Plane
    }

    [HideInInspector] public bool enabled;
    
    public JiggleColliderType type;
    
    public float radius;
    [HideInInspector] public float worldRadius;
    //public float length;
    
    [HideInInspector] public float4x4 localToWorldMatrix;
}

}