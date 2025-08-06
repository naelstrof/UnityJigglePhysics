using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class JiggleMemoryBus {// : IContainer<JiggleTreeStruct> {
    private List<JiggleTreeStruct> jiggleTreeStructsList;
    private List<JiggleTransform> simulateInputPosesList;
    private List<JiggleTransform> restPoseTransformsList;
    private List<JiggleTransform> previousLocalRestPoseTransformsList;
    private List<float3> rootOutputPositionsList;
    private List<JiggleTransform> simulationOutputPosesList;
    private List<JiggleTransform> interpolationCurrentPosesList;
    private List<JiggleTransform> interpolationPreviousPosesList;
    private List<JiggleTransform> interpolationOutputPosesList;
    private List<float3> simulationOutputRootPositionsList;
    private List<float3> interpolationCurrentRootPositionsList;
    private List<float3> interpolationPreviousRootPositionsList;
    private List<float3> simulationOutputRootOffsetsList;
    private List<float3> interpolationCurrentRootOffsetsList;
    private List<float3> interpolationPreviousRootOffsetsList;
    private List<float3> colliderPositionsList;
    
    public NativeArray<JiggleTreeStruct> jiggleTreeStructs;
    public NativeArray<JiggleTransform> simulateInputPoses;
    public NativeArray<JiggleTransform> restPoseTransforms;
    public NativeArray<JiggleTransform> previousLocalRestPoseTransforms;
    public NativeArray<float3> rootOutputPositions;
    public NativeArray<JiggleTransform> simulationOutputPoses;
    public NativeArray<JiggleTransform> interpolationCurrentPoses;
    public NativeArray<JiggleTransform> interpolationPreviousPoses;
    public NativeArray<JiggleTransform> interpolationOutputPoses;
    public NativeArray<float3> simulationOutputRootPositions;
    public NativeArray<float3> interpolationCurrentRootPositions;
    public NativeArray<float3> interpolationPreviousRootPositions;
    public NativeArray<float3> simulationOutputRootOffsets;
    public NativeArray<float3> interpolationCurrentRootOffsets;
    public NativeArray<float3> interpolationPreviousRootOffsets;
    public NativeArray<float3> colliderPositions;
    
    private List<Transform> transformAccessList;
    private List<Transform> transformRootAccessList;
    private List<Transform> colliderTransformAccessList;
    
    public TransformAccessArray transformAccessArray;
    public TransformAccessArray transformRootAccessArray;
    public TransformAccessArray colliderTransformAccessArray;
    
    public int treeCount { get; private set; }
    public int transformCount { get; private set; }

    public void RotateBuffers() {
        var tempPosesa = interpolationPreviousPoses;
        interpolationPreviousPoses = interpolationCurrentPoses;
        interpolationCurrentPoses = simulationOutputPoses;
        simulationOutputPoses = tempPosesa;

        var tempSimulatedRootOffseta = interpolationPreviousRootOffsets;
        interpolationPreviousRootOffsets = interpolationCurrentRootOffsets;
        interpolationCurrentRootOffsets = simulationOutputRootOffsets;
        simulationOutputRootOffsets = tempSimulatedRootOffseta;

        var tempSimulatedRootPositiona = interpolationPreviousRootPositions;
        interpolationPreviousRootPositions = interpolationCurrentRootPositions;
        interpolationCurrentRootPositions = simulationOutputRootPositions;
        simulationOutputRootPositions = tempSimulatedRootPositiona;
    }
    public JiggleMemoryBus() {
        jiggleTreeStructsList = new();
        simulateInputPosesList = new();
        restPoseTransformsList = new();
        previousLocalRestPoseTransformsList = new();
        rootOutputPositionsList = new();
        simulationOutputPosesList = new();
        interpolationCurrentPosesList = new();
        interpolationPreviousPosesList = new();
        interpolationOutputPosesList = new();
        simulationOutputRootPositionsList = new();
        interpolationCurrentRootPositionsList = new();
        interpolationPreviousRootPositionsList = new();
        simulationOutputRootOffsetsList = new();
        interpolationCurrentRootOffsetsList = new();
        interpolationPreviousRootOffsetsList = new();
        interpolationOutputPoses.Dispose();
        
        WriteOut();
        
        transformAccessList = new List<Transform>();
        transformRootAccessList = new List<Transform>();
        colliderTransformAccessArray = new TransformAccessArray(new Transform[] {});
        colliderPositions = new NativeArray<float3>(new float3[]{}, Allocator.Persistent);
        treeCount = 0;
        transformCount = 0;
    }

    private void ReadIn() {
        if (!jiggleTreeStructs.IsCreated) {
            return;
        }
        
        jiggleTreeStructsList.Clear();
        jiggleTreeStructsList.AddRange(jiggleTreeStructs.ToArray());

        simulateInputPosesList.Clear();
        simulateInputPosesList.AddRange(simulateInputPoses.ToArray());
        
        restPoseTransformsList.Clear();
        restPoseTransformsList.AddRange(restPoseTransforms.ToArray());
        
        previousLocalRestPoseTransformsList.Clear();
        previousLocalRestPoseTransformsList.AddRange(previousLocalRestPoseTransforms.ToArray());
        
        rootOutputPositionsList.Clear();
        rootOutputPositionsList.AddRange(rootOutputPositions.ToArray());
        
        simulationOutputPosesList.Clear();
        simulationOutputPosesList.AddRange(simulationOutputPoses.ToArray());
        
        interpolationCurrentPosesList.Clear();
        interpolationCurrentPosesList.AddRange(interpolationCurrentPoses.ToArray());
        
        interpolationPreviousPosesList.Clear();
        interpolationPreviousPosesList.AddRange(interpolationPreviousPoses.ToArray());
        
        interpolationOutputPosesList.Clear();
        interpolationOutputPosesList.AddRange(interpolationOutputPoses.ToArray());
        
        interpolationCurrentRootPositionsList.Clear();
        interpolationCurrentRootPositionsList.AddRange(interpolationCurrentRootPositions.ToArray());
        
        interpolationPreviousRootPositionsList.Clear();
        interpolationPreviousRootPositionsList.AddRange(interpolationPreviousRootPositions.ToArray());
        
        simulationOutputRootOffsetsList.Clear();
        simulationOutputRootOffsetsList.AddRange(simulationOutputRootOffsets.ToArray());
        
        interpolationCurrentRootOffsetsList.Clear();
        interpolationCurrentRootOffsetsList.AddRange(interpolationCurrentRootOffsets.ToArray());
        
        interpolationPreviousRootOffsetsList.Clear();
        interpolationPreviousRootOffsetsList.AddRange(interpolationPreviousRootOffsets.ToArray());
    }

    private void WriteOut() {
        if (jiggleTreeStructs.IsCreated) {
            jiggleTreeStructs.Dispose();
            simulateInputPoses.Dispose();
            restPoseTransforms.Dispose();
            previousLocalRestPoseTransforms.Dispose();
            rootOutputPositions.Dispose();
            simulationOutputPoses.Dispose();
            interpolationCurrentPoses.Dispose();
            interpolationPreviousPoses.Dispose();
            interpolationOutputPoses.Dispose();
            interpolationCurrentRootPositions.Dispose();
            interpolationPreviousRootPositions.Dispose();
            simulationOutputRootOffsets.Dispose();
            interpolationCurrentRootOffsets.Dispose();
            interpolationPreviousRootOffsets.Dispose();
        }
        jiggleTreeStructs = new NativeArray<JiggleTreeStruct>(jiggleTreeStructsList.ToArray(), Allocator.Persistent);
        simulateInputPoses = new NativeArray<JiggleTransform>(simulateInputPosesList.ToArray(), Allocator.Persistent);
        restPoseTransforms = new NativeArray<JiggleTransform>(restPoseTransformsList.ToArray(), Allocator.Persistent);
        previousLocalRestPoseTransforms = new NativeArray<JiggleTransform>(previousLocalRestPoseTransformsList.ToArray(), Allocator.Persistent);
        rootOutputPositions = new NativeArray<float3>(rootOutputPositionsList.ToArray(), Allocator.Persistent);
        simulationOutputPoses = new NativeArray<JiggleTransform>(simulationOutputPosesList.ToArray(), Allocator.Persistent);
        interpolationCurrentPoses = new NativeArray<JiggleTransform>(interpolationCurrentPosesList.ToArray(), Allocator.Persistent);
        interpolationPreviousPoses = new NativeArray<JiggleTransform>(interpolationPreviousPosesList.ToArray(), Allocator.Persistent);
        interpolationOutputPoses = new NativeArray<JiggleTransform>(interpolationOutputPosesList.ToArray(), Allocator.Persistent);
        simulationOutputRootPositions = new NativeArray<float3>(simulationOutputRootPositionsList.ToArray(), Allocator.Persistent);
        interpolationCurrentRootPositions = new NativeArray<float3>(interpolationCurrentRootPositionsList.ToArray(), Allocator.Persistent);
        interpolationPreviousRootPositions = new NativeArray<float3>(interpolationPreviousRootPositionsList.ToArray(), Allocator.Persistent);
        simulationOutputRootOffsets = new NativeArray<float3>(simulationOutputRootOffsetsList.ToArray(), Allocator.Persistent);
        interpolationCurrentRootOffsets = new NativeArray<float3>(interpolationCurrentRootOffsetsList.ToArray(), Allocator.Persistent);
        interpolationPreviousRootOffsets = new NativeArray<float3>(interpolationPreviousRootOffsetsList.ToArray(), Allocator.Persistent);
    }

    public void Add(JiggleTree jiggleTree) {
        ReadIn();
        var jiggleTreeStruct = jiggleTree.GetStruct();
        jiggleTreeStruct.transformIndexOffset = (uint)transformCount;
        jiggleTreeStructsList.Add(jiggleTreeStruct);
        var tempJiggleTransforms = new JiggleTransform[jiggleTreeStruct.pointCount];
        var tempfloat3s = new float3[jiggleTreeStruct.pointCount];
        simulateInputPosesList.AddRange(tempJiggleTransforms);
        restPoseTransformsList.AddRange(tempJiggleTransforms);
        previousLocalRestPoseTransformsList.AddRange(tempJiggleTransforms);
        rootOutputPositionsList.AddRange(tempfloat3s);
        simulationOutputPosesList.AddRange(tempJiggleTransforms);
        interpolationCurrentPosesList.AddRange(tempJiggleTransforms);
        interpolationPreviousPosesList.AddRange(tempJiggleTransforms);
        interpolationOutputPosesList.AddRange(tempJiggleTransforms);
        simulationOutputRootPositionsList.AddRange(tempfloat3s);
        interpolationCurrentRootPositionsList.AddRange(tempfloat3s);
        interpolationPreviousRootPositionsList.AddRange(tempfloat3s);
        simulationOutputRootOffsetsList.AddRange(tempfloat3s);
        interpolationCurrentRootOffsetsList.AddRange(tempfloat3s);
        interpolationPreviousRootOffsetsList.AddRange(tempfloat3s);
        for (var index = 0; index < jiggleTree.points.Length; index++) {
            transformAccessList.Add(jiggleTree.bones[index]);
            transformRootAccessList.Add(jiggleTree.bones[0]);
        }
        RegnerateAccessArrays();
        WriteOut();
        treeCount++;
        transformCount += (int)jiggleTreeStruct.pointCount;
        jiggleTree.ClearDirty();
    }

    public void Remove(JiggleTree jiggleTree) {
        ReadIn();
        var jiggleTreeStruct = jiggleTree.GetStruct();
        var removed = false;
        var removedPointCount = 0u;
        for (var index = 0; index < jiggleTreeStructs.Length; index++) {
            if (!removed) {
                if (jiggleTreeStructs[index] == jiggleTreeStruct) {
                    jiggleTreeStructsList.RemoveAt(index);
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
        WriteOut();
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

    private void RemovePointAndTransformRange(uint start, uint count) {
        simulateInputPosesList.RemoveRange((int)start, (int)count);
        restPoseTransformsList.RemoveRange((int)start, (int)count);
        previousLocalRestPoseTransformsList.RemoveRange((int)start, (int)count);
        rootOutputPositionsList.RemoveRange((int)start, (int)count);
        simulationOutputPosesList.RemoveRange((int)start, (int)count);
        interpolationCurrentPosesList.RemoveRange((int)start, (int)count);
        interpolationPreviousPosesList.RemoveRange((int)start, (int)count);
        interpolationOutputPosesList.RemoveRange((int)start, (int)count);
        simulationOutputRootPositionsList.RemoveRange((int)start, (int)count);
        interpolationCurrentRootPositionsList.RemoveRange((int)start, (int)count);
        interpolationPreviousRootPositionsList.RemoveRange((int)start, (int)count);
        simulationOutputRootOffsetsList.RemoveRange((int)start, (int)count);
        interpolationCurrentRootOffsetsList.RemoveRange((int)start, (int)count);
        interpolationPreviousRootOffsetsList.RemoveRange((int)start, (int)count);
        transformAccessList.RemoveRange((int)start, (int)count);
        transformRootAccessList.RemoveRange((int)start, (int)count);
        transformCount -= (int)count;
    }

    public void Dispose() {
        if (jiggleTreeStructs.IsCreated) {
            jiggleTreeStructs.Dispose();
            simulateInputPoses.Dispose();
            restPoseTransforms.Dispose();
            previousLocalRestPoseTransforms.Dispose();
            rootOutputPositions.Dispose();
            simulationOutputPoses.Dispose();
            interpolationCurrentPoses.Dispose();
            interpolationPreviousPoses.Dispose();
            interpolationOutputPoses.Dispose();
            simulationOutputRootPositions.Dispose();
            interpolationCurrentRootPositions.Dispose();
            interpolationPreviousRootPositions.Dispose();
            simulationOutputRootOffsets.Dispose();
            interpolationCurrentRootOffsets.Dispose();
            interpolationPreviousRootOffsets.Dispose();
            colliderPositions.Dispose();
        }

        if (transformAccessArray.isCreated) {
            transformAccessArray.Dispose();
        }

        if (transformRootAccessArray.isCreated) {
            transformRootAccessArray.Dispose();
        }
        
        if (colliderTransformAccessArray.isCreated) {
            colliderTransformAccessArray.Dispose();
        }
    }
    
}
