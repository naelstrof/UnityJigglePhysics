using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCamera : MonoBehaviour {
    void LateUpdate() {
        transform.rotation = Quaternion.LookRotation((Camera.main.transform.position - transform.position).normalized, Vector3.up);
    }
}
