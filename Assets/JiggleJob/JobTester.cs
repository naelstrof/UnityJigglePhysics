using System;
using UnityEngine;
using Unity.Jobs;

public class JobTester : MonoBehaviour {
    [SerializeField] public Transform[] bones;
    
    private JiggleJob jiggleJob;
    private JobHandle jobHandle;
    private void Update() {
        jiggleJob = new JiggleJob() {
            bones = new Unity.Collections.NativeArray<Matrix4x4>(bones.Length, Unity.Collections.Allocator.TempJob),
            output = new Unity.Collections.NativeArray<Matrix4x4>(bones.Length, Unity.Collections.Allocator.TempJob)
        };
        var boneCount = bones.Length;
        for (int i = 0; i < boneCount; i++) {
            jiggleJob.bones[i] = bones[i].localToWorldMatrix;
        }
        jobHandle = jiggleJob.Schedule();
    }

    private void LateUpdate() {
        jobHandle.Complete();
        var boneCount = bones.Length;
        for (int i = 0; i < boneCount; i++) {
            var position = jiggleJob.output[i].GetPosition();
            var rotation = jiggleJob.output[i].rotation;
            bones[i].SetPositionAndRotation(position, rotation);
        }
        jiggleJob.bones.Dispose();
        jiggleJob.output.Dispose();
    }
}
