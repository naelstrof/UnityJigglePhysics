using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnAndDeleteSimulation : MonoBehaviour {
    [SerializeField]
    private GameObject obj;

    private void OnEnable() {
        StartCoroutine(SpawnAndDelete());
    }

    IEnumerator SpawnAndDelete() {
        while (isActiveAndEnabled) {
            var instance = Instantiate(obj, Random.insideUnitSphere * 10f, Random.rotation);
            if (Random.Range(0f, 1f) < 0.5f) {
                Destroy(instance, 0f);
            } else {
                Destroy(instance, Random.Range(3f,6f));
            }
            yield return null;
        }
    }
}
