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
        var time = Time.timeAsDouble;
        var fixedTime = Time.fixedDeltaTime;
        
        JigglePhysics.SchedulePose(time);
        JigglePhysics.CompletePose();
        if (debugDraw) {
            JigglePhysics.Render(proceduralMaterial, sphereMesh, time, fixedTime);
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
