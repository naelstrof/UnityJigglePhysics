using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

internal class JiggleRigLateUpdateHandler : JiggleRigHandler<JiggleRigLateUpdateHandler> {
    private void LateUpdate() {
        CachedSphereCollider.EnableSphereCollider();
        var gravity = Physics.gravity;
        var deltaTime = Time.deltaTime;
        var timeAsDouble = Time.timeAsDouble;
        foreach (var jiggleRig in jiggleRigs) {
            jiggleRig.Advance(deltaTime, gravity, timeAsDouble);
        }
        CachedSphereCollider.DisableSphereCollider();
    }
}

}
