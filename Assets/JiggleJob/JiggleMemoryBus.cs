using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class JiggleMemoryBus {// : IContainer<JiggleTreeStruct> {

    private NativeList<JiggleTreeStruct> jiggleTreeStructs;
    private NativeList<JiggleTransform> simulateInputPoses;
    private NativeList<JiggleTransform> restPoseTransforms;
    private NativeList<JiggleTransform> previousLocalRestPoseTransforms;
    private NativeList<float3> rootOutputPositions;
    private NativeList<JiggleTransform> simulationOutputPoses;
    private NativeList<JiggleTransform> interpolationCurrentPoses;
    private NativeList<JiggleTransform> interpolationPreviousPoses;
    private NativeList<JiggleTransform> interpolationOutputPoses;
    private NativeList<float3> simulationOutputRootPositions;
    private NativeList<float3> interpolationCurrentRootPositions;
    private NativeList<float3> interpolationPreviousRootPositions;
    private NativeList<float3> simulationOutputRootOffsets;
    private NativeList<float3> interpolationCurrentRootOffsets;
    private NativeList<float3> interpolationPreviousRootOffsets;
    private NativeList<float3> colliderPositions;
    
    public NativeArray<JiggleTreeStruct> jiggleTreeStructsArray;
    public NativeArray<JiggleTransform> simulateInputPosesArray;
    public NativeArray<JiggleTransform> restPoseTransformsArray;
    public NativeArray<JiggleTransform> previousLocalRestPoseTransformsArray;
    public NativeArray<float3> rootOutputPositionsArray;
    public NativeArray<JiggleTransform> simulationOutputPosesArray;
    public NativeArray<JiggleTransform> interpolationCurrentPosesArray;
    public NativeArray<JiggleTransform> interpolationPreviousPosesArray;
    public NativeArray<JiggleTransform> interpolationOutputPosesArray;
    public NativeArray<float3> simulationOutputRootPositionsArray;
    public NativeArray<float3> interpolationCurrentRootPositionsArray;
    public NativeArray<float3> interpolationPreviousRootPositionsArray;
    public NativeArray<float3> simulationOutputRootOffsetsArray;
    public NativeArray<float3> interpolationCurrentRootOffsetsArray;
    public NativeArray<float3> interpolationPreviousRootOffsetsArray;
    public NativeArray<float3> colliderPositionsArray;
    
    public List<Transform> transformAccessList;
    public List<Transform> transformRootAccessList;
    public List<Transform> colliderTransformAccessList;
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
        
        var tempPoses = interpolationPreviousPosesArray;
        interpolationPreviousPosesArray = interpolationCurrentPosesArray;
        interpolationCurrentPosesArray = simulationOutputPosesArray;
        simulationOutputPosesArray = tempPoses;

        var tempSimulatedRootOffset = interpolationPreviousRootOffsetsArray;
        interpolationPreviousRootOffsetsArray = interpolationCurrentRootOffsetsArray;
        interpolationCurrentRootOffsetsArray = simulationOutputRootOffsetsArray;
        simulationOutputRootOffsetsArray = tempSimulatedRootOffset;

        var tempSimulatedRootPosition = interpolationPreviousRootPositionsArray;
        interpolationPreviousRootPositionsArray = interpolationCurrentRootPositionsArray;
        interpolationCurrentRootPositionsArray = simulationOutputRootPositionsArray;
        simulationOutputRootPositionsArray = tempSimulatedRootPosition;
    }
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
        interpolationCurrentRootPositions = new NativeList<float3>(Allocator.Persistent);
        interpolationPreviousRootPositions = new NativeList<float3>(Allocator.Persistent);
        simulationOutputRootOffsets = new NativeList<float3>(Allocator.Persistent);
        interpolationCurrentRootOffsets = new NativeList<float3>(Allocator.Persistent);
        interpolationPreviousRootOffsets = new NativeList<float3>(Allocator.Persistent);
        transformAccessList = new List<Transform>();
        transformRootAccessList = new List<Transform>();
        colliderTransformAccessArray = new TransformAccessArray(new Transform[] {});
        colliderPositions = new NativeList<float3>(Allocator.Persistent);
        treeCount = 0;
        transformCount = 0;
    }

    public void Add(JiggleTree jiggleTree) {
        var jiggleTreeStruct = jiggleTree.GetStruct();
        jiggleTreeStruct.transformIndexOffset = (uint)transformCount;
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
        interpolationCurrentRootPositions.AddRange(tempfloat3s);
        interpolationPreviousRootPositions.AddRange(tempfloat3s);
        simulationOutputRootOffsets.AddRange(tempfloat3s);
        interpolationCurrentRootOffsets.AddRange(tempfloat3s);
        interpolationPreviousRootOffsets.AddRange(tempfloat3s);
        for (var index = 0; index < jiggleTree.points.Length; index++) {
            transformAccessList.Add(jiggleTree.bones[index]);
            transformRootAccessList.Add(jiggleTree.bones[0]);
        }
        tempJiggleTransforms.Dispose();
        tempfloat3s.Dispose();
        RegnerateAccessArrays();
        RegenerateArrays();
        treeCount++;
        transformCount += (int)jiggleTreeStruct.pointCount;
        jiggleTree.ClearDirty();
    }

    public void Remove(JiggleTree jiggleTree) {
        var jiggleTreeStruct = jiggleTree.GetStruct();
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
        RegenerateArrays();
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

    void RegenerateArrays() {
        if (jiggleTreeStructsArray.IsCreated) {
            jiggleTreeStructsArray.Dispose();
            simulateInputPosesArray.Dispose();
            restPoseTransformsArray.Dispose();
            previousLocalRestPoseTransformsArray.Dispose();
            rootOutputPositionsArray.Dispose();
            simulationOutputPosesArray.Dispose();
            interpolationCurrentPosesArray.Dispose();
            interpolationPreviousPosesArray.Dispose();
            interpolationOutputPosesArray.Dispose();
            simulationOutputRootPositionsArray.Dispose();
            interpolationCurrentRootPositionsArray.Dispose();
            interpolationPreviousRootPositionsArray.Dispose();
            simulationOutputRootOffsetsArray.Dispose();
            interpolationCurrentRootOffsetsArray.Dispose();
            interpolationPreviousRootOffsetsArray.Dispose();
            colliderPositionsArray.Dispose();
        }
        jiggleTreeStructsArray = jiggleTreeStructs.AsArray();
        simulateInputPosesArray = simulateInputPoses.AsArray();
        restPoseTransformsArray = restPoseTransforms.AsArray();
        previousLocalRestPoseTransformsArray = previousLocalRestPoseTransforms.AsArray();
        rootOutputPositionsArray = rootOutputPositions.AsArray();
        simulationOutputPosesArray = simulationOutputPoses.AsArray();
        interpolationCurrentPosesArray = interpolationCurrentPoses.AsArray();
        interpolationPreviousPosesArray = interpolationPreviousPoses.AsArray();
        interpolationOutputPosesArray = interpolationOutputPoses.AsArray();
        simulationOutputRootPositionsArray = simulationOutputRootPositions.AsArray();
        interpolationCurrentRootPositionsArray = interpolationCurrentRootPositions.AsArray();
        interpolationPreviousRootPositionsArray = interpolationPreviousRootPositions.AsArray();
        simulationOutputRootOffsetsArray = simulationOutputRootOffsets.AsArray();
        interpolationCurrentRootOffsetsArray = interpolationCurrentRootOffsets.AsArray();
        interpolationPreviousRootOffsetsArray = interpolationPreviousRootOffsets.AsArray();
        colliderPositionsArray = colliderPositions.AsArray();
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
        interpolationCurrentRootPositions.RemoveRange((int)start, (int)count);
        interpolationPreviousRootPositions.RemoveRange((int)start, (int)count);
        simulationOutputRootOffsets.RemoveRange((int)start, (int)count);
        interpolationCurrentRootOffsets.RemoveRange((int)start, (int)count);
        interpolationPreviousRootOffsets.RemoveRange((int)start, (int)count);
        for (var countdown = 0; countdown < count; countdown++) {
            transformAccessList.RemoveAt((int)start);
            transformRootAccessList.RemoveAt((int)start);
        }
        transformCount -= (int)count;
    }

    public void Dispose() {
        if (jiggleTreeStructsArray.IsCreated) {
            jiggleTreeStructsArray.Dispose();
            simulateInputPosesArray.Dispose();
            restPoseTransformsArray.Dispose();
            previousLocalRestPoseTransformsArray.Dispose();
            rootOutputPositionsArray.Dispose();
            simulationOutputPosesArray.Dispose();
            interpolationCurrentPosesArray.Dispose();
            interpolationPreviousPosesArray.Dispose();
            interpolationOutputPosesArray.Dispose();
            simulationOutputRootPositionsArray.Dispose();
            interpolationCurrentRootPositionsArray.Dispose();
            interpolationPreviousRootPositionsArray.Dispose();
            simulationOutputRootOffsetsArray.Dispose();
            interpolationCurrentRootOffsetsArray.Dispose();
            interpolationPreviousRootOffsetsArray.Dispose();
            colliderPositionsArray.Dispose();
        }

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
