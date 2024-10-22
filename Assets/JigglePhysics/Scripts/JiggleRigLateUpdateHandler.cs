using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

internal class JiggleRigLateUpdateHandler : JiggleRigHandler<JiggleRigLateUpdateHandler> {
    private void LateUpdate() {
        CachedSphereCollider.EnableSphereCollider();
        var gravity = Physics.gravity;
        var time = Time.deltaTime;
        foreach (var jiggleRig in jiggleRigs) {
            jiggleRig.Advance(time, gravity);
        }
        CachedSphereCollider.DisableSphereCollider();
    }
}

}
