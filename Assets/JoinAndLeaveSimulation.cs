using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoinAndLeaveSimulation : MonoBehaviour {
    [SerializeField]
    List<GameObject> objects = new List<GameObject>();
    void OnEnable() {
        StartCoroutine(Shuffle());
    }

    IEnumerator Shuffle() {
        while (isActiveAndEnabled) {
            //yield return new WaitForSeconds(Random.Range(0f, 1f));
            yield return null;
            int rng = Random.Range(0, objects.Count);
            var obj = objects[rng];
            obj.SetActive(!obj.activeInHierarchy);
        }
    }
}
