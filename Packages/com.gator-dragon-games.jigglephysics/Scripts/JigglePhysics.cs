using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GatorDragonGames.JigglePhysics {

public static class JigglePhysics {
    private static Dictionary<Transform, JiggleTreeSegment> jiggleRootLookup;
    private static bool _globalDirty = true;
    private static readonly List<Transform> tempTransforms = new List<Transform>();
    private static readonly List<JiggleSimulatedPoint> tempPoints = new List<JiggleSimulatedPoint>();
    private static readonly List<JigglePointParameters> tempParameters = new List<JigglePointParameters>();
    private static readonly List<JiggleCollider> tempColliders = new List<JiggleCollider>();
    private static readonly List<Transform> tempColliderTransforms = new List<Transform>();
    private static List<JiggleTreeSegment> rootJiggleTreeSegments;
    private static bool initializedRendering = false;

    private static double lastFixedCurrentTime = 0f;
    public const float MERGE_DISTANCE = 0.001f;

    private static JiggleJobs jobs;
    private static bool hasRunThisFrame;

    public static void ScheduleSimulate(double fixedCurrentTime, float fixedDeltaTime) {
        if (hasRunThisFrame) {
            return;
        }
        if (Math.Abs(lastFixedCurrentTime - fixedCurrentTime) < 0.0001f) {
            return;
        }
        
        var rootJiggleTreeSegmentsCount = rootJiggleTreeSegments.Count;
        for (int i = 0; i < rootJiggleTreeSegmentsCount; i++) {
            var segment = rootJiggleTreeSegments[i];
            segment.UpdateParametersIfNeeded();
        }
        
        lastFixedCurrentTime = fixedCurrentTime;

        jobs = GetJiggleJobs(fixedCurrentTime, fixedDeltaTime);
        jobs.Simulate(fixedCurrentTime);
        hasRunThisFrame = true;
    }

    public static void SchedulePose(double currentTime) {
        hasRunThisFrame = false;
        jobs?.SchedulePoses(currentTime);
    }


    public static void CompletePose() {
        jobs?.CompletePoses();
    }

    public static void OnDrawGizmos() {
        if (!Application.isPlaying) {
            return;
        }

        jobs?.OnDrawGizmos();
    }
    private static List<JigglePointParameters> parametersCache;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        parametersCache = new();
        rootJiggleTreeSegments = new List<JiggleTreeSegment>();
        jiggleRootLookup = new Dictionary<Transform, JiggleTreeSegment>();
        initializedRendering = false;
        _globalDirty = true;
        jobs?.Dispose();
        jobs = new JiggleJobs(Time.fixedTimeAsDouble, Time.fixedDeltaTime);
    }

    public static void Dispose() {
        jobs?.Dispose();
        JiggleRenderer.Dispose();
        rootJiggleTreeSegments = new List<JiggleTreeSegment>();
        jiggleRootLookup = new Dictionary<Transform, JiggleTreeSegment>();
        _globalDirty = true;
        jobs = null;
    }

    public static void ScheduleRender() {
        if (!initializedRendering) {
            JiggleRenderer.OnEnable(jobs);
            initializedRendering = true;
        }
        JiggleRenderer.PrepareRender(jobs);
    }

    public static void CompleteRender(Material proceduralMaterial, Mesh sphere) {
        if (!initializedRendering) {
            JiggleRenderer.OnEnable(jobs);
            initializedRendering = true;
        }

        JiggleRenderer.FinishRender(proceduralMaterial, sphere);
    }
    
    public static void SetGlobalDirty() => _globalDirty = true;

    public static void AddJiggleCollider(JiggleColliderSerializable collider) {
        jobs.ScheduleAdd(collider);
    }

    public static void RemoveJiggleCollider(JiggleColliderSerializable collider) {
        jobs?.ScheduleRemove(collider);
    }
    public static void FreeOnComplete(IntPtr pointer) {
        jobs.FreeOnComplete(pointer);
    }
    
    public static void AddJiggleTreeSegment(JiggleTreeSegment jiggleTreeSegment) {
        if (!jiggleRootLookup.TryAdd(jiggleTreeSegment.transform, jiggleTreeSegment)) {
            Debug.LogError("Multiple Jiggle trees detected targeting the same root transform, Jiggle Physics doesn't support this.", jiggleTreeSegment.transform);
            return;
        }
        RemoveAddChildren(jiggleTreeSegment.transform);
        TryAddRootJiggleTreeSegment(jiggleTreeSegment);
        _globalDirty = true;
    }
    
    private static bool GetParentJiggleTreeSegment(Transform t, out JiggleTreeSegment parentJiggleTreeSegment) {
        var current = t;
        while (current.parent != null) {
            current = current.parent;
            if (jiggleRootLookup.TryGetValue(current, out var jiggleTreeSegment)) {
                parentJiggleTreeSegment = jiggleTreeSegment;
                return true;
            }
        }
        parentJiggleTreeSegment = null;
        return false;
    }
    
    private static bool GetJiggleTreeSegmentByBone(Transform t, out JiggleTreeSegment jiggleRoot) {
        if (jiggleRootLookup.TryGetValue(t, out JiggleTreeSegment root)) {
            jiggleRoot = root;
            return true;
        }
        jiggleRoot = null;
        return false;
    }

    private static bool TryAddRootJiggleTreeSegment(JiggleTreeSegment jiggleTreeSegment) {
        var foundParent = GetParentJiggleTreeSegment(jiggleTreeSegment.transform, out var parentJiggleTreeSegment);
        if (foundParent) {
            jiggleTreeSegment.SetParent(parentJiggleTreeSegment);
            parentJiggleTreeSegment.SetDirty();
            return false;
        } else {
            rootJiggleTreeSegments.Add(jiggleTreeSegment);
            return true;
        }
    }

    private static void RemoveAddChildren(Transform t) {
        foreach (Transform child in t) {
            if (jiggleRootLookup.TryGetValue(child, out var jiggleRootSegment)) {
                rootJiggleTreeSegments.Remove(jiggleRootSegment);
                TryAddRootJiggleTreeSegment(jiggleRootSegment);
            }
            RemoveAddChildren(child);
        }
    }
    
    private static JiggleJobs GetJiggleJobs(double fixedTime, float fixedDeltaTime) {
        if (!_globalDirty) {
            return jobs;
        }
        jobs ??= new JiggleJobs(fixedTime, fixedDeltaTime);
        jobs.SetFixedDeltaTime(fixedDeltaTime);
        GetJiggleTrees();
        _globalDirty = false;
        return jobs;
    }

    public static void GetJiggleTrees() {
        Profiler.BeginSample("JiggleRoot.GetJiggleTrees");
        // TODO: Cleanup previous trees, or reuse them.
        foreach (var rootJiggleTreeSegment in rootJiggleTreeSegments) {
            var currentTree = rootJiggleTreeSegment.jiggleTree;
            if (currentTree is { dirty: false }) {
                continue;
            }
            rootJiggleTreeSegment.RegenerateJiggleTreeIfNeeded();
            jobs.ScheduleAdd(rootJiggleTreeSegment.jiggleTree);
        }
        Profiler.EndSample();
    }

    public static JiggleTree CreateJiggleTree(JiggleRigData jiggleRig, JiggleTree tree) {
        Profiler.BeginSample("JiggleTreeUtility.CreateJiggleTree");
        tempTransforms.Clear();
        tempPoints.Clear();
        tempParameters.Clear();
        jiggleRig.GetJiggleColliders(tempColliders);
        jiggleRig.GetJiggleColliderTransforms(tempColliderTransforms);
        if (!jiggleRig.GetCacheIsValid()) jiggleRig.BuildNormalizedDistanceFromRootList();
        var backProjection = Vector3.zero;
        if (jiggleRig.rootBone.childCount != 0) {
            var pos = jiggleRig.rootBone.position;
            var childPos = jiggleRig.rootBone.GetChild(0).position;
            var diff = pos - childPos;
            backProjection = pos + diff;
        } else {
            backProjection = jiggleRig.rootBone.position + jiggleRig.rootBone.up * 0.25f;
        }
        var lossyScaleSample = jiggleRig.rootBone.lossyScale;
        var lossyScale = (lossyScaleSample.x + lossyScaleSample.y + lossyScaleSample.z)/3f;
        var cache = jiggleRig.GetCache(jiggleRig.rootBone);
        var cachedScale = cache.lossyScale;
        tempPoints.Add(new JiggleSimulatedPoint() { // Back projected virtual root
            position = backProjection,
            lastPosition = backProjection,
            childrenCount = 0,
            parentIndex = -1,
            hasTransform = false,
            animated = false,
        });
        tempParameters.Add(jiggleRig.GetJiggleBoneParameter(0f, cachedScale, lossyScale));
        tempTransforms.Add(jiggleRig.rootBone);
        Visit(jiggleRig.rootBone, tempTransforms, tempPoints, tempParameters, 0, jiggleRig, backProjection, 0f, out int childIndex);
        if (childIndex != -1) {
            var rootPoint = tempPoints[0];
            AddChildToPoint(ref rootPoint, childIndex);
            tempPoints[0] = rootPoint;
        }

        Profiler.EndSample();
        if (tree != null) {
            tree.Set(tempTransforms, tempPoints, tempParameters, tempColliderTransforms, tempColliders);
            return tree;
        } else {
            return new JiggleTree(tempTransforms, tempPoints, tempParameters, tempColliderTransforms, tempColliders);
        }
    }

    public static void VisitForLength(Transform t, JiggleRigData rig, Vector3 lastPosition, float currentLength, out float totalLength) {
        if (rig.GetIsExcluded(t)) {
            totalLength = Mathf.Max(currentLength, 0.001f);
            return;
        }
        currentLength += Vector3.Distance(lastPosition, t.position);
        totalLength = Mathf.Max(currentLength, 0.001f);
        var validChildrenCount = rig.GetValidChildrenCount(t);
        for (int i = 0; i < validChildrenCount; i++) {
            var child = rig.GetValidChild(t, i);
            VisitForLength(child, rig, t.position, currentLength, out var siblingMaxLength);
            totalLength = Mathf.Max(totalLength, siblingMaxLength);
        }
    }

    private static void Visit(Transform t, List<Transform> transforms, List<JiggleSimulatedPoint> points, List<JigglePointParameters> parameters, int parentIndex, JiggleRigData lastJiggleRig, Vector3 lastPosition, float currentLength, out int newIndex) {
        if (Application.isPlaying && GetJiggleTreeSegmentByBone(t, out JiggleTreeSegment currentJiggleTreeSegment)) {
            lastJiggleRig = currentJiggleTreeSegment.jiggleRigData;
        }
        if (!lastJiggleRig.GetIsExcluded(t)) {
            var validChildrenCount = lastJiggleRig.GetValidChildrenCount(t);
            var currentPosition = t.position;
            var lossyScaleSample = t.lossyScale;
            var lossyScale = (lossyScaleSample.x + lossyScaleSample.y + lossyScaleSample.z) / 3f;
            var cache = lastJiggleRig.GetCache(t);
            var cachedLossyScale = cache.lossyScale;
            if (Vector3.Distance(t.position, lastPosition) < MERGE_DISTANCE) {
                if (validChildrenCount > 0) {
                    for (int i = 0; i < validChildrenCount; i++) {
                        var child = lastJiggleRig.GetValidChild(t, i);
                        Visit(child, transforms, points, parameters, parentIndex, lastJiggleRig, lastPosition, currentLength, out int childIndex);
                        if (childIndex != -1) {
                            var record = points[parentIndex];
                            AddChildToPoint(ref record, childIndex);
                            points[parentIndex] = record;
                        }
                    }
                    newIndex = -1;
                } else {
                    transforms.Add(t);
                    points.Add(new JiggleSimulatedPoint() { // virtual projected tip
                        position = currentPosition + (currentPosition - lastPosition),
                        lastPosition = currentPosition + (currentPosition - lastPosition),
                        childrenCount = 0,
                        distanceFromRoot = currentLength,
                        parentIndex = parentIndex,
                        hasTransform = false,
                        animated = false,
                    });
                    parameters.Add(lastJiggleRig.GetJiggleBoneParameter(cache.normalizedDistanceFromRoot, cachedLossyScale, lossyScale));
                    var record = points[parentIndex];
                    AddChildToPoint(ref record, points.Count - 1);
                    points[parentIndex] = record;
                    newIndex = points.Count - 1;
                }
                return;
            }
            transforms.Add(t);
            var parameter = lastJiggleRig.GetJiggleBoneParameter(cache.normalizedDistanceFromRoot, cachedLossyScale, lossyScale);
            if ((lastJiggleRig.excludeRoot && t == lastJiggleRig.rootBone) || lastJiggleRig.GetIsExcluded(t)) {
                parameter = new JigglePointParameters() {
                    angleElasticity = 1f,
                    lengthElasticity = 1f,
                    rootElasticity = 1f,
                    elasticitySoften = 0f
                };
            }

            if (points[parentIndex].hasTransform) {
                currentLength += Vector3.Distance(lastPosition, t.position);
            }
            

            points.Add(new JiggleSimulatedPoint() { // Regular point
                position = currentPosition,
                lastPosition = currentPosition,
                childrenCount = 0,
                distanceFromRoot = currentLength,
                parentIndex = parentIndex,
                hasTransform = true,
                animated = true,
            });
            parameters.Add(parameter);
            newIndex = points.Count - 1;
            
            if (validChildrenCount == 0) {
                transforms.Add(t);
                points.Add(new JiggleSimulatedPoint() { // virtual projected tip
                    position = currentPosition + (currentPosition - lastPosition),
                    lastPosition = currentPosition + (currentPosition - lastPosition),
                    childrenCount = 0,
                    distanceFromRoot = currentLength,
                    parentIndex = newIndex,
                    hasTransform = false,
                    animated = false,
                });
                parameters.Add(lastJiggleRig.GetJiggleBoneParameter(cache.normalizedDistanceFromRoot, cachedLossyScale, lossyScale));
                var record = points[newIndex];
                AddChildToPoint(ref record, points.Count - 1);
                points[newIndex] = record;
            } else {
                for (int i = 0; i < validChildrenCount; i++) {
                    var child = lastJiggleRig.GetValidChild(t, i);
                    Visit(child, transforms, points, parameters, newIndex, lastJiggleRig, currentPosition, currentLength, out int childIndex);
                    if (childIndex != -1) {
                        var record = points[newIndex];
                        AddChildToPoint(ref record, childIndex);
                        points[newIndex] = record;
                    }
                }
            }
        } else {
            newIndex = points.Count - 1;
        }

    }

    public static unsafe void AddChildToPoint(ref JiggleSimulatedPoint point, int childIndex) {
        if (point.childrenCount>=JiggleSimulatedPoint.MAX_CHILDREN) {
            return;
        }
        point.childrenIndices[point.childrenCount] = childIndex;
        point.childrenCount++;
    }
    
    public static void ScheduleRemoveJiggleTree(JiggleTree jiggleTree) {
        jobs?.ScheduleRemove(jiggleTree);
    }
    
    public static void RemoveJiggleTreeSegment(JiggleTreeSegment jiggleTreeSegment) {
        if (rootJiggleTreeSegments.Contains(jiggleTreeSegment)) {
            rootJiggleTreeSegments.Remove(jiggleTreeSegment);
        }

        jiggleRootLookup.Remove(jiggleTreeSegment.transform);
        
        jiggleTreeSegment.SetDirty();
        
        if (jiggleTreeSegment.parent != null) {
            jiggleTreeSegment.parent.SetDirty();
            jiggleTreeSegment.SetParent(null);
        }
        
        SetGlobalDirty();
    }

}

}