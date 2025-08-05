using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoinAndLeaveSimulation : MonoBehaviour {
    [SerializeField]
    List<GameObject> objects = new List<GameObject>();
    IEnumerator Start() {
        var waitForSeconds = new WaitForSeconds(1f);
        while (true) {
            yield return waitForSeconds;
            int rng = UnityEngine.Random.Range(0, objects.Count);
            var obj = objects[rng];
            obj.SetActive(!obj.activeInHierarchy);
        }
    }
}
