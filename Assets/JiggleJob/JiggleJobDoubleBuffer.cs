using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class JiggleJobDoubleBuffer {
    
    public struct JiggleJobOutput {
        public double timeStamp;
        public Matrix4x4[] output;
    }

    private int flips = 0;
    private bool flipped;
    public JiggleJob jobA;
    public JiggleJob jobB;
    public JiggleJobOutput previousJobOutput;
    private ref JiggleJob inProgressJob => ref flipped ? ref jobB : ref jobA;
    private ref JiggleJob finishedJob => ref flipped ? ref jobA : ref jobB;
    public bool HasData() => flips >= 3;
    
    public double finishedTimestamp => finishedJob.timeStamp;
    public NativeArray<Matrix4x4> finishedOutput => finishedJob.output;

    public void SetBones(Transform[] bones) {
        var boneCount = bones.Length;
        for (int i = 0; i < boneCount; i++) {
            inProgressJob.bones[i] = bones[i].localToWorldMatrix;
        }
        // TODO: inProgressJob.bones.CopyFrom(bones[].coolparralleljoblocalmatrixes);
        // Ideally we can take advantage of IJobParallelForTransform to just let it set the transforms
        inProgressJob.timeStamp = Time.timeAsDouble;
    }

    public JobHandle Schedule() {
        return inProgressJob.Schedule();
    }
    
    public JiggleJobDoubleBuffer(int boneCount) {
        jobA = new JiggleJob() {
            bones = new Unity.Collections.NativeArray<Matrix4x4>(boneCount, Unity.Collections.Allocator.Persistent),
            output = new Unity.Collections.NativeArray<Matrix4x4>(boneCount, Unity.Collections.Allocator.Persistent),
        };
        jobB = new JiggleJob() {
            bones = new Unity.Collections.NativeArray<Matrix4x4>(boneCount, Unity.Collections.Allocator.Persistent),
            output = new Unity.Collections.NativeArray<Matrix4x4>(boneCount, Unity.Collections.Allocator.Persistent),
        };
        previousJobOutput = new JiggleJobOutput() {
            timeStamp = 0,
            output = new Matrix4x4[boneCount]
        };
        flipped = false;
    }

    public void Flip() {
        previousJobOutput.timeStamp = finishedJob.timeStamp;
        for (int i = 0; i < previousJobOutput.output.Length; i++) {
            previousJobOutput.output[i] = finishedJob.output[i];
        }
        flips++;
        flipped = !flipped;
    }
}
