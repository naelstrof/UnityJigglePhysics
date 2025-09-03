using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDemo : MonoBehaviour {
    [SerializeField]
    private GameObject colliderPrefab;
    [SerializeField]
    private GameObject testObject;

    private List<GameObject> colliders;

    void Start() {
        colliders = new List<GameObject>();
        StartCoroutine(SpawnColliders());
        StartCoroutine(DestroyColliders());
        StartCoroutine(MoveColliders());
        StartCoroutine(TestCollider());
    }

    void Update() {
        transform.position += (Vector3.forward * Mathf.PerlinNoise1D(Time.time) + Vector3.right * Mathf.PerlinNoise1D(Time.time*3f)) * (Time.deltaTime);
    }
    
    IEnumerator SpawnColliders() {
        while (isActiveAndEnabled) {
            var obj = Instantiate(colliderPrefab, transform, true);
            obj.transform.position = new Vector3(Mathf.PerlinNoise1D(Time.time*13f)-0.5f, 0f, Mathf.PerlinNoise1D(Time.time*17f)-0.5f)*100f;
            obj.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.05f, 0.4f);
            colliders.Add(obj);
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator DestroyColliders() {
        while (isActiveAndEnabled) {
            if (colliders.Count == 0) {
                yield return null;
                continue;
            }
            var selectedCollider = colliders[UnityEngine.Random.Range(0, colliders.Count)];
            colliders.Remove(selectedCollider);
            Destroy(selectedCollider);
            yield return new WaitForSeconds(2f);
        }
    }

    IEnumerator MoveColliders() {
        while (isActiveAndEnabled) {
            foreach (var collider in colliders) {
                collider.transform.position += Vector3.up*(Mathf.Sin(Time.time*3f) * Time.deltaTime);
            }
            yield return null;
        }
    }

    IEnumerator TestCollider() {
        while (isActiveAndEnabled) {
            if (colliders.Count == 0) {
                yield return null;
                continue;
            }
            var selectedCollider = colliders[UnityEngine.Random.Range(0, colliders.Count)];
            testObject.transform.position = selectedCollider.transform.position +Vector3.forward*0.25f;
            yield return new WaitForSeconds(3f);
        }
    }
}
