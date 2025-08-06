using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class JiggleMemoryBus {// : IContainer<JiggleTreeStruct> {

    public NativeList<JiggleTreeStruct> jiggleTreeStructs;
    public NativeList<JiggleTransform> simulateInputPoses;
    public NativeList<JiggleTransform> restPoseTransforms;
    public NativeList<JiggleTransform> previousLocalRestPoseTransforms;
    public NativeList<float3> rootOutputPositions;
    public NativeList<JiggleTransform> simulationOutputPoses;
    public NativeList<JiggleTransform> interpolationCurrentPoses;
    public NativeList<JiggleTransform> interpolationPreviousPoses;
    public NativeList<JiggleTransform> interpolationOutputPoses;
    public NativeList<float3> simulationOutputRootPositions;
    public NativeList<float3> interpolationCurrentPositions;
    public NativeList<float3> interpolationPreviousRootPositions;
    public NativeList<float3> simulationOutputRootOffsets;
    public NativeList<float3> interpolationCurrentOffsets;
    public NativeList<float3> interpolationPreviousRootOffsets;
    public NativeList<float3> colliderPositions;
    public List<Transform> transformAccessList;
    public List<Transform> transformRootAccessList;
    public List<Transform> colliderTransformAccessList;
    public TransformAccessArray transformAccessArray;
    public TransformAccessArray transformRootAccessArray;
    public TransformAccessArray colliderTransformAccessArray;
    
    public int treeCount { get; private set; }
    public int transformCount { get; private set; }

    public JiggleMemoryBus() {
        jiggleTreeStructs = new NativeList<JiggleTreeStruct>(Allocator.Persistent);
        simulateInputPoses = new NativeList<JiggleTransform>(Allocator.Persistent);
        restPoseTransforms = new NativeList<JiggleTransform>(Allocator.Persistent);
        previousLocalRestPoseTransforms = new NativeList<JiggleTransform>(Allocator.Persistent);
        rootOutputPositions = new NativeList<float3>(Allocator.Persistent);
        simulationOutputPoses = new NativeList<JiggleTransform>(Allocator.Persistent);
        interpolationCurrentPoses = new NativeList<JiggleTransform>(Allocator.Persistent);
        interpolationPreviousPoses = new NativeList<JiggleTransform>(Allocator.Persistent);
        interpolationOutputPoses = new NativeList<JiggleTransform>(Allocator.Persistent);
        simulationOutputRootPositions = new NativeList<float3>(Allocator.Persistent);
        interpolationCurrentPositions = new NativeList<float3>(Allocator.Persistent);
        interpolationPreviousRootPositions = new NativeList<float3>(Allocator.Persistent);
        simulationOutputRootOffsets = new NativeList<float3>(Allocator.Persistent);
        interpolationCurrentOffsets = new NativeList<float3>(Allocator.Persistent);
        interpolationPreviousRootOffsets = new NativeList<float3>(Allocator.Persistent);
        transformAccessList = new List<Transform>();
        transformRootAccessList = new List<Transform>();
        colliderTransformAccessArray = new TransformAccessArray(new Transform[] {});
        colliderPositions = new NativeList<float3>(Allocator.Persistent);
    }

    public void Add(JiggleTree jiggleTree, JiggleTreeStruct jiggleTreeStruct) {
        jiggleTreeStructs.Add(jiggleTreeStruct);
        var tempJiggleTransforms = new NativeArray<JiggleTransform>((int) jiggleTreeStruct.pointCount, Allocator.Temp);
        var tempfloat3s = new NativeArray<float3>((int) jiggleTreeStruct.pointCount, Allocator.Temp);
        simulateInputPoses.AddRange(tempJiggleTransforms);
        restPoseTransforms.AddRange(tempJiggleTransforms);
        previousLocalRestPoseTransforms.AddRange(tempJiggleTransforms);
        rootOutputPositions.AddRange(tempfloat3s);
        simulationOutputPoses.AddRange(tempJiggleTransforms);
        interpolationCurrentPoses.AddRange(tempJiggleTransforms);
        interpolationPreviousPoses.AddRange(tempJiggleTransforms);
        interpolationOutputPoses.AddRange(tempJiggleTransforms);
        simulationOutputRootPositions.AddRange(tempfloat3s);
        interpolationCurrentPositions.AddRange(tempfloat3s);
        interpolationPreviousRootPositions.AddRange(tempfloat3s);
        simulationOutputRootOffsets.AddRange(tempfloat3s);
        interpolationCurrentOffsets.AddRange(tempfloat3s);
        interpolationPreviousRootOffsets.AddRange(tempfloat3s);
        for (var index = 0; index < jiggleTree.points.Length; index++) {
            transformAccessList.Add(jiggleTree.bones[index]);
            transformRootAccessList.Add(jiggleTree.bones[index]);
        }
        tempJiggleTransforms.Dispose();
        tempfloat3s.Dispose();
        RegnerateAccessArrays();
        treeCount++;
        transformCount += (int)jiggleTreeStruct.pointCount;
        jiggleTree.ClearDirty();
    }

    public void Remove(JiggleTreeStruct jiggleTreeStruct) {
        var removed = false;
        var removedPointCount = 0u;
        for (var index = 0; index < jiggleTreeStructs.Length; index++) {
            if (!removed) {
                if (jiggleTreeStructs[index] == jiggleTreeStruct) {
                    jiggleTreeStructs.RemoveAt(index);
                    index--;
                    removedPointCount = jiggleTreeStruct.pointCount;
                    removed = true;
                    RemovePointAndTransformRange(jiggleTreeStruct.transformIndexOffset, jiggleTreeStruct.pointCount);
                }
                continue;
            }
            // Adjusting indices of remaining jiggleTreeStructs
            var modifiedJiggleTreeStruct = jiggleTreeStructs[index];
            modifiedJiggleTreeStruct.transformIndexOffset -= removedPointCount;
            jiggleTreeStructs[index] = modifiedJiggleTreeStruct;
        }
        RegnerateAccessArrays();
        treeCount--;
    }

    void RegnerateAccessArrays() {
        if (transformAccessArray.isCreated) {
            transformAccessArray.Dispose();
        }
        if (transformRootAccessArray.isCreated) {
            transformRootAccessArray.Dispose();
        }
        transformAccessArray = new TransformAccessArray(transformAccessList.ToArray());
        transformRootAccessArray = new TransformAccessArray(transformRootAccessList.ToArray());
    }

    public void RemovePointAndTransformRange(uint start, uint count) {
        simulateInputPoses.RemoveRange((int)start, (int)count);
        restPoseTransforms.RemoveRange((int)start, (int)count);
        previousLocalRestPoseTransforms.RemoveRange((int)start, (int)count);
        rootOutputPositions.RemoveRange((int)start, (int)count);
        simulationOutputPoses.RemoveRange((int)start, (int)count);
        interpolationCurrentPoses.RemoveRange((int)start, (int)count);
        interpolationPreviousPoses.RemoveRange((int)start, (int)count);
        interpolationOutputPoses.RemoveRange((int)start, (int)count);
        simulationOutputRootPositions.RemoveRange((int)start, (int)count);
        interpolationCurrentPositions.RemoveRange((int)start, (int)count);
        interpolationPreviousRootPositions.RemoveRange((int)start, (int)count);
        simulationOutputRootOffsets.RemoveRange((int)start, (int)count);
        interpolationCurrentOffsets.RemoveRange((int)start, (int)count);
        interpolationPreviousRootOffsets.RemoveRange((int)start, (int)count);
        for (var countdown = 0; countdown < count; countdown++) {
            transformAccessList.RemoveAt((int)start);
            transformRootAccessList.RemoveAt((int)start);
        }
        transformCount -= (int)count;
    }

    public void Dispose() {
        if (transformAccessArray.isCreated) {
            transformAccessArray.Dispose();
        }
        if (colliderTransformAccessArray.isCreated) {
            colliderTransformAccessArray.Dispose();
        }
        if (transformRootAccessArray.isCreated) {
            transformRootAccessArray.Dispose();
        }
    }
    
}
