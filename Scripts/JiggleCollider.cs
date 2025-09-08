using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace GatorDragonGames.JigglePhysics {

[Serializable]
public struct JiggleColliderSerializable {
    public Transform transform;
    public JiggleCollider collider;

    public void OnDrawGizmosSelected() {
        if (transform == null) {
            return;
        }
        var position = transform.position;
        collider.Read(transform);
#if UNITY_6000_OR_NEWER
        Gizmos.color = Color.goldenRod;
#else
        Gizmos.color = Color.yellow;
#endif
        switch (collider.type) {
            case JiggleCollider.JiggleColliderType.Sphere:
                Gizmos.DrawWireSphere(position, collider.worldRadius);
            break;
        }
    }
}

[System.Serializable]
public struct JiggleCollider {
    public enum JiggleColliderType {
        Sphere,
        //Capsule,
        //Plane
    }

    [NonSerialized] public bool enabled;
    
    public JiggleColliderType type;
    
    public float radius;
    [NonSerialized] public float worldRadius;
    //public float length;
    
    [NonSerialized] public float4x4 localToWorldMatrix;
    private float AverageScale(float4x4 matrix) {
        float sx = math.length(matrix.c0.xyz);
        float sy = math.length(matrix.c1.xyz);
        float sz = math.length(matrix.c2.xyz);
        return (sx + sy + sz) / 3f;
    }
    public void Read(Transform transform) {
        Read(transform.localToWorldMatrix);
    }
    public void Read(TransformAccess transform) {
        Read(transform.localToWorldMatrix);
    }
    public void Read(float4x4 matrix) {
        localToWorldMatrix = matrix;
        var averageScale = AverageScale(localToWorldMatrix);
        worldRadius = radius * averageScale;
    }
}

}
