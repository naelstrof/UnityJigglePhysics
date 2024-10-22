using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

internal class JiggleRigFixedUpdateHandler : JiggleRigHandler<JiggleRigFixedUpdateHandler> {
    private void FixedUpdate() {
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
