using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

internal class JiggleRigFixedUpdateHandler : JiggleRigHandler<JiggleRigFixedUpdateHandler> {
    private void FixedUpdate() {
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
