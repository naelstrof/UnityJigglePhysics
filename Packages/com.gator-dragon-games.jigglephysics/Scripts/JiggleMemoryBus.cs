using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
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
    private JiggleCollider[] personalColliderArray;
    private JiggleCollider[] sceneColliderArray;

    public NativeArray<JiggleTreeJobData> jiggleTreeStructs;
    public NativeArray<JiggleTransform> simulateInputPoses;
    public NativeArray<JiggleTransform> restPoseTransforms;
    public NativeArray<JiggleTransform> previousLocalRestPoseTransforms;
    public NativeArray<float3> rootOutputPositions;
    public NativeArray<JiggleTransform> interpolationOutputPoses;
    public NativeArray<PoseData> simulationOutputPoseData;
    public NativeArray<PoseData> interpolationCurrentPoseData;
    public NativeArray<PoseData> interpolationPreviousPoseData;
    public NativeHashMap<int2, JiggleGridCell> broadPhaseMap;
    
    public NativeArray<JiggleCollider> personalColliders;
    
    public NativeArray<JiggleCollider> sceneColliders;

    private List<Transform> transformAccessList;
    private List<Transform> transformRootAccessList;
    private List<Transform> personalColliderTransformAccessList;
    private List<Transform> sceneColliderTransformAccessList;

    public JiggleDoubleBufferTransformAccessArray doubleBufferTransformAccessArray;
    public JiggleDoubleBufferTransformAccessArray doubleBufferTransformRootAccessArray;
    public JiggleDoubleBufferTransformAccessArray doubleBufferPersonalColliderTransformAccessArray;
    public JiggleDoubleBufferTransformAccessArray doubleBufferSceneColliderTransformAccessArray;

    private List<JiggleTree> pendingAddTrees;
    private List<JiggleTree> pendingRemoveTrees;

    private List<JiggleTree> pendingProcessingAdds;
    private List<JiggleTree> pendingProcessingRemoves;

    private JiggleMemoryFragmenter preMemoryFragmenter;
    private JiggleMemoryFragmenter memoryFragmenter;
    
    private JiggleMemoryFragmenter personalColliderMemoryFragmenter;
    private JiggleMemoryFragmenter sceneColliderMemoryFragmenter;
    
    private List<JiggleColliderSerializable> pendingSceneColliderAdd;
    private List<JiggleColliderSerializable> pendingSceneColliderRemove;

    private bool hasWrittenData = false;
    
    private int preTransformCount;

    public int transformCount;
    public int treeCount;
    
    public int personalColliderCount;
    public int personalColliderCapacity;
    
    public int sceneColliderCount;
    public int sceneColliderCapacity;

    private int currentTransformAccessIndex = 0;
    private int currentRootTransformAccessIndex = 0;
    private int currentPersonalColliderTransformAccessIndex = 0;
    private int currentSceneColliderTransformAccessIndex = 0;
    
    private static List<Transform> dummyTransforms;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init() {
        if (dummyTransforms != null) {
            foreach (Transform t in dummyTransforms) {
                Object.Destroy(t.gameObject);
            }
            dummyTransforms.Clear();
        } else {
            dummyTransforms = new List<Transform>();
        }
    }

    public void GetColliders(out JiggleCollider[] personalColliders, out JiggleCollider[] sceneColliders, out int personalColliderCount, out int sceneColliderCount) {
        if (commitSceneColliderState == CommitState.Idle) { // FIXME: dont reuse arrays here
            ReadIn(this.personalColliders, personalColliderArray, this.personalColliderCount);
            ReadIn(this.sceneColliders, sceneColliderArray, this.sceneColliderCount);
        }
        personalColliders = personalColliderArray;
        sceneColliders = sceneColliderArray;
        personalColliderCount = this.personalColliderCount;
        sceneColliderCount = this.sceneColliderCount;
    }

    public void GetResults(JobHandle interpolationJobHandle, JobHandle simulateJobHandle, out JiggleTransform[] poses, out JiggleTreeJobData[] treeJobData, out int poseCount, out int treeCount) {
        interpolationJobHandle.Complete();
        simulateJobHandle.Complete();

        if (commitTreeState == CommitState.Idle) { // FIXME: don't reuse Arrays here, 
            ReadIn(interpolationOutputPoses, interpolationOutputPosesArray, transformCount);
            ReadIn(jiggleTreeStructs, jiggleTreeStructsArray, this.treeCount);
        }

        poseCount = transformCount;
        treeCount = this.treeCount;
        poses = interpolationOutputPosesArray;
        treeJobData = jiggleTreeStructsArray;
    }

    public static Transform GetDummyTransform(int index) {
        while (dummyTransforms.Count <= index) {
            Transform dummyTransform = new GameObject($"JigglePhysicsDummyTransform{index}").transform;
            Object.DontDestroyOnLoad(dummyTransform.gameObject);
            dummyTransform.gameObject.hideFlags = HideFlags.HideAndDontSave;
            dummyTransforms.Add(dummyTransform);
        }
        return dummyTransforms[index];
    }

    public TransformAccessArray GetTransformAccessArray() => doubleBufferTransformAccessArray.GetTransformAccessArray();
    public TransformAccessArray GetTransformRootAccessArray() => doubleBufferTransformRootAccessArray.GetTransformAccessArray();
    public TransformAccessArray GetPersonalColliderTransformAccessArray() => doubleBufferPersonalColliderTransformAccessArray.GetTransformAccessArray();
    public TransformAccessArray GetSceneColliderTransformAccessArray() => doubleBufferSceneColliderTransformAccessArray.GetTransformAccessArray();

    public void RotateBuffers() {
        var tempPoses = interpolationPreviousPoseData;
        interpolationPreviousPoseData = interpolationCurrentPoseData;
        interpolationCurrentPoseData = simulationOutputPoseData;
        simulationOutputPoseData = tempPoses;
    }

    private void ResizeSceneColliderCapacity(int newColliderCapacity) {
        sceneColliderMemoryFragmenter.Resize(newColliderCapacity);
        var newColliders = new JiggleCollider[newColliderCapacity];
        if (sceneColliderArray != null) {
            System.Array.Copy(sceneColliderArray, newColliders,
                System.Math.Min(sceneColliderCount, newColliderCapacity));
        }
        sceneColliderArray = newColliders;
        if (sceneColliders.IsCreated) {
            sceneColliders.Dispose();
        }
        sceneColliders = new NativeArray<JiggleCollider>(sceneColliderArray, Allocator.Persistent);
        sceneColliderCapacity = newColliderCapacity;
    }

    private void ResizePersonalColliderCapacity(int newColliderCapacity) {
        personalColliderMemoryFragmenter.Resize(newColliderCapacity);
        var newColliders = new JiggleCollider[newColliderCapacity];
        if (personalColliderArray != null) {
            System.Array.Copy(personalColliderArray, newColliders,
                System.Math.Min(personalColliderCount, newColliderCapacity));
        }
        personalColliderArray = newColliders;
        if (personalColliders.IsCreated) {
            personalColliders.Dispose();
        }
        personalColliders = new NativeArray<JiggleCollider>(personalColliderArray, Allocator.Persistent);
        personalColliderCapacity = newColliderCapacity;
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
        pendingSceneColliderAdd = new();
        pendingSceneColliderRemove = new();
        preMemoryFragmenter = new JiggleMemoryFragmenter(4096);
        memoryFragmenter = new JiggleMemoryFragmenter(4096);
        personalColliderMemoryFragmenter = new JiggleMemoryFragmenter(2048);
        sceneColliderMemoryFragmenter = new JiggleMemoryFragmenter(2048);

        ResizeSceneColliderCapacity(2048);
        ResizePersonalColliderCapacity(2048);
        ResizeTransformCapacity(4096);
        ResizeTreeCapacity(512);

        WriteOut();

        transformAccessList = new List<Transform>();
        transformRootAccessList = new List<Transform>();
        personalColliderTransformAccessList = new List<Transform>();
        sceneColliderTransformAccessList = new List<Transform>();
        doubleBufferTransformAccessArray = new JiggleDoubleBufferTransformAccessArray(128);
        doubleBufferTransformRootAccessArray = new JiggleDoubleBufferTransformAccessArray(128);
        doubleBufferPersonalColliderTransformAccessArray = new JiggleDoubleBufferTransformAccessArray(128);
        doubleBufferSceneColliderTransformAccessArray = new JiggleDoubleBufferTransformAccessArray(128);

        transformCount = 0;
        treeCount = 0;
        sceneColliderCount = 0;
        personalColliderCount = 0;
        broadPhaseMap = new NativeHashMap<int2, JiggleGridCell>(128, Allocator.Persistent);
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
        ReadIn(personalColliders, personalColliderArray, personalColliderCount);
        Profiler.EndSample();
    }

    private bool GetIsValid(out string failReason) {
        for (int i = 0; i < treeCount; i++) {
            var tree = jiggleTreeStructsArray[i];
            if (!tree.GetIsValid(out failReason)) {
                return false;
            }
            for (int o=0;o<tree.pointCount;o++) {
                if (!memoryFragmenter.GetIsAllocated(o + (int)tree.transformIndexOffset)) {
                    failReason = $"Transform index {o + tree.transformIndexOffset} in tree {i} is not allocated, invalid access!";
                    return false;
                }
            }
        }

        for (int i = 0; i < sceneColliderCount; i++) {
            var collider = sceneColliderArray[i];
            if (collider.enabled) {
                if (!sceneColliderMemoryFragmenter.GetIsAllocated(i)) {
                    failReason = $"Scene collider index {i} is not allocated, invalid access!";
                    return false;
                }
            }
        }

        for (int i = 0; i < transformCount; i++) {
            var transformInfo = simulationOutputPoseDataArray[i];
            if (!transformInfo.pose.isVirtual) {
                if (!memoryFragmenter.GetIsAllocated(i)) {
                    failReason = $"Transform index {i} is not allocated, invalid access!";
                    return false;
                }
            }
        }

        failReason = "All good!";
        return true;
    }

    private void WriteOut() {
        #if UNITY_EDITOR
        if (!GetIsValid(out var failReason)) {
            Debug.LogError(failReason);
        }
        #endif
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
        NativeArray<JiggleCollider>.Copy(personalColliderArray, personalColliders, personalColliderCount);
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

    private void RemoveTree(JiggleTree tree) {
        int id = tree.rootID;
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

                transformAccessList[j] = GetDummyTransform(j);
                transformRootAccessList[j] = GetDummyTransform(j);
            }

            for (int j = (int)removedTree.colliderIndexOffset; j < removedTree.colliderIndexOffset + removedTree.colliderCount; j++) {
                personalColliderTransformAccessList[j] = GetDummyTransform(j);
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


    private CommitState commitTreeState = CommitState.Idle;
    private CommitState commitSceneColliderState = CommitState.Idle;

    private bool TryAddTransformsToSlice(int index, JiggleTree jiggleTree, JiggleTreeJobData jiggleTreeJobData) {
        // validate
        for (int o = 0; o < jiggleTreeJobData.pointCount; o++) {
            if (jiggleTree.bones[o]) continue;
            Debug.LogError($"JigglePhysics: Cannot add tree with null bone at index {o} to memory bus.");
            return false;
        }

        if (treeCount + 1 > treeCapacity) {
            ResizeTreeCapacity(treeCapacity * 2);
        }

        if (index + jiggleTreeJobData.pointCount >= transformCapacity) {
            ResizeTransformCapacity(transformCapacity * 2);
        }
        
        #region AddColliders

        if (jiggleTreeJobData.colliderCount > 0) {
            var success = personalColliderMemoryFragmenter.TryAllocate((int)jiggleTreeJobData.colliderCount, out var colliderStartIndex);
            if (!success) {
                ResizePersonalColliderCapacity(personalColliderCapacity * 2);
                personalColliderMemoryFragmenter.TryAllocate((int)jiggleTreeJobData.colliderCount, out colliderStartIndex);
            }

            jiggleTree.SetColliderIndexOffset(colliderStartIndex);
            jiggleTreeJobData.colliderIndexOffset = (uint)colliderStartIndex;
            while (personalColliderTransformAccessList.Count < colliderStartIndex + (int)jiggleTreeJobData.colliderCount) {
                personalColliderTransformAccessList.Add(jiggleTree.bones[0]);
            }

            for (int i = 0; i < jiggleTreeJobData.colliderCount; i++) {
                var collider = jiggleTree.personalColliders[i];
                collider.enabled = true;
                personalColliderArray[colliderStartIndex + i] = collider;
                personalColliderTransformAccessList[colliderStartIndex + i] = jiggleTree.personalColliderTransforms[i];
            }

            personalColliderCount = math.max(personalColliderCount, colliderStartIndex + (int)jiggleTreeJobData.colliderCount);
        }

        #endregion


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
        return true;
    }

    private void AddTreeToSlice(int index, JiggleTree jiggleTree, JiggleTreeJobData jiggleTreeJobData) {
        jiggleTree.SetTransformIndexOffset(index);
        jiggleTreeJobData.transformIndexOffset = (uint)index;
        
        if (treeCount + 1 > treeCapacity) {
            ResizeTreeCapacity(treeCapacity * 2);
        }

        if (index + jiggleTreeJobData.pointCount >= transformCapacity) {
            ResizeTransformCapacity(transformCapacity * 2);
        }
        
        jiggleTreeStructsArray[treeCount] = jiggleTreeJobData;
        var root = jiggleTree.bones[0];
        if (!root) {
            root = GetDummyTransform(index);
        }
        float3 rootPos = root.position;
        for (int o = 0; o < jiggleTreeJobData.pointCount; o++) {
            unsafe {
                var point = jiggleTreeJobData.points[o];
                var bone = jiggleTree.bones[o];
                bool hasBone = bone;
                var hasTransform = hasBone && point.hasTransform;
                if (!hasBone) {
                    bone = GetDummyTransform(index + o);
                }
                bone.GetPositionAndRotation(out var pos, out var rot);
                var pose = new JiggleTransform() {
                    isVirtual = !hasTransform,
                    position = pos,
                    rotation = rot,
                };
                var localPose = new JiggleTransform() {
                    isVirtual = !hasTransform,
                    position = jiggleTree.restPositions[o],
                    rotation = jiggleTree.restRotations[o],
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

    public void CommitColliders() {
        if (commitSceneColliderState == CommitState.Idle) {
            doubleBufferSceneColliderTransformAccessArray.ClearIfNeeded();
            var pendingAddSceneColliderCount = pendingSceneColliderAdd.Count;
            var pendingRemoveSceneColliderCount = pendingSceneColliderRemove.Count;

            if (pendingAddSceneColliderCount == 0 && pendingRemoveSceneColliderCount == 0) {
                return;
            }
            for (int i = 0; i < pendingRemoveSceneColliderCount; i++) {
                var collider = pendingSceneColliderRemove[i];
                var id = sceneColliderTransformAccessList.IndexOf(collider.transform);
                if (id != -1) {
                    sceneColliderMemoryFragmenter.Free(id, 1);
                    var sceneCollider = sceneColliderArray[id];
                    sceneCollider.enabled = false;
                    sceneColliderArray[id] = sceneCollider;
                    sceneColliderTransformAccessList[id] = GetDummyTransform(id);
                }
            }
            pendingSceneColliderRemove.Clear();

            for (int i = 0; i < pendingAddSceneColliderCount; i++) {
                var collider = pendingSceneColliderAdd[i];
                collider.collider.enabled = true;
                
                var found = sceneColliderMemoryFragmenter.TryAllocate(1, out var index);
                if (!found) {
                    ResizeSceneColliderCapacity(sceneColliderCapacity * 2);
                    sceneColliderMemoryFragmenter.TryAllocate(1, out index);
                }

                while (sceneColliderTransformAccessList.Count < index+1) {
                    sceneColliderTransformAccessList.Add(collider.transform);
                }
                sceneColliderTransformAccessList[index] = collider.transform;
                sceneColliderArray[index] = collider.collider;
                sceneColliderCount = math.max(index+1, sceneColliderCount);
            }

            pendingSceneColliderAdd.Clear();
            currentSceneColliderTransformAccessIndex = 0;
            commitSceneColliderState = CommitState.ProcessingTransformAccess;
        } else if (commitSceneColliderState == CommitState.ProcessingTransformAccess) {
            doubleBufferSceneColliderTransformAccessArray.GenerateNewAccessArrays(ref currentSceneColliderTransformAccessIndex, out var hasFinishedSceneColliders, sceneColliderTransformAccessList);
            if (!hasFinishedSceneColliders) return;
            NativeArray<JiggleCollider>.Copy(sceneColliderArray, sceneColliders, sceneColliderCount);
            doubleBufferSceneColliderTransformAccessArray.Flip();
            commitSceneColliderState = CommitState.Idle;
        }
    }

    public void CommitTrees() {
        if (commitTreeState == CommitState.Idle) {
            doubleBufferTransformAccessArray.ClearIfNeeded();
            doubleBufferTransformRootAccessArray.ClearIfNeeded();
            doubleBufferPersonalColliderTransformAccessArray.ClearIfNeeded();
            
            var pendingRemoveCount = pendingRemoveTrees.Count;
            var pendingAddCount = pendingAddTrees.Count;

            if (pendingRemoveCount == 0 && pendingAddCount == 0) {
                return;
            }

            preMemoryFragmenter.CopyFrom(memoryFragmenter);
            preTransformCount = transformCount;

            for (int i = 0; i < pendingRemoveCount; i++) {
                var currentRemoveID = pendingRemoveTrees[i].rootID;
                PreRemoveTree(currentRemoveID);
            }

            pendingProcessingRemoves.AddRange(pendingRemoveTrees);
            pendingRemoveTrees.Clear();

            for (int i = 0; i < pendingAddCount; i++) {
                var jiggleTree = pendingAddTrees[i];
                var jiggleTreeStruct = pendingAddTrees[i].GetStruct();
                var pointCount = (int)jiggleTreeStruct.pointCount;

                var found = preMemoryFragmenter.TryAllocate(pointCount, out var startIndex);
                if (!found) {
                    ResizeTransformCapacity(transformCapacity * 2);
                    preMemoryFragmenter.TryAllocate(pointCount, out startIndex);
                }

                if (!TryAddTransformsToSlice(startIndex, jiggleTree, jiggleTreeStruct)) {
                    preMemoryFragmenter.Free(startIndex, pointCount);
                    pendingAddTrees.RemoveAt(i);
                    i=Mathf.Max(i-1,0);
                }
            }

            pendingProcessingAdds.AddRange(pendingAddTrees);
            pendingAddTrees.Clear();
            
            currentTransformAccessIndex = 0;
            currentRootTransformAccessIndex = 0;
            currentPersonalColliderTransformAccessIndex = 0;
            commitTreeState = CommitState.ProcessingTransformAccess;
        } else if (commitTreeState == CommitState.ProcessingTransformAccess) {
            doubleBufferTransformAccessArray.GenerateNewAccessArrays(ref currentTransformAccessIndex, out var hasFinishedTransforms, transformAccessList);
            if (!hasFinishedTransforms) return;
            doubleBufferTransformRootAccessArray.GenerateNewAccessArrays(ref currentRootTransformAccessIndex, out var hasFinishedRoots, transformRootAccessList);
            if (!hasFinishedRoots) return;
            doubleBufferPersonalColliderTransformAccessArray.GenerateNewAccessArrays(ref currentPersonalColliderTransformAccessIndex, out var hasFinishedColliders, personalColliderTransformAccessList);
            if (!hasFinishedColliders) return;
            ReadIn();
            var processingPendingRemoveCount = pendingProcessingRemoves.Count;
            var processingPendingAddCount = pendingProcessingAdds.Count;

            #region Removing

            Profiler.BeginSample("JiggleMemoryBus.Commit.Remove");
            for (int i = 0; i < processingPendingRemoveCount; i++) {
                var tree = pendingProcessingRemoves[i];
                RemoveTree(tree);
                tree.Dispose();
            }

            pendingProcessingRemoves.Clear();
            Profiler.EndSample();

            #endregion

            #region Adding

            Profiler.BeginSample("JiggleMemoryBus.Commit.Add");
            for (int i = 0; i < processingPendingAddCount; i++) {
                var jiggleTree = pendingProcessingAdds[i];
                var jiggleTreeStruct = jiggleTree.GetStruct();
                int pointCount = (int)jiggleTreeStruct.pointCount;

                var found = memoryFragmenter.TryAllocate(pointCount, out var startIndex);
                if (!found) {
                    ResizeTransformCapacity(transformCapacity * 2);
                    memoryFragmenter.TryAllocate(pointCount, out startIndex);
                }

                AddTreeToSlice(startIndex, jiggleTree, jiggleTreeStruct);
            }

            pendingProcessingAdds.Clear();
            Profiler.EndSample();

            #endregion

            WriteOut();
            doubleBufferTransformAccessArray.Flip();
            doubleBufferTransformRootAccessArray.Flip();
            doubleBufferPersonalColliderTransformAccessArray.Flip();
            commitTreeState = CommitState.Idle;
        }
    }

    public void Add(JiggleColliderSerializable jiggleCollider) {
        var count = pendingSceneColliderAdd.Count;
        for (int i = 0; i < count; i++) {
            if (pendingSceneColliderAdd[i].transform == jiggleCollider.transform) {
                return;
            }
        }
        pendingSceneColliderAdd.Add(jiggleCollider);
    }

    public void Remove(JiggleColliderSerializable jiggleCollider) {
        var count = pendingSceneColliderAdd.Count;
        for (int i = 0; i < count; i++) {
            if (pendingSceneColliderAdd[i].transform == jiggleCollider.transform) {
                pendingSceneColliderAdd.RemoveAt(i);
                return;
            }
        }

        pendingSceneColliderRemove.Add(jiggleCollider);
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

    public void Remove(JiggleTree jiggleTree) {
        var count = pendingAddTrees.Count;
        for (int i = 0; i < count; i++) {
            if (pendingAddTrees[i].rootID == jiggleTree.rootID) {
                pendingAddTrees.RemoveAt(i);
                return;
            }
        }

        pendingRemoveTrees.Add(jiggleTree);
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
            personalColliders.Dispose();
            sceneColliders.Dispose();
        }

        var values = broadPhaseMap.GetValueArray(Allocator.Temp);
        var gridCells = new JiggleGridCell[values.Length];
        values.CopyTo(gridCells);
        values.Dispose();
        for (int i = 0; i < gridCells.Length; i++) {
            gridCells[i].Dispose();
        }
        broadPhaseMap.Dispose();

        doubleBufferTransformAccessArray?.Dispose();
        doubleBufferTransformRootAccessArray?.Dispose();
        doubleBufferPersonalColliderTransformAccessArray?.Dispose();
        doubleBufferSceneColliderTransformAccessArray?.Dispose();
    }

}

}