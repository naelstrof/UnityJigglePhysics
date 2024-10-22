using UnityEngine;

namespace JigglePhysics {
[System.Serializable]
public enum JiggleUpdateMode {
    LateUpdate,
    FixedUpdate
}
internal interface IJiggleAdvancable {
    public void Advance(float dt, Vector3 gravity, double timeAsDouble, double timeAsDoubleOneStepBack, SphereCollider sphereCollider);
}

}
