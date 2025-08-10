using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

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
    
    public TransformAccessArray oldTransformAccessArray;
    public TransformAccessArray oldTransformRootAccessArray;
    
    public TransformAccessArray transformAccessArray;
    public TransformAccessArray transformRootAccessArray;
    
    public TransformAccessArray newTransformAccessArray;
    public TransformAccessArray newTransformRootAccessArray;
    
    public TransformAccessArray colliderTransformAccessArray;
    
    private List<JiggleTree> pendingAddTrees;
    private List<int> pendingRemoveTrees;
    
    private List<JiggleTree> pendingProcessingAdds;
    private List<int> pendingProcessingRemoves;
    
    public int transformCount;
    public int treeCount;
    
    private int currentTransformAccessIndex = 0;

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
        jiggleTreeStructs = new NativeArray<JiggleTreeStruct>(jiggleTreeStructsArray, Allocator.Persistent);
        simulateInputPoses = new NativeArray<JiggleTransform>(simulateInputPosesArray, Allocator.Persistent);
        restPoseTransforms = new NativeArray<JiggleTransform>(restPoseTransformsArray, Allocator.Persistent);
        previousLocalRestPoseTransforms = new NativeArray<JiggleTransform>(previousLocalRestPoseTransformsArray, Allocator.Persistent);
        rootOutputPositions = new NativeArray<float3>(rootOutputPositionsArray, Allocator.Persistent);
        simulationOutputPoses = new NativeArray<JiggleTransform>(simulationOutputPosesArray, Allocator.Persistent);
        interpolationCurrentPoses = new NativeArray<JiggleTransform>(interpolationCurrentPosesArray, Allocator.Persistent);
        interpolationPreviousPoses = new NativeArray<JiggleTransform>(interpolationPreviousPosesArray, Allocator.Persistent);
        interpolationOutputPoses = new NativeArray<JiggleTransform>(interpolationOutputPosesArray, Allocator.Persistent);
        simulationOutputRootPositions = new NativeArray<float3>(simulationOutputRootPositionsArray, Allocator.Persistent);
        interpolationCurrentRootPositions = new NativeArray<float3>(interpolationCurrentRootPositionsArray, Allocator.Persistent);
        interpolationPreviousRootPositions = new NativeArray<float3>(interpolationPreviousRootPositionsArray, Allocator.Persistent);
        simulationOutputRootOffsets = new NativeArray<float3>(simulationOutputRootOffsetsArray, Allocator.Persistent);
        interpolationCurrentRootOffsets = new NativeArray<float3>(interpolationCurrentRootOffsetsArray, Allocator.Persistent);
        interpolationPreviousRootOffsets = new NativeArray<float3>(interpolationPreviousRootOffsetsArray, Allocator.Persistent);
        
        transformCapacity = newTransformCapacity;
        treeCapacity = newTreeCapacity;
    }
    
    public JiggleMemoryBus() {
        pendingAddTrees = new();
        pendingRemoveTrees = new();
        pendingProcessingRemoves = new();
        pendingProcessingAdds = new();

        Resize(50000, 1000);
        
        WriteOut();
        ClearTransformAccessArray();
        ClearRootTransformAccessArray();
        
        transformAccessList = new List<Transform>();
        transformRootAccessList = new List<Transform>();
        transformAccessArray = new TransformAccessArray(new Transform[] {});
        transformRootAccessArray = new TransformAccessArray(new Transform[] {});
        
        colliderTransformAccessArray = new TransformAccessArray(new Transform[] {});
        colliderPositions = new NativeArray<float3>(new float3[]{}, Allocator.Persistent);
        transformCount = 0;
        treeCount = 0;
    }

    private void ReadIn<T>(NativeArray<T> native, T[] array, int count) where T : struct {
        NativeArray<T>.Copy(native, array, count);
    }

    private void ReadIn() {
        Profiler.BeginSample("JiggleMemoryBus.ReadIn");
        if (!jiggleTreeStructs.IsCreated) {
            Profiler.EndSample();
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
        Profiler.EndSample();
    }

    private void WriteOut() {
        Profiler.BeginSample("JiggleMemoryBus.WriteOut");
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
        Profiler.EndSample();
    }

    private void ClearTransformAccessArray() {
        Profiler.BeginSample("JiggleMemoryBus.TransferTransformAccessArray");
        oldTransformAccessArray = transformAccessArray;
        transformAccessArray = newTransformAccessArray;
        Profiler.EndSample();
    }
    
    private void DisposeTransformAccessArray() {
        if (oldTransformAccessArray.isCreated) {
            oldTransformAccessArray.Dispose();
        }
    }

    private void ClearRootTransformAccessArray() {
        Profiler.BeginSample("JiggleMemoryBus.TransferRootTransformAccessArray");
        oldTransformRootAccessArray = transformRootAccessArray;
        transformRootAccessArray = newTransformRootAccessArray;
        Profiler.EndSample();
    }

    private void DisposeRootTransformAccessArray() {
        if (oldTransformRootAccessArray.isCreated) {
            oldTransformRootAccessArray.Dispose();
        }
    }

    private void RemoveTransformsForTree(int id) {
        for (int i = 0; i < treeCount; i++) {
            var removedTree = jiggleTreeStructsArray[i];
            if (removedTree.rootID != id) continue;
            for (int j = i; j < treeCount; j++) {
                var shift = jiggleTreeStructsArray[j + 1];
                shift.transformIndexOffset -= removedTree.pointCount;
                jiggleTreeStructsArray[j] = shift;
            }
            transformAccessList.RemoveRange((int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
            transformRootAccessList.RemoveRange((int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
            break;
        }
    }

    private void RemoveTree(int id) {
        Profiler.BeginSample("JiggleMemoryBus.RemoveTree");
        for (int i = 0; i < treeCount; i++) {
            var removedTree = jiggleTreeStructsArray[i];
            if (removedTree.rootID != id) continue;
            Profiler.BeginSample("JiggleMemoryBus.RemoveTree.ArrayManipulation");
            for (int j = i; j < treeCount; j++) {
                var shift = jiggleTreeStructsArray[j + 1];
                shift.transformIndexOffset -= removedTree.pointCount;
                jiggleTreeStructsArray[j] = shift;
            }
            treeCount--;
            Profiler.EndSample();
            Profiler.BeginSample("JiggleMemoryBus.RemoveTree.RemoveRanges");
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
            transformCount -= (int)removedTree.pointCount;
            Profiler.EndSample();
            break;
        }
        Profiler.EndSample();
    }
    
    private static void RemoveRange<T>(T[] array, int index, int count) {
        if (index < 0 || index >= array.Length || count < 0 || index + count > array.Length) {
            throw new System.ArgumentOutOfRangeException();
        }
        Profiler.BeginSample("JiggleMemoryBus.RemoveRange.Copy");
        System.Array.Copy(array, index + count, array, index, array.Length - (index + count));
        Profiler.EndSample();
    }

    private enum CommitState {
        Idle,
        Processing,
        RecreatingAccessArrays,
        RecreatingRootAccessArrays
    }
    
    private CommitState commitState = CommitState.Idle;

    public void Commit() {
        if (commitState == CommitState.Idle) {
            var pendingRemoveCount = pendingRemoveTrees.Count;
            var pendingAddCount = pendingAddTrees.Count;
            
            if (pendingRemoveCount == 0 && pendingAddCount == 0) {
                return;
            }

            for (int i = 0; i < pendingAddCount; i++) {
                var jiggleTree = pendingAddTrees[i];
                //if (transformAccessList.Count + jiggleTree.points.Length > transformCapacity) {
                //Resize(transformCapacity * 2, treeCapacity * 2);
                //}

                transformAccessList.AddRange(jiggleTree.bones);
                for (var index = 0; index < jiggleTree.points.Length; index++) {
                    transformRootAccessList.Add(jiggleTree.bones[0]);
                }
            }

            pendingProcessingAdds.AddRange(pendingAddTrees);
            pendingAddTrees.Clear();

            for (int i = 0; i < pendingRemoveCount; i++) {
                var currentRemoveID = pendingRemoveTrees[i];
                RemoveTransformsForTree(currentRemoveID);
            }

            pendingProcessingRemoves.AddRange(pendingRemoveTrees);
            pendingRemoveTrees.Clear();
            
            currentTransformAccessIndex = 0;
            commitState = CommitState.Processing;
        } else if (commitState == CommitState.Processing) {
            GenerateNewAccessArrays(ref currentTransformAccessIndex, out var hasFinished);
            if (!hasFinished) return;
            ReadIn();
            var processingPendingRemoveCount = pendingProcessingRemoves.Count;
            var processingPendingAddCount = pendingProcessingAdds.Count;
            
            #region Adding
            Profiler.BeginSample("JiggleMemoryBus.Commit.Add");
            for (int i = 0; i < processingPendingAddCount; i++) {
                var jiggleTree = pendingProcessingAdds[i];
                
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
                treeCount++;
                transformCount += (int)jiggleTreeStruct.pointCount;
            }
            pendingProcessingAdds.Clear();
            Profiler.EndSample();
            #endregion
            
            #region Removing
            Profiler.BeginSample("JiggleMemoryBus.Commit.Remove");
            for (int i = 0; i < processingPendingRemoveCount; i++) {
                var currentRemoveID = pendingProcessingRemoves[i];
                RemoveTree(currentRemoveID);
            }
            pendingProcessingRemoves.Clear();
            Profiler.EndSample();
            #endregion

            
            WriteOut();
            ClearTransformAccessArray();
            ClearRootTransformAccessArray();
            commitState = CommitState.RecreatingAccessArrays;
        } else if (commitState == CommitState.RecreatingAccessArrays) {
            DisposeTransformAccessArray();
            commitState = CommitState.RecreatingRootAccessArrays;
        } else if (commitState == CommitState.RecreatingRootAccessArrays) {
            DisposeRootTransformAccessArray();
            commitState = CommitState.Idle;
        }
    }
    
    public void Add(JiggleTree jiggleTree) {
        pendingAddTrees.Add(jiggleTree);
    }

    public void Remove(int rootBoneInstanceID) {
        pendingRemoveTrees.Add(rootBoneInstanceID);
    }

    void GenerateNewAccessArrays(ref int currentIndex, out bool hasFinished) {
        Profiler.BeginSample("RegenerateTransformAccessArrays");
        var count = transformAccessList.Count;
        if (currentIndex == 0) {
            newTransformAccessArray = new TransformAccessArray(count);
            newTransformRootAccessArray = new TransformAccessArray(count);
        }
        int addedSoFar = 0;
        for (var index = currentIndex; index < count && addedSoFar < 512; index++) {
            newTransformAccessArray.Add(transformAccessList[index]);
            newTransformRootAccessArray.Add(transformRootAccessList[index]);
            addedSoFar++;
        }
        currentIndex += addedSoFar;
        
        if (currentIndex == count) {
            hasFinished = true;
            return;
        }

        hasFinished = false;
        Profiler.EndSample();
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
