using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

public class JiggleUpdater : MonoBehaviour {
    private void LateUpdate() {
        JiggleJobManager.ScheduleUpdate(Time.deltaTime);
        JiggleJobManager.CompleteUpdate();
    }

    void OnApplicationQuit() {
        JiggleTreeUtility.Dispose();
    }

    private void OnDrawGizmos() {
        JiggleJobManager.OnDrawGizmos();
    }
}

}
