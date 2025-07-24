using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Jobs;

public class JiggleJobDoubleBuffer {
    
    public struct JiggleJobOutput {
        public double timeStamp;
        public Matrix4x4[] output;
    }

    private int flips = 0;
    private bool flipped;
    public JiggleJob jobA;
    public JiggleJob jobB;
    public JiggleBulkTransformRead bulkRead;
    public TransformAccessArray transformAccessArray;
    public JiggleJobOutput previousJobOutput;
    private ref JiggleJob inProgressJob => ref flipped ? ref jobB : ref jobA;
    private ref JiggleJob finishedJob => ref flipped ? ref jobA : ref jobB;
    public bool HasData() => flips >= 3;
    
    public double finishedTimestamp => finishedJob.timeStamp;
    public NativeArray<Matrix4x4> finishedOutput => finishedJob.output;

    public void ReadBones() {
        var handle = bulkRead.Schedule(transformAccessArray);
        handle.Complete();
        inProgressJob.transformMatrices.CopyFrom(bulkRead.matrices);
        inProgressJob.timeStamp = Time.timeAsDouble;
        inProgressJob.gravity = Physics.gravity;
    }

    public JobHandle Schedule() {
        return inProgressJob.Schedule();
    }
    
    public JiggleJobDoubleBuffer(Transform[] bones, JiggleBoneSimulatedPoint[] points) {
        var boneCount = bones.Length;
        var pointCount = points.Length;
        jobA = new JiggleJob() {
            transformMatrices = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
            simulatedPoints = new NativeArray<JiggleBoneSimulatedPoint>(pointCount, Allocator.Persistent),
            output = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
        };
        jobB = new JiggleJob() {
            transformMatrices = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
            simulatedPoints = new NativeArray<JiggleBoneSimulatedPoint>(pointCount, Allocator.Persistent),
            output = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
        };
        previousJobOutput = new JiggleJobOutput() {
            timeStamp = 0,
            output = new Matrix4x4[boneCount]
        };

        bulkRead = new JiggleBulkTransformRead() {
            matrices = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent),
        };
        
        jobA.simulatedPoints.CopyFrom(points);
        jobB.simulatedPoints.CopyFrom(points);
        
        transformAccessArray = new TransformAccessArray(bones);
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
