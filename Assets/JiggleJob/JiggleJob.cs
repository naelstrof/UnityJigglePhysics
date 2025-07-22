using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct JiggleJob : IJob {
    // TODO: doubles are strictly a bad way to count, probably should be ints or longs.
    public double previousTimeStamp;
    public double timeStamp;
    
    public NativeArray<Matrix4x4> bones;
    
    public NativeArray<Matrix4x4> previousOutput;
    
    public NativeArray<Matrix4x4> output;
    public void Execute() {
        int boneCount = bones.Length;
        var delta = timeStamp-previousTimeStamp;
        for (int i = 0; i < boneCount; i++) {
            output[i] = bones[i] * Matrix4x4.Translate(Vector3.forward);
        }
    }
}
