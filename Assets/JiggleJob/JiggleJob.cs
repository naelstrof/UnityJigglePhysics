using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct JiggleJob : IJob {
    public NativeArray<Matrix4x4> bones;
    public NativeArray<Matrix4x4> output;
    public void Execute() {
        int boneCount = bones.Length;
        for (int i = 0; i < boneCount; i++) {
            output[i] = bones[i] * Matrix4x4.Translate(Vector3.forward*0.01f);
        }
    }
}
