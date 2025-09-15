using System;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

public class JiggleUpdateExample : MonoBehaviour {
    [SerializeField] private bool debugDraw = false;
    [SerializeField] private Material proceduralMaterial;
    [SerializeField] private Mesh sphereMesh;

    private void FixedUpdate() {
        var fixedTime = Time.fixedTimeAsDouble;
        JigglePhysics.ScheduleSimulate(fixedTime, Time.fixedDeltaTime);
    }

    private void LateUpdate() {
        var time = Time.timeAsDouble;
        
        JigglePhysics.SchedulePose(time);
        if (debugDraw) {
            JigglePhysics.ScheduleRender();
        }
        
        JigglePhysics.CompletePose();
        if (debugDraw) {
            JigglePhysics.CompleteRender(proceduralMaterial, sphereMesh);
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
