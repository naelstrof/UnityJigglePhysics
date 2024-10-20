using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

internal class JiggleRigLateUpdateHandler : JiggleRigHandler<JiggleRigLateUpdateHandler> {
    private void LateUpdate() {
        CachedSphereCollider.EnableSphereCollider();
        foreach (var jiggleRig in jiggleRigs) {
            jiggleRig.Advance(Time.deltaTime);
        }
        CachedSphereCollider.DisableSphereCollider();
    }
}

}
