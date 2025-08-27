using System;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

public class JiggleUpdateExample : MonoBehaviour {
    private void FixedUpdate() {
        JigglePhysics.ScheduleSimulate(Time.timeAsDouble, Time.fixedTimeAsDouble, Time.fixedDeltaTime);
    }

    private void LateUpdate() {
        JigglePhysics.SchedulePose(Time.timeAsDouble);
        JigglePhysics.CompletePose();
    }

    void OnApplicationQuit() {
        JigglePhysics.Dispose();
    }

    private void OnDrawGizmos() {
        JigglePhysics.OnDrawGizmos();
    }
}

}
