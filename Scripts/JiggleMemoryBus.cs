using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace GatorDragonGames.JigglePhysics {

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

    public override string ToString() {
        return $"(Pose: {pose}, RootPosition: {rootPosition}, RootOffset: {rootOffset})";
    }
}

public class JiggleMemoryBus {
    // : IContainer<JiggleTreeStruct> {
    private int treeCapacity = 0;
    private int transformCapacity = 0;
    private JiggleTreeJobData[] jiggleTreeStructsArray;
    private JiggleTransform[] simulateInputPosesArray;
    private JiggleTransform[] restPoseTransformsArray;
    private JiggleTransform[] previousLocalRestPoseTransformsArray;
    private float3[] rootOutputPositionsArray;
    private JiggleTransform[] interpolationOutputPosesArray;
    private PoseData[] simulationOutputPoseDataArray;
    private PoseData[] interpolationCurrentPoseDataArray;
    private PoseData[] interpolationPreviousPoseDataArray;
    private JiggleCollider[] jiggleCollidersArray;

    public NativeArray<JiggleTreeJobData> jiggleTreeStructs;
    public NativeArray<JiggleTransform> simulateInputPoses;
    public NativeArray<JiggleTransform> restPoseTransforms;
    public NativeArray<JiggleTransform> previousLocalRestPoseTransforms;
    public NativeArray<float3> rootOutputPositions;
    public NativeArray<JiggleTransform> interpolationOutputPoses;
    public NativeArray<PoseData> simulationOutputPoseData;
    public NativeArray<PoseData> interpolationCurrentPoseData;
    public NativeArray<PoseData> interpolationPreviousPoseData;
    
    public NativeArray<JiggleCollider> colliders;

    private List<Transform> transformAccessList;
    private List<Transform> transformRootAccessList;
    private List<Transform> colliderTransformAccessList;

    public JiggleDoubleBufferTransformAccessArray doubleBufferTransformAccessArray;
    public JiggleDoubleBufferTransformAccessArray doubleBufferTransformRootAccessArray;
    public JiggleDoubleBufferTransformAccessArray doubleBufferColliderTransformAccessArray;

    private List<JiggleTree> pendingAddTrees;
    private List<int> pendingRemoveTrees;

    private List<JiggleTree> pendingProcessingAdds;
    private List<int> pendingProcessingRemoves;

    private JiggleMemoryFragmenter preMemoryFragmenter;
    private JiggleMemoryFragmenter memoryFragmenter;
    
    private JiggleMemoryFragmenter colliderMemoryFragmenter;
    
    
    private int preTransformCount;

    public int transformCount;
    public int treeCount;
    
    public int colliderCount;
    public int colliderCapacity;

    private int currentTransformAccessIndex = 0;
    private int currentRootTransformAccessIndex = 0;
    private int currentColliderTransformAccessIndex = 0;

    public TransformAccessArray GetTransformAccessArray() => doubleBufferTransformAccessArray.GetTransformAccessArray();

    public TransformAccessArray GetTransformRootAccessArray() => doubleBufferTransformRootAccessArray.GetTransformAccessArray();
    public TransformAccessArray GetColliderTransformAccessArray() => doubleBufferColliderTransformAccessArray.GetTransformAccessArray();

    public void RotateBuffers() {
        var tempPoses = interpolationPreviousPoseData;
        interpolationPreviousPoseData = interpolationCurrentPoseData;
        interpolationCurrentPoseData = simulationOutputPoseData;
        simulationOutputPoseData = tempPoses;
    }

    private void ResizeColliderCapacity(int newColliderCapacity) {
        colliderMemoryFragmenter.Resize(newColliderCapacity);
        var newColliders = new JiggleCollider[newColliderCapacity];
        if (jiggleCollidersArray != null) {
            System.Array.Copy(jiggleCollidersArray, newColliders,
                System.Math.Min(colliderCount, newColliderCapacity));
        }
        jiggleCollidersArray = newColliders;
        if (colliders.IsCreated) {
            colliders.Dispose();
        }
        colliders = new NativeArray<JiggleCollider>(jiggleCollidersArray, Allocator.Persistent);
        colliderCapacity = newColliderCapacity;
    }

    private void ResizeTransformCapacity(int newTransformCapacity) {
        preMemoryFragmenter.Resize(newTransformCapacity);
        memoryFragmenter.Resize(newTransformCapacity);
        var newSimulateInputPosesArray = new JiggleTransform[newTransformCapacity];
        var newRestPoseTransformsArray = new JiggleTransform[newTransformCapacity];
        var newPreviousLocalRestPoseTransformsArray = new JiggleTransform[newTransformCapacity];
        var newRootOutputPositionsArray = new float3[newTransformCapacity];
        var newInterpolationOutputPosesArray = new JiggleTransform[newTransformCapacity];
        var newSimulationOutputPoseDataArray = new PoseData[newTransformCapacity];
        var newInterpolationCurrentPoseDataArray = new PoseData[newTransformCapacity];
        var newInterpolationPreviousPoseDataArray = new PoseData[newTransformCapacity];

        if (jiggleTreeStructsArray != null) {
            System.Array.Copy(simulateInputPosesArray, newSimulateInputPosesArray,
                System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(restPoseTransformsArray, newRestPoseTransformsArray,
                System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(previousLocalRestPoseTransformsArray, newPreviousLocalRestPoseTransformsArray,
                System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(rootOutputPositionsArray, newRootOutputPositionsArray,
                System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(interpolationOutputPosesArray, newInterpolationOutputPosesArray,
                System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(simulationOutputPoseDataArray, newSimulationOutputPoseDataArray,
                System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(interpolationCurrentPoseDataArray, newInterpolationCurrentPoseDataArray,
                System.Math.Min(transformCount, newTransformCapacity));
            System.Array.Copy(interpolationPreviousPoseDataArray, newInterpolationPreviousPoseDataArray,
                System.Math.Min(transformCount, newTransformCapacity));
        }

        simulateInputPosesArray = newSimulateInputPosesArray;
        restPoseTransformsArray = newRestPoseTransformsArray;
        previousLocalRestPoseTransformsArray = newPreviousLocalRestPoseTransformsArray;
        rootOutputPositionsArray = newRootOutputPositionsArray;
        interpolationOutputPosesArray = newInterpolationOutputPosesArray;
        simulationOutputPoseDataArray = newSimulationOutputPoseDataArray;
        interpolationCurrentPoseDataArray = newInterpolationCurrentPoseDataArray;
        interpolationPreviousPoseDataArray = newInterpolationPreviousPoseDataArray;

        if (jiggleTreeStructs.IsCreated) {
            simulateInputPoses.Dispose();
            restPoseTransforms.Dispose();
            previousLocalRestPoseTransforms.Dispose();
            rootOutputPositions.Dispose();
            interpolationOutputPoses.Dispose();
            simulationOutputPoseData.Dispose();
            interpolationCurrentPoseData.Dispose();
            interpolationPreviousPoseData.Dispose();
        }

        simulateInputPoses = new NativeArray<JiggleTransform>(simulateInputPosesArray, Allocator.Persistent);
        restPoseTransforms = new NativeArray<JiggleTransform>(restPoseTransformsArray, Allocator.Persistent);
        previousLocalRestPoseTransforms =
            new NativeArray<JiggleTransform>(previousLocalRestPoseTransformsArray, Allocator.Persistent);
        rootOutputPositions = new NativeArray<float3>(rootOutputPositionsArray, Allocator.Persistent);
        interpolationOutputPoses =
            new NativeArray<JiggleTransform>(interpolationOutputPosesArray, Allocator.Persistent);
        simulationOutputPoseData = new NativeArray<PoseData>(simulationOutputPoseDataArray, Allocator.Persistent);
        interpolationCurrentPoseData =
            new NativeArray<PoseData>(interpolationCurrentPoseDataArray, Allocator.Persistent);
        interpolationPreviousPoseData =
            new NativeArray<PoseData>(interpolationPreviousPoseDataArray, Allocator.Persistent);

        transformCapacity = newTransformCapacity;
    }

    private void ResizeTreeCapacity(int newTreeCapacity) {
        var newJiggleTreeStructsArray = new JiggleTreeJobData[newTreeCapacity];

        if (jiggleTreeStructsArray != null) {
            System.Array.Copy(jiggleTreeStructsArray, newJiggleTreeStructsArray,
                System.Math.Min(treeCount, newTreeCapacity));
        }

        jiggleTreeStructsArray = newJiggleTreeStructsArray;

        if (jiggleTreeStructs.IsCreated) {
            jiggleTreeStructs.Dispose();
        }

        jiggleTreeStructs = new NativeArray<JiggleTreeJobData>(jiggleTreeStructsArray, Allocator.Persistent);
        treeCapacity = newTreeCapacity;
    }

    public JiggleMemoryBus() {
        pendingAddTrees = new();
        pendingRemoveTrees = new();
        pendingProcessingRemoves = new();
        pendingProcessingAdds = new();
        preMemoryFragmenter = new JiggleMemoryFragmenter(4096);
        memoryFragmenter = new JiggleMemoryFragmenter(4096);
        colliderMemoryFragmenter = new JiggleMemoryFragmenter(4096);

        ResizeColliderCapacity(4096);
        ResizeTransformCapacity(4096);
        ResizeTreeCapacity(512);

        WriteOut();

        transformAccessList = new List<Transform>();
        transformRootAccessList = new List<Transform>();
        colliderTransformAccessList = new List<Transform>();
        doubleBufferTransformAccessArray = new JiggleDoubleBufferTransformAccessArray(128);
        doubleBufferTransformRootAccessArray = new JiggleDoubleBufferTransformAccessArray(128);
        doubleBufferColliderTransformAccessArray = new JiggleDoubleBufferTransformAccessArray(128);

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
        NativeArray<JiggleTreeJobData>.Copy(jiggleTreeStructsArray, jiggleTreeStructs, treeCount);
        NativeArray<JiggleTransform>.Copy(simulateInputPosesArray, simulateInputPoses, transformCount);
        NativeArray<JiggleTransform>.Copy(restPoseTransformsArray, restPoseTransforms, transformCount);
        NativeArray<JiggleTransform>.Copy(previousLocalRestPoseTransformsArray, previousLocalRestPoseTransforms,
            transformCount);
        NativeArray<float3>.Copy(rootOutputPositionsArray, rootOutputPositions, transformCount);
        NativeArray<JiggleTransform>.Copy(interpolationOutputPosesArray, interpolationOutputPoses, transformCount);
        NativeArray<PoseData>.Copy(simulationOutputPoseDataArray, simulationOutputPoseData, transformCount);
        NativeArray<PoseData>.Copy(interpolationCurrentPoseDataArray, interpolationCurrentPoseData, transformCount);
        NativeArray<PoseData>.Copy(interpolationPreviousPoseDataArray, interpolationPreviousPoseData, transformCount);
        Profiler.EndSample();
    }

    private void PreRemoveTree(int id) {
        for (int i = 0; i < treeCount; i++) {
            var removedTree = jiggleTreeStructsArray[i];
            if (removedTree.rootID != id) continue;
            preMemoryFragmenter.Free((int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
            break;
        }
    }

    private void RemoveTree(int id) {
        Profiler.BeginSample("JiggleMemoryBus.RemoveTree");
        for (int i = 0; i < treeCount; i++) {
            var removedTree = jiggleTreeStructsArray[i];
            if (removedTree.rootID != id) continue;
            int shiftCount = treeCount - i - 1;
            if (shiftCount > 0) {
                System.Array.Copy(jiggleTreeStructsArray, i + 1, jiggleTreeStructsArray, i, shiftCount);
            }

            treeCount--;
            for (int j = (int)removedTree.transformIndexOffset;
                 j < removedTree.transformIndexOffset + removedTree.pointCount;
                 j++) {
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

            memoryFragmenter.Free((int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
            break;
        }

        Profiler.EndSample();
    }

    private enum CommitState {
        Idle,
        ProcessingTransformAccess,
    }


    private CommitState commitState = CommitState.Idle;

    private void AddTransformsToSlice(int index, JiggleTree jiggleTree, JiggleTreeJobData jiggleTreeJobData) {
        if (treeCount + 1 > treeCapacity) {
            ResizeTreeCapacity(treeCapacity * 2);
        }

        if (index + jiggleTreeJobData.pointCount >= transformCapacity) {
            ResizeTransformCapacity(transformCapacity * 2);
        }

        var rootBone = jiggleTree.bones[0];

        var desiredCount = index + (int)jiggleTreeJobData.pointCount;
        while (transformAccessList.Count < desiredCount) {
            transformAccessList.Add(rootBone);
            transformRootAccessList.Add(rootBone);
        }

        for (int o = 0; o < jiggleTreeJobData.pointCount; o++) {
            transformAccessList[index + o] = jiggleTree.bones[o];
            transformRootAccessList[index + o] = rootBone;
        }

        preTransformCount = math.max(index + (int)jiggleTreeJobData.pointCount, preTransformCount);
    }

    private void AddTreeToSlice(int index, JiggleTree jiggleTree, JiggleTreeJobData jiggleTreeJobData) {
        jiggleTreeJobData.transformIndexOffset = (uint)index;
        
        #region AddColliders

        if (jiggleTreeJobData.colliderCount > 0) {
            var success =
                colliderMemoryFragmenter.TryAllocate((int)jiggleTreeJobData.colliderCount, out var colliderStartIndex);
            if (!success) {
                ResizeColliderCapacity(colliderCapacity * 2);
                colliderMemoryFragmenter.TryAllocate((int)jiggleTreeJobData.colliderCount, out colliderStartIndex);
            }

            jiggleTreeJobData.colliderIndexOffset = (uint)colliderStartIndex;
            while (colliderTransformAccessList.Count < colliderStartIndex + (int)jiggleTreeJobData.colliderCount) {
                colliderTransformAccessList.Add(jiggleTree.bones[0]);
            }

            for (int i = 0; i < jiggleTreeJobData.colliderCount; i++) {
                jiggleCollidersArray[colliderStartIndex + i] = jiggleTree.personalColliders[i];
                colliderTransformAccessList[colliderStartIndex + i] = jiggleTree.personalColliderTransforms[i];
            }

            colliderCount = math.max(colliderCount, colliderStartIndex + (int)jiggleTreeJobData.colliderCount);
        }

        #endregion
             
        if (treeCount + 1 > treeCapacity) {
            ResizeTreeCapacity(treeCapacity * 2);
        }

        if (index + jiggleTreeJobData.pointCount >= transformCapacity) {
            ResizeTransformCapacity(transformCapacity * 2);
        }
        
        jiggleTreeStructsArray[treeCount] = jiggleTreeJobData;
        float3 rootPos = jiggleTree.bones[0].position;
        for (int o = 0; o < jiggleTreeJobData.pointCount; o++) {
            unsafe {
                var point = jiggleTreeJobData.points[o];
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
        transformCount = math.max((int)(index + jiggleTreeJobData.pointCount), transformCount);
    }

    public void Commit() {
        if (commitState == CommitState.Idle) {
            doubleBufferTransformAccessArray.ClearIfNeeded();
            doubleBufferTransformRootAccessArray.ClearIfNeeded();
            doubleBufferColliderTransformAccessArray.ClearIfNeeded();
            var pendingRemoveCount = pendingRemoveTrees.Count;
            var pendingAddCount = pendingAddTrees.Count;

            if (pendingRemoveCount == 0 && pendingAddCount == 0) {
                return;
            }

            preMemoryFragmenter.CopyFrom(memoryFragmenter);
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

                var found = preMemoryFragmenter.TryAllocate((int)jiggleTreeStruct.pointCount,
                    out var startIndex);
                if (!found) {
                    ResizeTransformCapacity(transformCapacity * 2);
                    preMemoryFragmenter.TryAllocate((int)jiggleTreeStruct.pointCount, out startIndex);
                }

                AddTransformsToSlice(startIndex, jiggleTree, jiggleTreeStruct);
            }

            pendingProcessingAdds.AddRange(pendingAddTrees);
            pendingAddTrees.Clear();

            currentTransformAccessIndex = 0;
            currentRootTransformAccessIndex = 0;
            currentColliderTransformAccessIndex = 0;
            commitState = CommitState.ProcessingTransformAccess;
        } else if (commitState == CommitState.ProcessingTransformAccess) {
            doubleBufferTransformAccessArray.GenerateNewAccessArrays(ref currentTransformAccessIndex, out var hasFinishedTransforms, transformAccessList);
            if (!hasFinishedTransforms) return;
            doubleBufferTransformRootAccessArray.GenerateNewAccessArrays(ref currentRootTransformAccessIndex, out var hasFinishedRoots, transformRootAccessList);
            if (!hasFinishedRoots) return;
            doubleBufferColliderTransformAccessArray.GenerateNewAccessArrays(ref currentColliderTransformAccessIndex, out var hasFinishedColliders, colliderTransformAccessList);
            if (!hasFinishedColliders) return;
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

                var found = memoryFragmenter.TryAllocate((int)jiggleTreeStruct.pointCount, out var startIndex);
                if (!found) {
                    ResizeTransformCapacity(transformCapacity * 2);
                    memoryFragmenter.TryAllocate((int)jiggleTreeStruct.pointCount, out startIndex);
                }

                AddTreeToSlice(startIndex, jiggleTree, jiggleTreeStruct);
            }

            pendingProcessingAdds.Clear();
            Profiler.EndSample();

            #endregion

            WriteOut();
            doubleBufferTransformAccessArray.Flip();
            doubleBufferTransformRootAccessArray.Flip();
            doubleBufferColliderTransformAccessArray.Flip();
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
            colliders.Dispose();
        }

        doubleBufferTransformAccessArray?.Dispose();
        doubleBufferColliderTransformAccessArray?.Dispose();
        doubleBufferColliderTransformAccessArray?.Dispose();
    }

}

}