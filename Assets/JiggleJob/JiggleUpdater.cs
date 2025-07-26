using UnityEngine;

public class JiggleUpdater : MonoBehaviour{
    private void LateUpdate() {
        JiggleJobManager.Update(Time.deltaTime);
        JiggleJobManager.Pose();
    }
}
