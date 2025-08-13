using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

public struct PoseData {
    public JiggleTransform pose;
    public float3 rootPosition;
    public float3 rootOffset;

    public static PoseData Lerp(PoseData a, PoseData b, float t) {
        return new PoseData() {
            pose = JiggleTransform.Lerp(a.pose, b.pose, t),
            rootPosition = math.lerp(a.rootPosition, b.rootPosition, t),
            rootOffset = math.lerp(a.rootOffset, b.rootOffset, t),
        };
    }
}

public struct Fragment {
    public int startIndex;
    public int count;
}

public class JiggleMemoryBus {// : IContainer<JiggleTreeStruct> {
    private int treeCapacity = 0;
    private int transformCapacity = 0;
    private JiggleTreeStruct[] jiggleTreeStructsArray;
    private JiggleTransform[] simulateInputPosesArray;
    private JiggleTransform[] restPoseTransformsArray;
    private JiggleTransform[] previousLocalRestPoseTransformsArray;
    private float3[] rootOutputPositionsArray;
    private JiggleTransform[] interpolationOutputPosesArray;
    private PoseData[] simulationOutputPoseDataArray;
    private PoseData[] interpolationCurrentPoseDataArray;
    private PoseData[] interpolationPreviousPoseDataArray;
    private float3[] colliderPositionsArray;
    
    public NativeArray<JiggleTreeStruct> jiggleTreeStructs;
    public NativeArray<JiggleTransform> simulateInputPoses;
    public NativeArray<JiggleTransform> restPoseTransforms;
    public NativeArray<JiggleTransform> previousLocalRestPoseTransforms;
    public NativeArray<float3> rootOutputPositions;
    public NativeArray<JiggleTransform> interpolationOutputPoses;
    public NativeArray<PoseData> simulationOutputPoseData;
    public NativeArray<PoseData> interpolationCurrentPoseData;
    public NativeArray<PoseData> interpolationPreviousPoseData;
    public NativeArray<float3> colliderPositions;
    
    private List<Transform> transformAccessList;
    private List<Transform> transformRootAccessList;
    private List<Transform> colliderTransformAccessList;
    
    public TransformAccessArray transformAccessArray;
    public TransformAccessArray transformRootAccessArray;
    
    public TransformAccessArray newTransformAccessArray;
    public TransformAccessArray newTransformRootAccessArray;
    
    public TransformAccessArray colliderTransformAccessArray;
    
    private List<JiggleTree> pendingAddTrees;
    private List<int> pendingRemoveTrees;
    
    private List<JiggleTree> pendingProcessingAdds;
    private List<int> pendingProcessingRemoves;
    
    private List<Fragment> memoryFragments;
    private List<Fragment> preMemoryFragments;
    
    private int preTransformCount;
    
    public int transformCount;
    public int treeCount;
    
    private int currentTransformAccessIndex = 0;

    public void RotateBuffers() {
        var tempPoses = interpolationPreviousPoseData;
        interpolationPreviousPoseData = interpolationCurrentPoseData;
        interpolationCurrentPoseData = simulationOutputPoseData;
        simulationOutputPoseData = tempPoses;
    }


    private void Resize(int newTransformCapacity, int newTreeCapacity) {
        Debug.Log($"Resized to {newTransformCapacity} transforms and {newTreeCapacity} trees.");
        var newJiggleTreeStructsArray = new JiggleTreeStruct[newTreeCapacity];
        var newSimulateInputPosesArray = new JiggleTransform[newTransformCapacity];
        var newRestPoseTransformsArray = new JiggleTransform[newTransformCapacity];
        var newPreviousLocalRestPoseTransformsArray = new JiggleTransform[newTransformCapacity];
        var newRootOutputPositionsArray = new float3[newTransformCapacity];
        var newInterpolationOutputPosesArray = new JiggleTransform[newTransformCapacity];
        var newSimulationOutputPoseDataArray = new PoseData[newTransformCapacity];
        var newInterpolationCurrentPoseDataArray = new PoseData[newTransformCapacity];
        var newInterpolationPreviousPoseDataArray = new PoseData[newTransformCapacity];
        
        if (jiggleTreeStructsArray != null) {
            System.Array.Copy(jiggleTreeStructsArray, newJiggleTreeStructsArray, System.Math.Min(treeCount, newTreeCapacity));
            System.Array.Copy(simulateInputPosesArray, newSimulateInputPosesArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(restPoseTransformsArray, newRestPoseTransformsArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(previousLocalRestPoseTransformsArray, newPreviousLocalRestPoseTransformsArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(rootOutputPositionsArray, newRootOutputPositionsArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(interpolationOutputPosesArray, newInterpolationOutputPosesArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(simulationOutputPoseDataArray, newSimulationOutputPoseDataArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(interpolationCurrentPoseDataArray, newInterpolationCurrentPoseDataArray, System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(interpolationPreviousPoseDataArray, newInterpolationPreviousPoseDataArray, System.Math.Min(transformCount, newTransformCapacity));
        }
        
        jiggleTreeStructsArray = newJiggleTreeStructsArray;
        simulateInputPosesArray = newSimulateInputPosesArray;
        restPoseTransformsArray = newRestPoseTransformsArray;
        previousLocalRestPoseTransformsArray = newPreviousLocalRestPoseTransformsArray;
        rootOutputPositionsArray = newRootOutputPositionsArray;
        interpolationOutputPosesArray = newInterpolationOutputPosesArray;
        simulationOutputPoseDataArray = newSimulationOutputPoseDataArray;
        interpolationCurrentPoseDataArray = newInterpolationCurrentPoseDataArray;
        interpolationPreviousPoseDataArray = newInterpolationPreviousPoseDataArray;
        
        if (jiggleTreeStructs.IsCreated) {
            jiggleTreeStructs.Dispose();
            simulateInputPoses.Dispose();
            restPoseTransforms.Dispose();
            previousLocalRestPoseTransforms.Dispose();
            rootOutputPositions.Dispose();
            interpolationOutputPoses.Dispose();
            simulationOutputPoseData.Dispose();
            interpolationCurrentPoseData.Dispose();
            interpolationPreviousPoseData.Dispose();
        }
        jiggleTreeStructs = new NativeArray<JiggleTreeStruct>(jiggleTreeStructsArray, Allocator.Persistent);
        simulateInputPoses = new NativeArray<JiggleTransform>(simulateInputPosesArray, Allocator.Persistent);
        restPoseTransforms = new NativeArray<JiggleTransform>(restPoseTransformsArray, Allocator.Persistent);
        previousLocalRestPoseTransforms = new NativeArray<JiggleTransform>(previousLocalRestPoseTransformsArray, Allocator.Persistent);
        rootOutputPositions = new NativeArray<float3>(rootOutputPositionsArray, Allocator.Persistent);
        interpolationOutputPoses = new NativeArray<JiggleTransform>(interpolationOutputPosesArray, Allocator.Persistent);
        simulationOutputPoseData = new NativeArray<PoseData>(simulationOutputPoseDataArray, Allocator.Persistent);
        interpolationCurrentPoseData = new NativeArray<PoseData>(interpolationCurrentPoseDataArray, Allocator.Persistent);
        interpolationPreviousPoseData = new NativeArray<PoseData>(interpolationPreviousPoseDataArray, Allocator.Persistent);
        
        transformCapacity = newTransformCapacity;
        treeCapacity = newTreeCapacity;
    }
    
    public JiggleMemoryBus() {
        pendingAddTrees = new();
        pendingRemoveTrees = new();
        pendingProcessingRemoves = new();
        pendingProcessingAdds = new();

        Resize(4096, 512);
        
        WriteOut();
        
        memoryFragments = new List<Fragment>();
        preMemoryFragments = new List<Fragment>();
        transformAccessList = new List<Transform>();
        transformRootAccessList = new List<Transform>();
        transformAccessArray = new TransformAccessArray(100);
        transformRootAccessArray = new TransformAccessArray(100);
        newTransformAccessArray = new TransformAccessArray(100);
        newTransformRootAccessArray = new TransformAccessArray(100);
        
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
        ReadIn(interpolationOutputPoses, interpolationOutputPosesArray, transformCount);
        ReadIn(simulationOutputPoseData, simulationOutputPoseDataArray, transformCount);
        ReadIn(interpolationCurrentPoseData, interpolationCurrentPoseDataArray, transformCount);
        ReadIn(interpolationPreviousPoseData, interpolationPreviousPoseDataArray, transformCount);
        Profiler.EndSample();
    }

    private void WriteOut() {
        Profiler.BeginSample("JiggleMemoryBus.WriteOut");
        NativeArray<JiggleTreeStruct>.Copy(jiggleTreeStructsArray, jiggleTreeStructs, treeCount);
        NativeArray<JiggleTransform>.Copy(simulateInputPosesArray, simulateInputPoses, transformCount);
        NativeArray<JiggleTransform>.Copy(restPoseTransformsArray, restPoseTransforms, transformCount);
        NativeArray<JiggleTransform>.Copy(previousLocalRestPoseTransformsArray, previousLocalRestPoseTransforms, transformCount);
        NativeArray<float3>.Copy(rootOutputPositionsArray, rootOutputPositions, transformCount);
        NativeArray<JiggleTransform>.Copy(interpolationOutputPosesArray, interpolationOutputPoses, transformCount);
        NativeArray<PoseData>.Copy(simulationOutputPoseDataArray, simulationOutputPoseData, transformCount);
        NativeArray<PoseData>.Copy(interpolationCurrentPoseDataArray, interpolationCurrentPoseData, transformCount);
        NativeArray<PoseData>.Copy(interpolationPreviousPoseDataArray, interpolationPreviousPoseData, transformCount);
        Profiler.EndSample();
    }

    private void FlipTransformAccessArrays() {
        Profiler.BeginSample("JiggleMemoryBus.TransferTransformAccessArray");
        (transformAccessArray, newTransformAccessArray) = (newTransformAccessArray, transformAccessArray);
        (transformRootAccessArray, newTransformRootAccessArray) = (newTransformRootAccessArray, transformRootAccessArray);
        Profiler.EndSample();
    }
    
    private void ClearTransformAccessArrays(out bool isDone) {
        var length = newTransformAccessArray.length;
        int removedSoFar = 0;
        for (int i = 0; i < length && removedSoFar < 512; i++) {
            newTransformAccessArray.RemoveAtSwapBack(0);
            newTransformRootAccessArray.RemoveAtSwapBack(0);
            removedSoFar++;
        }
        isDone = removedSoFar == length;
    }

    private void PreRemoveTree(int id) {
        for (int i = 0; i < treeCount; i++) {
            var removedTree = jiggleTreeStructsArray[i];
            if (removedTree.rootID != id) continue;
            preMemoryFragments.Add(new Fragment() {
                startIndex = (int)removedTree.transformIndexOffset,
                count = (int)removedTree.pointCount
            });
            break;
        }
    }

    private void RemoveTree(int id) {
        Profiler.BeginSample("JiggleMemoryBus.RemoveTree");
        for (int i = 0; i < treeCount; i++) {
            var removedTree = jiggleTreeStructsArray[i];
            if (removedTree.rootID != id) continue;
            Profiler.BeginSample("JiggleMemoryBus.RemoveTree.ArrayManipulation");
            int shiftCount = treeCount - i - 1;
            if (shiftCount > 0) {
                System.Array.Copy(jiggleTreeStructsArray, i + 1, jiggleTreeStructsArray, i, shiftCount);
            }
            treeCount--;
            Profiler.EndSample();
            Profiler.BeginSample("JiggleMemoryBus.RemoveTree.MarkVirtual");
            for (int j = (int)removedTree.transformIndexOffset;j < removedTree.transformIndexOffset + removedTree.pointCount; j++) {
                var pose = simulationOutputPoseDataArray[j].pose;
                pose.isVirtual = true;
                simulationOutputPoseDataArray[j].pose = pose;
                
                var interpPose = interpolationCurrentPoseDataArray[j].pose;
                interpPose.isVirtual = true;
                interpolationCurrentPoseDataArray[j].pose = interpPose;
                
                var interpPose2 = interpolationPreviousPoseDataArray[j].pose;
                interpPose2.isVirtual = true;
                interpolationPreviousPoseDataArray[j].pose = interpPose2;
            }
            memoryFragments.Add(new Fragment() {
                startIndex = (int)removedTree.transformIndexOffset,
                count = (int)removedTree.pointCount
            });
            Profiler.EndSample();
            break;
        }
        Profiler.EndSample();
    }

    private enum CommitState {
        Idle,
        ProcessingTransformAccess,
        RecreatingAccessArrays,
    }

    
    private CommitState commitState = CommitState.Idle;

    private void AddTransformsToSlice(int index, JiggleTree jiggleTree, JiggleTreeStruct jiggleTreeStruct) {
        if (treeCount + 1 > treeCapacity) {
            Resize(transformCapacity, treeCapacity * 2);
        }

        if (index + jiggleTreeStruct.pointCount >= transformCapacity) {
            Resize(transformCapacity * 2, treeCapacity);
        }

        var rootBone = jiggleTree.bones[0];
        
        var desiredCount = index + (int)jiggleTreeStruct.pointCount;
        while (transformAccessList.Count < desiredCount) {
            transformAccessList.Add(rootBone);
            transformRootAccessList.Add(rootBone);
        }

        for (int o = 0; o < jiggleTreeStruct.pointCount; o++) {
            transformAccessList[index + o] = jiggleTree.bones[o];
            transformRootAccessList[index + o] = rootBone;
        }
        preTransformCount = math.max(index + (int)jiggleTreeStruct.pointCount, preTransformCount);
    }

    private void AddTreeToSlice(int index, JiggleTree jiggleTree, JiggleTreeStruct jiggleTreeStruct) {
        jiggleTreeStruct.transformIndexOffset = (uint)index;

        if (treeCount + 1 > treeCapacity) {
            Resize(transformCapacity, treeCapacity * 2);
        }

        if (index + jiggleTreeStruct.pointCount >= transformCapacity) {
            Resize(transformCapacity * 2, treeCapacity);
        }
        
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
                simulateInputPosesArray[index + o] = pose;
                restPoseTransformsArray[index + o] = localPose;
                previousLocalRestPoseTransformsArray[index + o] = localPose;
                rootOutputPositionsArray[index + o] = rootPos;
                interpolationOutputPosesArray[index + o] = pose;
                var poseData = new PoseData() {
                    pose = pose,
                    rootOffset = new float3(0f),
                    rootPosition = rootPos,
                };
                simulationOutputPoseDataArray[index + o] = poseData;
                interpolationCurrentPoseDataArray[index + o] = poseData;
                interpolationPreviousPoseDataArray[index + o] = poseData;
            }
        }
        treeCount++;
        transformCount = math.max((int)(index + jiggleTreeStruct.pointCount), transformCount);
    }

    public void Commit() {
        if (commitState == CommitState.Idle) {
            var pendingRemoveCount = pendingRemoveTrees.Count;
            var pendingAddCount = pendingAddTrees.Count;

            if (pendingRemoveCount == 0 && pendingAddCount == 0) {
                return;
            }

            preMemoryFragments.Clear();
            preMemoryFragments.AddRange(memoryFragments);
            preTransformCount = transformCount;

            for (int i = 0; i < pendingRemoveCount; i++) {
                var currentRemoveID = pendingRemoveTrees[i];
                PreRemoveTree(currentRemoveID);
            }

            pendingProcessingRemoves.AddRange(pendingRemoveTrees);
            pendingRemoveTrees.Clear();

            for (int i = 0; i < pendingAddCount; i++) {
                var jiggleTree = pendingAddTrees[i];
                var jiggleTreeStruct = pendingAddTrees[i].GetStruct();

                var memoryFragmentCount = preMemoryFragments.Count;
                bool foundMemoryFragment = false;
                for (int j = 0; j < memoryFragmentCount; j++) {
                    var fragment = preMemoryFragments[j];
                    if (fragment.count >= jiggleTreeStruct.pointCount) {
                        foundMemoryFragment = true;
                        AddTransformsToSlice(fragment.startIndex, jiggleTree, jiggleTreeStruct);
                        fragment.startIndex += (int)jiggleTreeStruct.pointCount;
                        fragment.count -= (int)jiggleTreeStruct.pointCount;
                        if (fragment.count <= 0) {
                            preMemoryFragments.RemoveAt(j);
                        } else {
                            preMemoryFragments[j] = fragment;
                        }

                        break;
                    }
                }

                if (!foundMemoryFragment) {
                    AddTransformsToSlice(preTransformCount, jiggleTree, jiggleTreeStruct);
                }
            }

            pendingProcessingAdds.AddRange(pendingAddTrees);
            pendingAddTrees.Clear();
            
            currentTransformAccessIndex = 0;
            commitState = CommitState.ProcessingTransformAccess;
        } else if (commitState == CommitState.ProcessingTransformAccess) {
            GenerateNewAccessArrays(ref currentTransformAccessIndex, out var hasFinished);
            if (!hasFinished) return;
            ReadIn();
            var processingPendingRemoveCount = pendingProcessingRemoves.Count;
            var processingPendingAddCount = pendingProcessingAdds.Count;
            
            #region Removing
            Profiler.BeginSample("JiggleMemoryBus.Commit.Remove");
            for (int i = 0; i < processingPendingRemoveCount; i++) {
                var currentRemoveID = pendingProcessingRemoves[i];
                RemoveTree(currentRemoveID);
            }
            pendingProcessingRemoves.Clear();
            Profiler.EndSample();
            #endregion
            
            #region Adding
            Profiler.BeginSample("JiggleMemoryBus.Commit.Add");
            for (int i = 0; i < processingPendingAddCount; i++) {
                var jiggleTree = pendingProcessingAdds[i];
                var jiggleTreeStruct = jiggleTree.GetStruct();
                var memoryFragmentCount = memoryFragments.Count;
                
                bool foundMemoryFragment = false;
                for (int j = 0; j < memoryFragmentCount; j++) {
                    var fragment = memoryFragments[j];
                    if (fragment.count >= jiggleTreeStruct.pointCount) {
                        foundMemoryFragment = true;
                        AddTreeToSlice(fragment.startIndex, jiggleTree, jiggleTreeStruct);
                        fragment.startIndex += (int)jiggleTreeStruct.pointCount;
                        fragment.count -= (int)jiggleTreeStruct.pointCount;
                        if (fragment.count <= 0) {
                            memoryFragments.RemoveAt(j);
                        } else {
                            memoryFragments[j] = fragment;
                        }
                        break;
                    }
                }

                if (!foundMemoryFragment) {
                    AddTreeToSlice(transformCount, jiggleTree, jiggleTreeStruct);
                }
            }
            pendingProcessingAdds.Clear();
            Profiler.EndSample();
            #endregion

            WriteOut();
            FlipTransformAccessArrays();
            commitState = CommitState.RecreatingAccessArrays;
        } else if (commitState == CommitState.RecreatingAccessArrays) {
            ClearTransformAccessArrays(out var hasFinished);
            if (hasFinished) {
                commitState = CommitState.Idle;
            }
        }
    }
    
    public void Add(JiggleTree jiggleTree) {
        var count = pendingAddTrees.Count;
        for (int i = 0; i < count; i++) {
            if (pendingAddTrees[i].rootID == jiggleTree.rootID) {
                return;
            }
        }
        pendingAddTrees.Add(jiggleTree);
    }

    public void Remove(int rootBoneInstanceID) {
        var count = pendingAddTrees.Count;
        for (int i = 0; i < count; i++) {
            if (pendingAddTrees[i].rootID == rootBoneInstanceID) {
                pendingAddTrees.RemoveAt(i);
                return;
            }
        }
        pendingRemoveTrees.Add(rootBoneInstanceID);
    }

    void GenerateNewAccessArrays(ref int currentIndex, out bool hasFinished) {
        Profiler.BeginSample("RegenerateTransformAccessArrays");
        var count = transformAccessList.Count;
        if (!newTransformAccessArray.isCreated) {
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
            Profiler.EndSample();
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
            interpolationOutputPoses.Dispose();
            simulationOutputPoseData.Dispose();
            interpolationCurrentPoseData.Dispose();
            interpolationPreviousPoseData.Dispose();
            colliderPositions.Dispose();
        }

        if (transformAccessArray.isCreated) {
            transformAccessArray.Dispose();
        }

        if (transformRootAccessArray.isCreated) {
            transformRootAccessArray.Dispose();
        }
        
        if (newTransformAccessArray.isCreated) {
            newTransformAccessArray.Dispose();
        }

        if (newTransformRootAccessArray.isCreated) {
            newTransformRootAccessArray.Dispose();
        }
        
        if (colliderTransformAccessArray.isCreated) {
            colliderTransformAccessArray.Dispose();
        }
    }
    
}
