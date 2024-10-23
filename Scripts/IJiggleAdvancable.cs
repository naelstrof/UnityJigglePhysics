using UnityEngine;

namespace JigglePhysics {
[System.Serializable]
public enum JiggleUpdateMode {
    LateUpdate,
    FixedUpdate
}
public interface IJiggleAdvancable {
    public void Advance(float dt, Vector3 gravity, double timeAsDouble, double timeAsDoubleOneStepBack, SphereCollider sphereCollider);
}

public interface IJiggleBlendable {
    public bool enabled { get; set; }
    public float blend { get; set; }
}

}
