using System;
using UnityEngine;
using Unity.Jobs;

public class JobTester : MonoBehaviour {
    [SerializeField] public Transform[] bones;

    private void OnEnable() {
        JiggleJobManager.AddJiggleTree(bones);
    }

    private void OnDisable() {
    }
    
    private void LateUpdate() {
        JiggleJobManager.Update(Time.deltaTime);
        JiggleJobManager.Pose();
    }
}
