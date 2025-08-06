using System;
using UnityEngine;

public class JiggleUpdater : MonoBehaviour{
    private void LateUpdate() {
        JiggleJobManager.ScheduleUpdate(Time.deltaTime);
        JiggleJobManager.CompleteUpdate();
    }

    private void OnDrawGizmos() {
        //JiggleJobManager.OnDrawGizmos();
    }
}
