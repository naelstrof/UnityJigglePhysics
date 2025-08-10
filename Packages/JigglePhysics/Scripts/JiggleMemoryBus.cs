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
        var tempPoses = interpolationPreviousPoseData;
        interpolationPreviousPoseData = interpolationCurrentPoseData;
        interpolationCurrentPoseData = simulationOutputPoseData;
        simulationOutputPoseData = tempPoses;
    }


    private void Resize(int newTransformCapacity, int newTreeCapacity) {
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
            int shiftCount = treeCount - i - 1;
            if (shiftCount > 0) {
                System.Array.Copy(jiggleTreeStructsArray, i + 1, jiggleTreeStructsArray, i, shiftCount);
                for (int j = i; j < i + shiftCount; j++) {
                    jiggleTreeStructsArray[j].transformIndexOffset -= removedTree.pointCount;
                }
            }
            treeCount--;
            Profiler.EndSample();
            Profiler.BeginSample("JiggleMemoryBus.RemoveTree.RemoveRanges");
            RemoveRange(simulateInputPosesArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
            RemoveRange(restPoseTransformsArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
            RemoveRange(previousLocalRestPoseTransformsArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
            RemoveRange(rootOutputPositionsArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
            RemoveRange(interpolationOutputPosesArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
            RemoveRange(simulationOutputPoseDataArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
            RemoveRange(interpolationCurrentPoseDataArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
            RemoveRange(interpolationPreviousPoseDataArray, (int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
            transformCount -= (int)removedTree.pointCount;
            Profiler.EndSample();
            break;
        }
        Profiler.EndSample();
    }
    
    private void RemoveRange<T>(T[] array, int index, int count) {
        Profiler.BeginSample("JiggleMemoryBus.RemoveRange.Copy");
        int tailCount = transformCount - (index + count);
        if (tailCount > 0) {
            System.Array.Copy(array, index + count, array, index, tailCount);
        }
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

            for (int i = 0; i < pendingRemoveCount; i++) {
                var currentRemoveID = pendingRemoveTrees[i];
                RemoveTransformsForTree(currentRemoveID);
            }

            pendingProcessingRemoves.AddRange(pendingRemoveTrees);
            pendingRemoveTrees.Clear();
            
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
            
            currentTransformAccessIndex = 0;
            commitState = CommitState.Processing;
        } else if (commitState == CommitState.Processing) {
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
                jiggleTreeStruct.transformIndexOffset = (uint)transformCount;

                if (treeCount + 1 > treeCapacity) {
                    Resize(transformCapacity, treeCapacity * 2);
                }

                if (transformCount + jiggleTreeStruct.pointCount >= transformCapacity) {
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
                        simulateInputPosesArray[transformCount + o] = pose;
                        restPoseTransformsArray[transformCount + o] = localPose;
                        previousLocalRestPoseTransformsArray[transformCount + o] = localPose;
                        rootOutputPositionsArray[transformCount + o] = rootPos;
                        interpolationOutputPosesArray[transformCount + o] = pose;
                        var poseData = new PoseData() {
                            pose = pose,
                            rootOffset = new float3(0f),
                            rootPosition = rootPos,
                        };
                        simulationOutputPoseDataArray[transformCount + o] = poseData;
                        interpolationCurrentPoseDataArray[transformCount + o] = poseData;
                        interpolationPreviousPoseDataArray[transformCount + o] = poseData;
                    }
                }
                treeCount++;
                transformCount += (int)jiggleTreeStruct.pointCount;
            }
            pendingProcessingAdds.Clear();
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
        
        if (colliderTransformAccessArray.isCreated) {
            colliderTransformAccessArray.Dispose();
        }
    }
    
}
