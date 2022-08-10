using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LateUpdateScript : MonoBehaviour {
    void LateUpdate() {
        transform.position = Vector3.forward * (Time.time*5f + Mathf.Sin(Time.time));
    }
}
