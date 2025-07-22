using System;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {

internal class JiggleRigLateUpdateHandler : JiggleRigHandler<JiggleRigLateUpdateHandler> {
    private void LateUpdate() {
        CachedSphereCollider.EnableSphereCollider();
        var gravity = Physics.gravity;
        var deltaTime = Time.deltaTime;
        var timeAsDouble = Time.timeAsDouble;
        var timeAsDoubleOneStepBack = timeAsDouble - JiggleRigBuilder.VERLET_TIME_STEP;
        if (!CachedSphereCollider.TryGet(out SphereCollider sphereCollider)) {
            throw new UnityException( "Failed to create a sphere collider, this should never happen! Is a scene not loaded but a jiggle rig is?");
        }
        foreach (var jiggleRig in jiggleRigs) {
            try {
                jiggleRig.Advance(deltaTime, gravity, timeAsDouble, timeAsDoubleOneStepBack, sphereCollider);
            } catch(SystemException exception) {
                Debug.LogException(exception);
                invalidAdvancables.Add(jiggleRig);
            }
        }
        CachedSphereCollider.DisableSphereCollider();
        JiggleRigBuilder.unitySubsystemLateUpdateRegistration = false;
        CommitRemovalOfInvalidAdvancables();
    }
}

}
