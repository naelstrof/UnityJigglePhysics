using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

public static class JiggleTreeUtility {
    private static HashSet<JiggleTreeSegment> jiggleTreeSegments;
    private static Dictionary<Transform, JiggleTreeSegment> jiggleRootLookup;
    private static bool _globalDirty = true;
    private static HashSet<JiggleTree> jiggleTrees;
    private static Transform[] colliderTransforms;
    private static readonly List<Transform> tempTransforms = new List<Transform>();
    private static readonly List<JiggleBoneSimulatedPoint> tempPoints = new List<JiggleBoneSimulatedPoint>();
    private static List<JiggleTreeSegment> rootJiggleTreeSegments;

    public static JiggleJobs jobs;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        jiggleTreeSegments = new HashSet<JiggleTreeSegment>();
        rootJiggleTreeSegments = new List<JiggleTreeSegment>();
        jiggleRootLookup = new Dictionary<Transform, JiggleTreeSegment>();
        jiggleTrees = new HashSet<JiggleTree>();
        colliderTransforms = null;
        _globalDirty = true;
        jobs = new JiggleJobs();
    }

    public static void Dispose() {
        jobs?.Dispose();
        jiggleTreeSegments = new HashSet<JiggleTreeSegment>();
        rootJiggleTreeSegments = new List<JiggleTreeSegment>();
        jiggleRootLookup = new Dictionary<Transform, JiggleTreeSegment>();
        jiggleTrees = new HashSet<JiggleTree>();
        colliderTransforms = null;
        _globalDirty = true;
        jobs = null;
    }
    
    public static void SetGlobalDirty() => _globalDirty = true;

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
    
    public static JiggleJobs GetJiggleJobs() {
        if (!_globalDirty) {
            return jobs;
        }
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

    public static JiggleTree CreateJiggleTree(JiggleRig jiggleRig, JiggleTreeSegment segment) {
        Profiler.BeginSample("JiggleTreeUtility.CreateJiggleTree");
        tempTransforms.Clear();
        tempPoints.Clear();
        VisitForLength(jiggleRig.rootBone, jiggleRig, jiggleRig.rootBone.position, 0f, out var totalLength);
        var backProjection = Vector3.zero;
        if (jiggleRig.rootBone.childCount != 0) {
            var pos = jiggleRig.rootBone.position;
            var childPos = jiggleRig.rootBone.GetChild(0).position;
            var diff = pos - childPos;
            backProjection = pos + diff;
        } else {
            backProjection = jiggleRig.rootBone.position;
        }
        tempPoints.Add(new JiggleBoneSimulatedPoint() { // Back projected virtual root
            position = backProjection,
            lastPosition = backProjection,
            childenCount = 1,
            distanceFromRoot = 0f,
            parameters = jiggleRig.GetJiggleBoneParameter(0f),
            parentIndex = -1,
            hasTransform = false,
            animated = false,
        });
        tempTransforms.Add(jiggleRig.rootBone);
        Visit(jiggleRig.rootBone, tempTransforms, tempPoints, 0, jiggleRig, jiggleRig.rootBone.position, 0f, totalLength, out int childIndex);
        unsafe {
            var rootPoint = tempPoints[0];
            rootPoint.childrenIndices[0] = childIndex;
            tempPoints[0] = rootPoint;
        }
        Profiler.EndSample();
        bool hasSegment = segment != null;
        if (hasSegment && segment.jiggleTree != null) {
            segment.jiggleTree.Set(tempTransforms, tempPoints);
            return segment.jiggleTree;
        } else if (hasSegment) {
            segment.SetJiggleTree(new JiggleTree(tempTransforms, tempPoints));
            return segment.jiggleTree;
        } else {
            return new JiggleTree(tempTransforms, tempPoints);
        }
    }

    // TODO: Make this respect excluded transforms
    private static Transform[] GetColliderTransforms() {
        var colliderTransforms = UnityEngine.Object.FindObjectsOfType<JigglePhysicsCollider>().Select(c => c.transform)
            .ToArray();
        return colliderTransforms;
    }

    private static void VisitForLength(Transform t, JiggleRig rig, Vector3 lastPosition, float currentLength, out float totalLength) {
        if (rig.CheckExcluded(t)) {
            totalLength = currentLength;
            return;
        }
        currentLength += Vector3.Distance(lastPosition, t.position);
        totalLength = currentLength;
        var validChildrenCount = GetValidChildrenCount(t, rig);
        for (int i = 0; i < validChildrenCount; i++) {
            var child = GetValidChild(t, rig, i);
            VisitForLength(child, rig, t.position, currentLength, out var maxLength);
            totalLength = Mathf.Max(totalLength, maxLength);
        }
    }

    private static void Visit(Transform t, List<Transform> transforms, List<JiggleBoneSimulatedPoint> points, int parentIndex, JiggleRig lastJiggleRig, Vector3 lastPosition, float currentLength, float totalLength, out int newIndex) {
        if (Application.isPlaying && GetJiggleTreeSegmentByBone(t, out JiggleTreeSegment currentJiggleTreeSegment)) {
            lastJiggleRig = currentJiggleTreeSegment.rig;
        }
        if (!lastJiggleRig.CheckExcluded(t)) {
            transforms.Add(t);
            var parameters = lastJiggleRig.GetJiggleBoneParameter(currentLength / totalLength);
            if ((lastJiggleRig.rootExcluded && t == lastJiggleRig.rootBone) || lastJiggleRig.CheckExcluded(t)) {
                parameters = new JiggleBoneParameters() {
                    angleElasticity = 1f,
                    lengthElasticity = 1f,
                    rootElasticity = 1f,
                    elasticitySoften = 0f
                };
            }

            if (points[parentIndex].hasTransform) {
                currentLength += Vector3.Distance(lastPosition, t.position);
            }
            
            var currentPosition = t.position;

            var validChildrenCount = GetValidChildrenCount(t, lastJiggleRig);
            points.Add(new JiggleBoneSimulatedPoint() { // Regular point
                position = currentPosition,
                lastPosition = currentPosition,
                childenCount = validChildrenCount == 0 ? 1 : validChildrenCount,
                distanceFromRoot = currentLength,
                parameters = parameters,
                parentIndex = parentIndex,
                hasTransform = true,
                animated = true,
            });
            newIndex = points.Count - 1;
            
            if (validChildrenCount == 0) {
                transforms.Add(t);
                points.Add(new JiggleBoneSimulatedPoint() { // virtual projected tip
                    position = currentPosition + (currentPosition - lastPosition),
                    lastPosition = currentPosition + (currentPosition - lastPosition),
                    childenCount = 0,
                    distanceFromRoot = currentLength,
                    parameters = lastJiggleRig.GetJiggleBoneParameter(currentLength / totalLength),
                    parentIndex = newIndex,
                    hasTransform = false,
                    animated = false,
                });
                unsafe { // WEIRD
                    var record = points[newIndex];
                    record.childrenIndices[0] = points.Count - 1;
                    points[newIndex] = record;
                }
            } else {
                for (int i = 0; i < validChildrenCount; i++) {
                    var child = GetValidChild(t, lastJiggleRig, i);
                    Visit(child, transforms, points, newIndex, lastJiggleRig, currentPosition, currentLength, totalLength, out int childIndex);
                    unsafe { // WEIRD
                        var record = points[newIndex];
                        record.childrenIndices[i] = childIndex;
                        points[newIndex] = record;
                    }
                }
            }
        } else {
            newIndex = points.Count - 1;
        }

    }

    private static int GetValidChildrenCount(Transform t, JiggleRig jiggleRig) {
        int count = 0;
        var childCount = t.childCount;
        for(int i=0;i<childCount;i++) {
            if (jiggleRig.CheckExcluded(t.GetChild(i))) continue;
            count++;
        }
        return count;
    }

    private static Transform GetValidChild(Transform t, JiggleRig jiggleRig, int index) {
        int count = 0;
        var childCount = t.childCount;
        for(int i=0;i<childCount;i++) {
            var child = t.GetChild(i);
            if (jiggleRig.CheckExcluded(child)) continue;
            if (count == index) {
                return child;
            }
            count++;
        }
        return null;
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
