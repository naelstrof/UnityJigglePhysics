using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class JiggleMemoryBus {// : IContainer<JiggleTreeStruct> {
    private int treeCapacity = 0;
    private int transformCapacity = 0;
    private JiggleTreeStruct[] jiggleTreeStructsArray;
    private JiggleTransform[] simulateInputPosesArray;
    private JiggleTransform[] restPoseTransformsArray;
    private JiggleTransform[] previousLocalRestPoseTransformsArray;
    private float3[] rootOutputPositionsArray;
    private JiggleTransform[] simulationOutputPosesArray;
    private JiggleTransform[] interpolationCurrentPosesArray;
    private JiggleTransform[] interpolationPreviousPosesArray;
    private JiggleTransform[] interpolationOutputPosesArray;
    private float3[] simulationOutputRootPositionsArray;
    private float3[] interpolationCurrentRootPositionsArray;
    private float3[] interpolationPreviousRootPositionsArray;
    private float3[] simulationOutputRootOffsetsArray;
    private float3[] interpolationCurrentRootOffsetsArray;
    private float3[] interpolationPreviousRootOffsetsArray;
    private float3[] colliderPositionsArray;
    
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
    
    private List<JiggleTree> pendingAddTrees;
    private List<int> pendingRemoveTrees;
    
    public int treeCount { get; private set; }
    public int transformCount { get; private set; }

    public void RotateBuffers() {
        var tempPoses = interpolationPreviousPoses;
        interpolationPreviousPoses = interpolationCurrentPoses;
        interpolationCurrentPoses = simulationOutputPoses;
        simulationOutputPoses = tempPoses;

        var tempSimulatedRootOffset = interpolationPreviousRootOffsets;
        interpolationPreviousRootOffsets = interpolationCurrentRootOffsets;
        interpolationCurrentRootOffsets = simulationOutputRootOffsets;
        simulationOutputRootOffsets = tempSimulatedRootOffset;

        var tempSimulatedRootPosition = interpolationPreviousRootPositions;
        interpolationPreviousRootPositions = interpolationCurrentRootPositions;
        interpolationCurrentRootPositions = simulationOutputRootPositions;
        simulationOutputRootPositions = tempSimulatedRootPosition;
    }


    private void Resize(int newTransformCapacity, int newTreeCapacity) {
        var newJiggleTreeStructsArray = new JiggleTreeStruct[newTreeCapacity];
        var newSimulateInputPosesArray = new JiggleTransform[newTransformCapacity];
        var newRestPoseTransformsArray = new JiggleTransform[newTransformCapacity];
        var newPreviousLocalRestPoseTransformsArray = new JiggleTransform[newTransformCapacity];
        var newRootOutputPositionsArray = new float3[newTransformCapacity];
        var newSimulationOutputPosesArray = new JiggleTransform[newTransformCapacity];
        var newInterpolationCurrentPosesArray = new JiggleTransform[newTransformCapacity];
        var newInterpolationPreviousPosesArray = new JiggleTransform[newTransformCapacity];
        var newInterpolationOutputPosesArray = new JiggleTransform[newTransformCapacity];
        var newSimulationOutputRootPositionsArray = new float3[newTransformCapacity];
        var newInterpolationCurrentRootPositionsArray = new float3[newTransformCapacity];
        var newInterpolationPreviousRootPositionsArray = new float3[newTransformCapacity];
        var newSimulationOutputRootOffsetsArray = new float3[newTransformCapacity];
        var newInterpolationCurrentRootOffsetsArray = new float3[newTransformCapacity];
        var newInterpolationPreviousRootOffsetsArray = new float3[newTransformCapacity];
        
        if (jiggleTreeStructsArray != null) {
            System.Array.Copy(jiggleTreeStructsArray, newJiggleTreeStructsArray, System.Math.Min(treeCount, newTreeCapacity));
            System.Array.Copy(simulateInputPosesArray, newSimulateInputPosesArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(restPoseTransformsArray, newRestPoseTransformsArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(previousLocalRestPoseTransformsArray, newPreviousLocalRestPoseTransformsArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(rootOutputPositionsArray, newRootOutputPositionsArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(simulationOutputPosesArray, newSimulationOutputPosesArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(interpolationCurrentPosesArray, newInterpolationCurrentPosesArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(interpolationPreviousPosesArray, newInterpolationPreviousPosesArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(interpolationOutputPosesArray, newInterpolationOutputPosesArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(simulationOutputRootPositionsArray, newSimulationOutputRootPositionsArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(interpolationCurrentRootPositionsArray, newInterpolationCurrentRootPositionsArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(interpolationPreviousRootPositionsArray, newInterpolationPreviousRootPositionsArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(simulationOutputRootOffsetsArray, newSimulationOutputRootOffsetsArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(interpolationCurrentRootOffsetsArray, newInterpolationCurrentRootOffsetsArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(interpolationPreviousRootOffsetsArray, newInterpolationPreviousRootOffsetsArray, System.Math.Min(transformCount, newTransformCapacity));
        }
        
        jiggleTreeStructsArray = newJiggleTreeStructsArray;
        simulateInputPosesArray = newSimulateInputPosesArray;
        restPoseTransformsArray = newRestPoseTransformsArray;
        previousLocalRestPoseTransformsArray = newPreviousLocalRestPoseTransformsArray;
        rootOutputPositionsArray = newRootOutputPositionsArray;
        simulationOutputPosesArray = newSimulationOutputPosesArray;
        interpolationCurrentPosesArray = newInterpolationCurrentPosesArray;
        interpolationPreviousPosesArray = newInterpolationPreviousPosesArray;
        interpolationOutputPosesArray = newInterpolationOutputPosesArray;
        simulationOutputRootPositionsArray = newSimulationOutputRootPositionsArray;
        interpolationCurrentRootPositionsArray = newInterpolationCurrentRootPositionsArray;
        interpolationPreviousRootPositionsArray = newInterpolationPreviousRootPositionsArray;
        simulationOutputRootOffsetsArray = newSimulationOutputRootOffsetsArray;
        interpolationCurrentRootOffsetsArray = newInterpolationCurrentRootOffsetsArray;
        interpolationPreviousRootOffsetsArray = newInterpolationPreviousRootOffsetsArray;
        
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
        jiggleTreeStructs = new NativeArray<JiggleTreeStruct>(newTransformCapacity, Allocator.Persistent);
        simulateInputPoses = new NativeArray<JiggleTransform>(newTransformCapacity, Allocator.Persistent);
        restPoseTransforms = new NativeArray<JiggleTransform>(newTransformCapacity, Allocator.Persistent);
        previousLocalRestPoseTransforms = new NativeArray<JiggleTransform>(newTransformCapacity, Allocator.Persistent);
        rootOutputPositions = new NativeArray<float3>(newTransformCapacity, Allocator.Persistent);
        simulationOutputPoses = new NativeArray<JiggleTransform>(newTransformCapacity, Allocator.Persistent);
        interpolationCurrentPoses = new NativeArray<JiggleTransform>(newTransformCapacity, Allocator.Persistent);
        interpolationPreviousPoses = new NativeArray<JiggleTransform>(newTransformCapacity, Allocator.Persistent);
        interpolationOutputPoses = new NativeArray<JiggleTransform>(newTransformCapacity, Allocator.Persistent);
        simulationOutputRootPositions = new NativeArray<float3>(newTransformCapacity, Allocator.Persistent);
        interpolationCurrentRootPositions = new NativeArray<float3>(newTransformCapacity, Allocator.Persistent);
        interpolationPreviousRootPositions = new NativeArray<float3>(newTransformCapacity, Allocator.Persistent);
        simulationOutputRootOffsets = new NativeArray<float3>(newTransformCapacity, Allocator.Persistent);
        interpolationCurrentRootOffsets = new NativeArray<float3>(newTransformCapacity, Allocator.Persistent);
        interpolationPreviousRootOffsets = new NativeArray<float3>(newTransformCapacity, Allocator.Persistent);
        
        transformCapacity = newTransformCapacity;
        treeCapacity = newTreeCapacity;
    }
    
    public JiggleMemoryBus() {
        pendingAddTrees = new();
        pendingRemoveTrees = new();

        Resize(2048, 256);
        
        WriteOut();
        
        transformAccessList = new List<Transform>();
        transformRootAccessList = new List<Transform>();
        RegnerateAccessArrays();
        
        colliderTransformAccessArray = new TransformAccessArray(new Transform[] {});
        colliderPositions = new NativeArray<float3>(new float3[]{}, Allocator.Persistent);
        treeCount = 0;
        transformCount = 0;
    }

    private void ReadIn<T>(NativeArray<T> native, T[] array, int count) where T : struct {
        NativeArray<T>.Copy(native, array, count);
    }

    private void ReadIn() {
        if (!jiggleTreeStructs.IsCreated) {
            return;
        }
        
        ReadIn(jiggleTreeStructs, jiggleTreeStructsArray, treeCount);
        
        ReadIn(simulateInputPoses, simulateInputPosesArray, transformCount);
        ReadIn(restPoseTransforms, restPoseTransformsArray, transformCount);
        ReadIn(previousLocalRestPoseTransforms, previousLocalRestPoseTransformsArray, transformCount);
        ReadIn(rootOutputPositions, rootOutputPositionsArray, transformCount);
        ReadIn(simulationOutputPoses, simulationOutputPosesArray, transformCount);
        ReadIn(interpolationCurrentPoses, interpolationCurrentPosesArray, transformCount);
        ReadIn(interpolationPreviousPoses, interpolationPreviousPosesArray, transformCount);
        ReadIn(interpolationOutputPoses, interpolationOutputPosesArray, transformCount);
        ReadIn(simulationOutputRootPositions, simulationOutputRootPositionsArray, transformCount);
        ReadIn(interpolationCurrentRootPositions, interpolationCurrentRootPositionsArray, transformCount);
        ReadIn(interpolationPreviousRootPositions, interpolationPreviousRootPositionsArray, transformCount);
        ReadIn(simulationOutputRootOffsets, simulationOutputRootOffsetsArray, transformCount);
        ReadIn(interpolationCurrentRootOffsets, interpolationCurrentRootOffsetsArray, transformCount);
        ReadIn(interpolationPreviousRootOffsets, interpolationPreviousRootOffsetsArray, transformCount);
    }

    private void WriteOut() {
        NativeArray<JiggleTreeStruct>.Copy(jiggleTreeStructsArray, jiggleTreeStructs, treeCount);
        NativeArray<JiggleTransform>.Copy(simulateInputPosesArray, simulateInputPoses, transformCount);
        NativeArray<JiggleTransform>.Copy(restPoseTransformsArray, restPoseTransforms, transformCount);
        NativeArray<JiggleTransform>.Copy(previousLocalRestPoseTransformsArray, previousLocalRestPoseTransforms, transformCount);
        NativeArray<float3>.Copy(rootOutputPositionsArray, rootOutputPositions, transformCount);
        NativeArray<JiggleTransform>.Copy(simulationOutputPosesArray, simulationOutputPoses, transformCount);
        NativeArray<JiggleTransform>.Copy(interpolationCurrentPosesArray, interpolationCurrentPoses, transformCount);
        NativeArray<JiggleTransform>.Copy(interpolationPreviousPosesArray, interpolationPreviousPoses, transformCount);
        NativeArray<JiggleTransform>.Copy(interpolationOutputPosesArray, interpolationOutputPoses, transformCount);
        NativeArray<float3>.Copy(simulationOutputRootPositionsArray, simulationOutputRootPositions, transformCount);
        NativeArray<float3>.Copy(interpolationCurrentRootPositionsArray, interpolationCurrentRootPositions, transformCount);
        NativeArray<float3>.Copy(interpolationPreviousRootPositionsArray, interpolationPreviousRootPositions, transformCount);
        NativeArray<float3>.Copy(simulationOutputRootOffsetsArray, simulationOutputRootOffsets, transformCount);
        NativeArray<float3>.Copy(interpolationCurrentRootOffsetsArray, interpolationCurrentRootOffsets, transformCount);
        NativeArray<float3>.Copy(interpolationPreviousRootOffsetsArray, interpolationPreviousRootOffsets, transformCount);
    }

    private void RemoveTreeAt(int index) {
        var removedTree = jiggleTreeStructsArray[index];
        for (int i = index; i < treeCount; i++) {
            var shift = jiggleTreeStructsArray[i + 1];
            shift.transformIndexOffset -= removedTree.pointCount;
            jiggleTreeStructsArray[i] = shift;
        }
        treeCount--;
        RemoveRange(simulateInputPosesArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        RemoveRange(restPoseTransformsArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        RemoveRange(previousLocalRestPoseTransformsArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        RemoveRange(rootOutputPositionsArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        RemoveRange(simulationOutputPosesArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        RemoveRange(interpolationCurrentPosesArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        RemoveRange(interpolationPreviousPosesArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        RemoveRange(interpolationOutputPosesArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        RemoveRange(simulationOutputRootPositionsArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        RemoveRange(interpolationCurrentRootPositionsArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        RemoveRange(interpolationPreviousRootPositionsArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        RemoveRange(simulationOutputRootOffsetsArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        RemoveRange(interpolationCurrentRootOffsetsArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        RemoveRange(interpolationPreviousRootOffsetsArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        
        transformAccessList.RemoveRange((int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        transformRootAccessList.RemoveRange((int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
        transformCount -= (int)removedTree.pointCount;
    }
    
    private static void RemoveRange<T>(T[] array, int index, int count) {
        if (index < 0 || index >= array.Length || count < 0 || index + count > array.Length) {
            throw new System.ArgumentOutOfRangeException();
        }

        for (int i = 0; i < array.Length - (index+count); i++) {
            array[index+i] = array[index + i + count];
        }
        //System.Array.Copy(array, index + count, array, index, array.Length - (index + count));
    }

    public void Commit() {
        var pendingRemoveCount = pendingRemoveTrees.Count;
        var pendingAddCount = pendingAddTrees.Count;
        if (pendingRemoveCount == 0 && pendingAddCount == 0) {
            return;
        }
        ReadIn();
        
        #region Removing
        for (int i = 0; i < pendingRemoveCount; i++) {
            var currentRemoveID = pendingRemoveTrees[i];
            for (var index = 0; index < treeCount; index++) {
                if (jiggleTreeStructsArray[index].rootID == currentRemoveID) {
                    RemoveTreeAt(index);
                    break;
                }
            }
        }
        pendingRemoveTrees.Clear();
        #endregion

        #region Adding
        for (int i = 0; i < pendingAddCount; i++) {
            var jiggleTree = pendingAddTrees[i];
            if (treeCount+1 > treeCapacity || transformCount + jiggleTree.points.Length > transformCapacity) {
                Resize(transformCapacity*2, treeCapacity*2);
            }
            
            var jiggleTreeStruct = jiggleTree.GetStruct();
            jiggleTreeStruct.transformIndexOffset = (uint)transformCount;

            jiggleTreeStructsArray[treeCount] = jiggleTreeStruct;
            float3 rootPos = jiggleTree.bones[0].position;
            for (int o = 0; o < jiggleTreeStruct.pointCount; o++) {
                unsafe {
                    var point = jiggleTreeStruct.points[o];
                    jiggleTree.bones[o].GetPositionAndRotation(out var pos, out var rot);
                    jiggleTree.bones[o].GetLocalPositionAndRotation(out var lpos, out var lrot);
                    var pose = new JiggleTransform() {
                        isVirtual = !point.hasTransform,
                        position = pos,
                        rotation = rot,
                    };
                    var localPose = new JiggleTransform() {
                        isVirtual = !point.hasTransform,
                        position = lpos,
                        rotation = lrot,
                    };
                    simulateInputPosesArray[transformCount + o] = pose;
                    restPoseTransformsArray[transformCount + o] = localPose;
                    previousLocalRestPoseTransformsArray[transformCount + o] = localPose;
                    rootOutputPositionsArray[transformCount + o] = rootPos;
                    simulationOutputPosesArray[transformCount + o] = pose;
                    interpolationCurrentPosesArray[transformCount + o] = pose;
                    interpolationPreviousPosesArray[transformCount + o] = pose;
                    interpolationOutputPosesArray[transformCount + o] = pose;
                    simulationOutputRootPositionsArray[transformCount + o] = rootPos;
                    interpolationCurrentRootPositionsArray[transformCount + o] = rootPos;
                    interpolationPreviousRootPositionsArray[transformCount + o] = rootPos;
                    simulationOutputRootOffsetsArray[transformCount + o] = new float3(0f);
                    interpolationCurrentRootOffsetsArray[transformCount + o] = new float3(0f);
                    interpolationPreviousRootOffsetsArray[transformCount + o] = new float3(0f);
                }
            }
            transformAccessList.AddRange(jiggleTree.bones);
            for (var index = 0; index < jiggleTree.points.Length; index++) {
                transformRootAccessList.Add(jiggleTree.bones[0]);
            }
            treeCount++;
            transformCount += (int)jiggleTreeStruct.pointCount;
        }
        pendingAddTrees.Clear();

        RegnerateAccessArrays();
        WriteOut();
        #endregion
    }
    
    public void Add(JiggleTree jiggleTree) {
        pendingAddTrees.Add(jiggleTree);
    }

    public void Remove(int rootBoneInstanceID) {
        pendingRemoveTrees.Add(rootBoneInstanceID);
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
