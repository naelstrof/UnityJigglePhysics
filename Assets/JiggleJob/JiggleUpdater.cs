using UnityEngine;

public class JiggleUpdater : MonoBehaviour{
    private void LateUpdate() {
        JiggleJobManager.SampleAndStepSimulation(Time.deltaTime);
        JiggleJobManager.SchedulePose();
        JiggleJobManager.CompletePose();
    }
}
