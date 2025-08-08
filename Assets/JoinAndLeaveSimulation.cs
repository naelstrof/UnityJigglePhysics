using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoinAndLeaveSimulation : MonoBehaviour {
    [SerializeField]
    List<GameObject> objects = new List<GameObject>();
    IEnumerator Start() {
        while (true) {
            yield return new WaitForSeconds(0.25f);
            int rng = Random.Range(0, objects.Count);
            var obj = objects[rng];
            obj.SetActive(!obj.activeInHierarchy);
        }
    }
}
