using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

internal class JiggleRigFixedUpdateHandler : JiggleRigHandler<JiggleRigFixedUpdateHandler> {
    private void FixedUpdate() {
        CachedSphereCollider.EnableSphereCollider();
        var gravity = Physics.gravity;
        var deltaTime = Time.deltaTime;
        var timeAsDouble = Time.timeAsDouble;
        var timeAsDoubleOneStepBack = timeAsDouble - JiggleRigBuilder.VERLET_TIME_STEP;
        if (!CachedSphereCollider.TryGet(out SphereCollider sphereCollider)) {
            throw new UnityException( "Failed to create a sphere collider, this should never happen! Is a scene not loaded but a jiggle rig is?");
        }
        foreach (var jiggleRig in jiggleRigs) {
            jiggleRig.Advance(deltaTime, gravity, timeAsDouble, timeAsDoubleOneStepBack, sphereCollider);
        }
        CachedSphereCollider.DisableSphereCollider();
    }
}

}
