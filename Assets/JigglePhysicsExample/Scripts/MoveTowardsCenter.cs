using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysicsExample {
    public class MoveTowardsCenter : MonoBehaviour {
        void Update() {
            transform.position = Vector3.MoveTowards(transform.position, Vector3.zero, Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.AngleAxis(180f,Vector3.up), Time.deltaTime*180f);
        }
    }
}