using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour {
    IEnumerator Start() {
        DontDestroyOnLoad(gameObject);
        int levelCount = SceneManager.sceneCountInBuildSettings;
        for(int i=1;i<levelCount;i++) {
            var handle = SceneManager.LoadSceneAsync(i, LoadSceneMode.Single);
            while (handle != null && !handle.isDone) {
                yield return null;
            }
            yield return new WaitForSeconds(20f);
        }
    }
}
