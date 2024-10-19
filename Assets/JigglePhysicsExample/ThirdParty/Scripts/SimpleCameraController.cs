using UnityEngine;

namespace UnityTemplateProjects {
    public class SimpleCameraController : MonoBehaviour {
        public enum UpdateMode {
            FixedUpdate,
            LateUpdate,
        }
        public UpdateMode mode;
        private Vector3 vel;
        public Transform targetLookat;
        public void SetNewTarget(Transform target) {
            targetLookat = target;
        }
        public float distance = 10f;
        private Vector2 offset;
        void Simulate() {
            if (Input.GetButton("Fire1") || Input.GetButton("Fire2")) {
                offset += new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"))*2f;
            } else {
                //offset += new Vector2(Input.GetAxis("Horizontal"), 0f)*2f;
            }
            transform.rotation = Quaternion.AngleAxis(offset.x,Vector3.up)*Quaternion.AngleAxis(offset.y,Vector3.right);
            transform.position = Vector3.SmoothDamp(transform.position, targetLookat.position - transform.forward*distance, ref vel, 0.04f);
            transform.rotation = Quaternion.LookRotation((targetLookat.position - transform.position).normalized, Vector3.up);
            distance -= Input.GetAxis("Mouse ScrollWheel");
            distance = Mathf.Max(distance, 1f);
        }
        public void LateUpdate() {
            if (mode == UpdateMode.LateUpdate) {Simulate();}
        }
        public void FixedUpdate() {
            if (mode == UpdateMode.FixedUpdate) {Simulate();}
        }
    }

}