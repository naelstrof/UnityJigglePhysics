using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

internal class JiggleRigFixedUpdateHandler : JiggleRigHandler<JiggleRigFixedUpdateHandler> {
    private void FixedUpdate() {
        CachedSphereCollider.EnableSphereCollider();
        foreach (var jiggleRig in jiggleRigs) {
            jiggleRig.Advance(Time.deltaTime);
        }
        CachedSphereCollider.DisableSphereCollider();
    }
}

}
