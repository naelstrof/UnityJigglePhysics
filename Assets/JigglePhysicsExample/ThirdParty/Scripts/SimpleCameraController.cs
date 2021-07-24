using UnityEngine;

namespace UnityTemplateProjects {
    public class SimpleCameraController : MonoBehaviour {
        public Transform targetLookat;
        public void SetNewTarget(Transform target) {
            targetLookat = target;
        }
        public float distance = 10f;
        private Vector2 offset;
        public void LateUpdate() {
            if (Input.GetButton("Fire1") || Input.GetButton("Fire2")) {
                offset += new Vector2(Input.GetAxis("Mouse X") + Input.GetAxis("Horizontal"), -Input.GetAxis("Mouse Y"))*2f;
            } else {
                offset += new Vector2(Input.GetAxis("Horizontal"), 0f)*2f;
            }
            transform.rotation = Quaternion.AngleAxis(offset.x,Vector3.up)*Quaternion.AngleAxis(offset.y,Vector3.right);
            transform.position = Vector3.Lerp(transform.position, targetLookat.position - transform.forward*distance, Time.deltaTime*4f);
            transform.rotation = Quaternion.LookRotation((targetLookat.position - transform.position).normalized, Vector3.up);
            distance -= Input.GetAxis("Mouse ScrollWheel") + Input.GetAxis("Vertical")*Time.deltaTime*8f;
            distance = Mathf.Max(distance, 1f);
        }
    }

}