using System;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

public class JiggleUpdateExample : MonoBehaviour {
    [SerializeField] private bool debugDraw = false;
    [SerializeField] private Material proceduralMaterial;
    [SerializeField] private Mesh sphereMesh;
    private void FixedUpdate() {
        JigglePhysics.ScheduleSimulate(Time.timeAsDouble, Time.fixedTimeAsDouble, Time.fixedDeltaTime);
    }

    private void LateUpdate() {
        JigglePhysics.SchedulePose(Time.timeAsDouble);
        JigglePhysics.CompletePose();
        if (debugDraw) {
            JigglePhysics.Render(proceduralMaterial, sphereMesh);
        }
    }

    void OnApplicationQuit() {
        JigglePhysics.Dispose();
    }

    private void OnDrawGizmos() {
        JigglePhysics.OnDrawGizmos();
    }
}

}
