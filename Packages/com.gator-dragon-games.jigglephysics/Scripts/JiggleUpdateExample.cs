using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

public class JiggleUpdateExample : MonoBehaviour {
    private void LateUpdate() {
        JigglePhysics.ScheduleUpdate(Time.timeAsDouble);
        JigglePhysics.CompleteUpdate();
    }

    void OnApplicationQuit() {
        JigglePhysics.Dispose();
    }

    private void OnDrawGizmos() {
        JigglePhysics.OnDrawGizmos();
    }
}

}
