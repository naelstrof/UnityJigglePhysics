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
}

public class JiggleMemoryBus {
    // : IContainer<JiggleTreeStruct> {
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

    public JiggleDoubleBufferTransformAccessArray doubleBufferTransformAccessArray;

    public TransformAccessArray colliderTransformAccessArray;

    private List<JiggleTree> pendingAddTrees;
    private List<int> pendingRemoveTrees;

    private List<JiggleTree> pendingProcessingAdds;
    private List<int> pendingProcessingRemoves;

    private JiggleMemoryFragmentCollection preMemoryFragmentCollection;
    private JiggleMemoryFragmentCollection memoryFragmentCollection;

    private int preTransformCount;

    public int transformCount;
    public int treeCount;

    private int currentTransformAccessIndex = 0;

    public TransformAccessArray GetTransformAccessArray() => doubleBufferTransformAccessArray.GetTransformAccessArray();

    public TransformAccessArray GetTransformRootAccessArray() =>
        doubleBufferTransformAccessArray.GetTransformRootAccessArray();

    public void RotateBuffers() {
        var tempPoses = interpolationPreviousPoseData;
        interpolationPreviousPoseData = interpolationCurrentPoseData;
        interpolationCurrentPoseData = simulationOutputPoseData;
        simulationOutputPoseData = tempPoses;
    }


    private void ResizeTransformCapacity(int newTransformCapacity) {
        preMemoryFragmentCollection.Resize(newTransformCapacity);
        memoryFragmentCollection.Resize(newTransformCapacity);
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
        var newJiggleTreeStructsArray = new JiggleTreeStruct[newTreeCapacity];

        if (jiggleTreeStructsArray != null) {
            System.Array.Copy(jiggleTreeStructsArray, newJiggleTreeStructsArray,
                System.Math.Min(treeCount, newTreeCapacity));
        }

        jiggleTreeStructsArray = newJiggleTreeStructsArray;

        if (jiggleTreeStructs.IsCreated) {
            jiggleTreeStructs.Dispose();
        }

        jiggleTreeStructs = new NativeArray<JiggleTreeStruct>(jiggleTreeStructsArray, Allocator.Persistent);
        treeCapacity = newTreeCapacity;
    }

    public JiggleMemoryBus() {
        pendingAddTrees = new();
        pendingRemoveTrees = new();
        pendingProcessingRemoves = new();
        pendingProcessingAdds = new();
        preMemoryFragmentCollection = new JiggleMemoryFragmentCollection(4096);
        memoryFragmentCollection = new JiggleMemoryFragmentCollection(4096);

        ResizeTransformCapacity(4096);
        ResizeTreeCapacity(512);

        WriteOut();

        transformAccessList = new List<Transform>();
        transformRootAccessList = new List<Transform>();
        doubleBufferTransformAccessArray = new JiggleDoubleBufferTransformAccessArray(128);

        colliderTransformAccessArray = new TransformAccessArray(128);
        colliderPositions = new NativeArray<float3>(new float3[] { }, Allocator.Persistent);
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
            preMemoryFragmentCollection.Free((int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
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

            memoryFragmentCollection.Free((int)removedTree.transformIndexOffset, (int)removedTree.pointCount);
            break;
        }

        Profiler.EndSample();
    }

    private enum CommitState {
        Idle,
        ProcessingTransformAccess,
    }


    private CommitState commitState = CommitState.Idle;

    private void AddTransformsToSlice(int index, JiggleTree jiggleTree, JiggleTreeStruct jiggleTreeStruct) {
        if (treeCount + 1 > treeCapacity) {
            ResizeTreeCapacity(treeCapacity * 2);
        }

        if (index + jiggleTreeStruct.pointCount >= transformCapacity) {
            ResizeTransformCapacity(transformCapacity * 2);
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
            ResizeTreeCapacity(treeCapacity * 2);
        }

        if (index + jiggleTreeStruct.pointCount >= transformCapacity) {
            ResizeTransformCapacity(transformCapacity * 2);
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
            doubleBufferTransformAccessArray.ClearIfNeeded();
            var pendingRemoveCount = pendingRemoveTrees.Count;
            var pendingAddCount = pendingAddTrees.Count;

            if (pendingRemoveCount == 0 && pendingAddCount == 0) {
                return;
            }

            preMemoryFragmentCollection.CopyFrom(memoryFragmentCollection);
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

                var found = preMemoryFragmentCollection.TryAllocate((int)jiggleTreeStruct.pointCount,
                    out var startIndex);
                if (!found) {
                    ResizeTransformCapacity(transformCapacity * 2);
                    preMemoryFragmentCollection.TryAllocate((int)jiggleTreeStruct.pointCount, out startIndex);
                }

                AddTransformsToSlice(startIndex, jiggleTree, jiggleTreeStruct);
            }

            pendingProcessingAdds.AddRange(pendingAddTrees);
            pendingAddTrees.Clear();

            currentTransformAccessIndex = 0;
            commitState = CommitState.ProcessingTransformAccess;
        } else if (commitState == CommitState.ProcessingTransformAccess) {
            doubleBufferTransformAccessArray.GenerateNewAccessArrays(ref currentTransformAccessIndex,
                out var hasFinished, transformAccessList, transformRootAccessList);
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

                var found = memoryFragmentCollection.TryAllocate((int)jiggleTreeStruct.pointCount, out var startIndex);
                if (!found) {
                    ResizeTransformCapacity(transformCapacity * 2);
                    memoryFragmentCollection.TryAllocate((int)jiggleTreeStruct.pointCount, out startIndex);
                }

                AddTreeToSlice(startIndex, jiggleTree, jiggleTreeStruct);
            }

            pendingProcessingAdds.Clear();
            Profiler.EndSample();

            #endregion

            WriteOut();
            doubleBufferTransformAccessArray.Flip();
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

    public int AddSphere(Transform transform) {
        colliderTransformAccessArray.Add(transform);
        colliderPositions.Dispose();
        colliderPositions = new NativeArray<float3>(colliderTransformAccessArray.length, Allocator.Persistent);
        return colliderTransformAccessArray.length - 1;
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
            colliderPositions.Dispose();
        }

        doubleBufferTransformAccessArray?.Dispose();

        if (colliderTransformAccessArray.isCreated) {
            colliderTransformAccessArray.Dispose();
        }
    }

}

}