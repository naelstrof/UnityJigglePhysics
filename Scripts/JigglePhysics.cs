using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace GatorDragonGames.JigglePhysics {

public static class JigglePhysics {
    private static HashSet<JiggleTreeSegment> jiggleTreeSegments;
    private static Dictionary<Transform, JiggleTreeSegment> jiggleRootLookup;
    private static bool _globalDirty = true;
    private static HashSet<JiggleTree> jiggleTrees;
    private static readonly List<Transform> tempTransforms = new List<Transform>();
    private static readonly List<JiggleSimulatedPoint> tempPoints = new List<JiggleSimulatedPoint>();
    private static readonly List<JiggleCollider> tempColliders = new List<JiggleCollider>();
    private static readonly List<Transform> tempColliderTransforms = new List<Transform>();
    private static List<JiggleTreeSegment> rootJiggleTreeSegments;

    private static double lastFixedCurrentTime = 0f;

    private static JiggleJobs jobs;

    public static void ScheduleSimulate(double currentTime, double fixedCurrentTime, float fixedDeltaTime) {
        if (Math.Abs(lastFixedCurrentTime - fixedCurrentTime) < 0.0001f) {
            return;
        }
        
        lastFixedCurrentTime = fixedCurrentTime;

        jobs = GetJiggleJobs(currentTime, fixedDeltaTime);
        jobs.Simulate(fixedCurrentTime, currentTime);
    }

    public static void SchedulePose(double currentTime) {
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
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        jiggleTreeSegments = new HashSet<JiggleTreeSegment>();
        rootJiggleTreeSegments = new List<JiggleTreeSegment>();
        jiggleRootLookup = new Dictionary<Transform, JiggleTreeSegment>();
        jiggleTrees = new HashSet<JiggleTree>();
        _globalDirty = true;
        jobs?.Dispose();
        jobs = new JiggleJobs(Time.timeAsDouble, Time.fixedDeltaTime);
    }

    public static void Dispose() {
        jobs?.Dispose();
        jiggleTreeSegments = new HashSet<JiggleTreeSegment>();
        rootJiggleTreeSegments = new List<JiggleTreeSegment>();
        jiggleRootLookup = new Dictionary<Transform, JiggleTreeSegment>();
        jiggleTrees = new HashSet<JiggleTree>();
        _globalDirty = true;
        jobs = null;
    }
    
    public static void SetGlobalDirty() => _globalDirty = true;

    public static void AddJiggleCollider(JiggleColliderSerializable collider) {
        jobs.Add(collider);
    }

    public static void RemoveJiggleCollider(JiggleColliderSerializable collider) {
        jobs?.Remove(collider);
    }
    
    public static void AddJiggleTreeSegment(JiggleTreeSegment jiggleTreeSegment) {
        jiggleTreeSegments.Add(jiggleTreeSegment);
        if (TryAddRootJiggleTreeSegment(jiggleTreeSegment)) {
            jiggleRootLookup.Add(jiggleTreeSegment.transform, jiggleTreeSegment);
        }
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
    
    private static JiggleJobs GetJiggleJobs(double currentTimeAsDouble, float fixedDeltaTime) {
        if (!_globalDirty) {
            return jobs;
        }
        jobs ??= new JiggleJobs(currentTimeAsDouble, fixedDeltaTime);
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
            if (currentTree != null && jiggleTrees.Contains(currentTree) && !currentTree.dirty) {
                continue;
            }
            if (currentTree == null || currentTree.dirty) {
                CreateJiggleTree(rootJiggleTreeSegment.rig, rootJiggleTreeSegment);
            }
            jiggleTrees.Add(rootJiggleTreeSegment.jiggleTree);
            jobs.Add(rootJiggleTreeSegment.jiggleTree);
        }
        Profiler.EndSample();
    }

    public static JiggleTree CreateJiggleTree(JiggleRigData jiggleRig, JiggleTreeSegment segment) {
        Profiler.BeginSample("JiggleTreeUtility.CreateJiggleTree");
        tempTransforms.Clear();
        tempPoints.Clear();
        jiggleRig.GetJiggleColliders(tempColliders);
        jiggleRig.GetJiggleColliderTransforms(tempColliderTransforms);
        if (!jiggleRig.GetNormalizedDistanceFromRootListIsValid()) jiggleRig.BuildNormalizedDistanceFromRootList();
        var backProjection = Vector3.zero;
        if (jiggleRig.rootBone.childCount != 0) {
            var pos = jiggleRig.rootBone.position;
            var childPos = jiggleRig.rootBone.GetChild(0).position;
            var diff = pos - childPos;
            backProjection = pos + diff;
        } else {
            backProjection = jiggleRig.rootBone.position;
        }
        var lossyScaleSample = jiggleRig.rootBone.lossyScale;
        var lossyScale = (lossyScaleSample.x + lossyScaleSample.y + lossyScaleSample.z)/3f;
        var cachedScale = jiggleRig.GetCachedLossyScale(jiggleRig.rootBone);
        tempPoints.Add(new JiggleSimulatedPoint() { // Back projected virtual root
            position = backProjection,
            lastPosition = backProjection,
            childenCount = 0,
            parameters = jiggleRig.GetJiggleBoneParameter(0f, cachedScale, lossyScale),
            parentIndex = -1,
            hasTransform = false,
            animated = false,
        });
        tempTransforms.Add(jiggleRig.rootBone);
        Visit(jiggleRig.rootBone, tempTransforms, tempPoints, 0, jiggleRig, backProjection, 0f, out int childIndex);
        if (childIndex != -1) {
            unsafe {
                var rootPoint = tempPoints[0];
                rootPoint.childrenIndices[rootPoint.childenCount] = childIndex;
                rootPoint.childenCount++;
                tempPoints[0] = rootPoint;
            }
        }

        Profiler.EndSample();
        bool hasSegment = segment != null;
        if (hasSegment && segment.jiggleTree != null) {
            segment.jiggleTree.Set(tempTransforms, tempPoints, tempColliderTransforms, tempColliders);
            return segment.jiggleTree;
        } else if (hasSegment) {
            segment.SetJiggleTree(new JiggleTree(tempTransforms, tempPoints, tempColliderTransforms, tempColliders));
            return segment.jiggleTree;
        } else {
            return new JiggleTree(tempTransforms, tempPoints, tempColliderTransforms, tempColliders);
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

    private static void Visit(Transform t, List<Transform> transforms, List<JiggleSimulatedPoint> points, int parentIndex, JiggleRigData lastJiggleRig, Vector3 lastPosition, float currentLength, out int newIndex) {
        if (Application.isPlaying && GetJiggleTreeSegmentByBone(t, out JiggleTreeSegment currentJiggleTreeSegment)) {
            lastJiggleRig = currentJiggleTreeSegment.rig;
        }
        if (!lastJiggleRig.GetIsExcluded(t)) {
            var validChildrenCount = lastJiggleRig.GetValidChildrenCount(t);
            var currentPosition = t.position;
            var lossyScaleSample = t.lossyScale;
            var lossyScale = (lossyScaleSample.x + lossyScaleSample.y + lossyScaleSample.z) / 3f;
            var cachedLossyScale = lastJiggleRig.GetCachedLossyScale(t);
            const float MERGE_DISTANCE = 0.001f;
            if (Vector3.Distance(t.position, lastPosition) < MERGE_DISTANCE) {
                if (validChildrenCount > 0) {
                    for (int i = 0; i < validChildrenCount; i++) {
                        var child = lastJiggleRig.GetValidChild(t, i);
                        Visit(child, transforms, points, parentIndex, lastJiggleRig, lastPosition, currentLength, out int childIndex);
                        if (childIndex != -1) {
                            unsafe {
                                // WEIRD
                                var record = points[parentIndex];
                                record.childrenIndices[record.childenCount] = childIndex;
                                record.childenCount++;
                                points[parentIndex] = record;
                            }
                        }
                    }
                    newIndex = -1;
                } else {
                    transforms.Add(t);
                    points.Add(new JiggleSimulatedPoint() { // virtual projected tip
                        position = currentPosition + (currentPosition - lastPosition),
                        lastPosition = currentPosition + (currentPosition - lastPosition),
                        childenCount = 0,
                        distanceFromRoot = currentLength,
                        parameters = lastJiggleRig.GetJiggleBoneParameter(lastJiggleRig.GetNormalizedDistanceFromRoot(t), cachedLossyScale, lossyScale),
                        parentIndex = parentIndex,
                        hasTransform = false,
                        animated = false,
                    });
                    unsafe { // WEIRD
                        var record = points[parentIndex];
                        record.childrenIndices[record.childenCount] = points.Count - 1;
                        record.childenCount++;
                        points[parentIndex] = record;
                    }
                    newIndex = points.Count - 1;
                }
                return;
            }
            transforms.Add(t);
            var parameters = lastJiggleRig.GetJiggleBoneParameter(lastJiggleRig.GetNormalizedDistanceFromRoot(t), cachedLossyScale, lossyScale);
            if ((lastJiggleRig.excludeRoot && t == lastJiggleRig.rootBone) || lastJiggleRig.GetIsExcluded(t)) {
                parameters = new JigglePointParameters() {
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
                childenCount = 0,
                distanceFromRoot = currentLength,
                parameters = parameters,
                parentIndex = parentIndex,
                hasTransform = true,
                animated = true,
            });
            newIndex = points.Count - 1;
            
            if (validChildrenCount == 0) {
                transforms.Add(t);
                points.Add(new JiggleSimulatedPoint() { // virtual projected tip
                    position = currentPosition + (currentPosition - lastPosition),
                    lastPosition = currentPosition + (currentPosition - lastPosition),
                    childenCount = 0,
                    distanceFromRoot = currentLength,
                    parameters = lastJiggleRig.GetJiggleBoneParameter(lastJiggleRig.GetNormalizedDistanceFromRoot(t), cachedLossyScale, lossyScale),
                    parentIndex = newIndex,
                    hasTransform = false,
                    animated = false,
                });
                unsafe { // WEIRD
                    var record = points[newIndex];
                    record.childrenIndices[record.childenCount] = points.Count - 1;
                    record.childenCount++;
                    points[newIndex] = record;
                }
            } else {
                for (int i = 0; i < validChildrenCount; i++) {
                    var child = lastJiggleRig.GetValidChild(t, i);
                    Visit(child, transforms, points, newIndex, lastJiggleRig, currentPosition, currentLength, out int childIndex);
                    if (childIndex != -1) {
                        unsafe {
                            // WEIRD
                            var record = points[newIndex];
                            record.childrenIndices[record.childenCount] = childIndex;
                            record.childenCount++;
                            points[newIndex] = record;
                        }
                    }
                }
            }
        } else {
            newIndex = points.Count - 1;
        }

    }
    
    public static void RemoveJiggleTreeSegment(JiggleTreeSegment jiggleTreeSegment) {
        if (rootJiggleTreeSegments.Contains(jiggleTreeSegment)) {
            rootJiggleTreeSegments.Remove(jiggleTreeSegment);
        }

        jiggleRootLookup.Remove(jiggleTreeSegment.transform);

        if (jiggleTreeSegments.Contains(jiggleTreeSegment)) {
            jiggleTreeSegments.Remove(jiggleTreeSegment);
        }
        
        if (jiggleTreeSegment.jiggleTree != null) {
            if (jiggleTrees.Contains(jiggleTreeSegment.jiggleTree)) {
                jiggleTrees.Remove(jiggleTreeSegment.jiggleTree);
                jobs.Remove(jiggleTreeSegment.jiggleTree);
            }
            jiggleTreeSegment.jiggleTree.Dispose();
        }

        if (jiggleTreeSegment.parent != null) {
            jiggleTreeSegment.parent.SetDirty();
            jiggleTreeSegment.SetParent(null);
        }
        
        SetGlobalDirty();
    }

}

}