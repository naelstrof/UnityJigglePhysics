using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

public static class JiggleTreeUtility {
    private static List<JiggleTreeSegment> jiggleTreeSegments;
    private static Dictionary<Transform, JiggleTreeSegment> jiggleRootLookup;
    private static bool _globalDirty = false;
    private static JiggleTree[] jiggleTrees;
    private static Transform[] colliderTransforms;
    private static readonly List<Transform> tempTransforms = new List<Transform>();
    private static readonly List<JiggleBoneSimulatedPoint> tempPoints = new List<JiggleBoneSimulatedPoint>();

    public static JiggleJobs jobs;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        jiggleTreeSegments = new List<JiggleTreeSegment>();
        jiggleRootLookup = new Dictionary<Transform, JiggleTreeSegment>();
        jiggleTrees = null;
        colliderTransforms = null;
        _globalDirty = true;
        jobs?.Dispose();
        jobs = new JiggleJobs();
    }
    
    public static void SetGlobalDirty() => _globalDirty = true;

    public static void AddJiggleTreeSegment(JiggleTreeSegment jiggleTreeSegment) {
        jiggleTreeSegments.Add(jiggleTreeSegment);
        jiggleRootLookup.Add(jiggleTreeSegment.transform, jiggleTreeSegment);
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
    
    private static List<JiggleTreeSegment> GetRootJiggleTreeSegments() {
        var rootJiggleTreeSegments = new List<JiggleTreeSegment>();
        foreach (var jiggleTreeSegment in jiggleTreeSegments) {
            var foundParent = GetParentJiggleTreeSegment(jiggleTreeSegment.transform, out var parentJiggleTreeSegment);
            if (foundParent) {
                jiggleTreeSegment.SetParent(parentJiggleTreeSegment);
            } else {
                rootJiggleTreeSegments.Add(jiggleTreeSegment);
            }
        }
        return rootJiggleTreeSegments;
    }

    public static JiggleJobs GetJiggleJobs() {
        if (!_globalDirty) {
            return jobs;
        }
        jobs.Dispose();
        jobs.Set(GetJiggleTrees(), GetColliderTransforms());
        _globalDirty = false;
        return jobs;
    }

    public static JiggleTree[] GetJiggleTrees() {
        Profiler.BeginSample("JiggleRoot.GetJiggleTrees");
        // TODO: Cleanup previous trees, or reuse them.
        var newJiggleTrees = new List<JiggleTree>();
        var rootJiggleTreeSegments = GetRootJiggleTreeSegments();
        foreach (var rootJiggleTreeSegment in rootJiggleTreeSegments) {
            Profiler.BeginSample("JiggleRoot.FindExistingJiggleTree");
            // TODO: CHECK FOR DIRTY MEMBER, USE OLD JIGGLE TREE IF NOT DIRTY
            if (rootJiggleTreeSegment.jiggleTree!=null && !rootJiggleTreeSegment.jiggleTree.dirty) {
                newJiggleTrees.Add(rootJiggleTreeSegment.jiggleTree);
                Profiler.EndSample();
                continue;
            }
            Profiler.EndSample();
            tempTransforms.Clear();
            tempPoints.Clear();
            var backProjection = Vector3.zero;
            if (rootJiggleTreeSegment.transform.childCount != 0) {
                var pos = rootJiggleTreeSegment.transform.position;
                var childPos = rootJiggleTreeSegment.transform.GetChild(0).position;
                var diff = pos - childPos;
                backProjection = pos + diff;
            } else {
                backProjection = rootJiggleTreeSegment.transform.position;
            }
            tempPoints.Add(new JiggleBoneSimulatedPoint() { // Back projected virtual root
                position = backProjection,
                lastPosition = backProjection,
                childenCount = 1,
                parameters = rootJiggleTreeSegment.rig.GetJiggleBoneParameter(0f),
                parentIndex = -1,
                hasTransform = false,
                animated = false,
            });
            tempTransforms.Add(rootJiggleTreeSegment.transform);
            Visit(rootJiggleTreeSegment.transform, tempTransforms, tempPoints, 0, rootJiggleTreeSegment, rootJiggleTreeSegment.transform.position, out int childIndex);
            unsafe {
                var rootPoint = tempPoints[0];
                rootPoint.childrenIndices[0] = childIndex;
                tempPoints[0] = rootPoint;
            }
            var newJiggleTree = new JiggleTree(tempTransforms.ToArray(), tempPoints.ToArray());
            newJiggleTrees.Add(newJiggleTree);
            rootJiggleTreeSegment.SetJiggleTree(newJiggleTree);
        }
        jiggleTrees = newJiggleTrees.ToArray();
        Profiler.EndSample();
        return jiggleTrees;
    }

    private static Transform[] GetColliderTransforms() {
        var colliderTransforms = UnityEngine.Object.FindObjectsOfType<JigglePhysicsCollider>().Select(c => c.transform)
            .ToArray();
        return colliderTransforms;
    }

    private static void Visit(Transform t, List<Transform> transforms, List<JiggleBoneSimulatedPoint> points, int parentIndex, JiggleTreeSegment lastJiggleTreeSegment, Vector3 lastPosition, out int newIndex) {
        if (GetJiggleTreeSegmentByBone(t, out JiggleTreeSegment currentJiggleTreeSegment)) {
            lastJiggleTreeSegment = currentJiggleTreeSegment;
        }
        if (!lastJiggleTreeSegment.rig.CheckExcluded(t)) {
            transforms.Add(t);
            var parameters = lastJiggleTreeSegment.rig.GetJiggleBoneParameter(0.5f);
            if ((lastJiggleTreeSegment.rig.rootExcluded && t == lastJiggleTreeSegment.transform) || lastJiggleTreeSegment.rig.CheckExcluded(t)) {
                parameters = new JiggleBoneParameters() {
                    angleElasticity = 1f,
                    lengthElasticity = 1f,
                    rootElasticity = 1f,
                    elasticitySoften = 0f
                };
            }

            var currentPosition = t.position;

            var validChildren = GetValidChildren(t, lastJiggleTreeSegment.rig);
            points.Add(new JiggleBoneSimulatedPoint() { // Regular point
                position = currentPosition,
                lastPosition = currentPosition,
                childenCount = validChildren.Count == 0 ? 1 : validChildren.Count,
                parameters = parameters,
                parentIndex = parentIndex,
                hasTransform = true,
                animated = true,
            });
            newIndex = points.Count - 1;
            
            if (validChildren.Count == 0) {
                transforms.Add(t);
                points.Add(new JiggleBoneSimulatedPoint() { // virtual projected tip
                    position = currentPosition + (currentPosition - lastPosition),
                    lastPosition = currentPosition + (currentPosition - lastPosition),
                    childenCount = 0,
                    parameters = lastJiggleTreeSegment.rig.GetJiggleBoneParameter(1f),
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
                for (int i = 0; i < validChildren.Count; i++) {
                    var child = validChildren[i];
                    Visit(child, transforms, points, newIndex, lastJiggleTreeSegment, currentPosition, out int childIndex);
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

    private static List<Transform> GetValidChildren(Transform t, JiggleRig jiggleRig) {
        var validChildren = new List<Transform>();
        foreach (Transform child in t) {
            if (jiggleRig.CheckExcluded(child)) continue;
            validChildren.Add(child);
        }
        return validChildren;
    }
    
    public static void RemoveJiggleTreeSegment(JiggleTreeSegment jiggleTreeSegment) {
        jiggleTreeSegment.SetDirty();
        if (jiggleTreeSegments.Contains(jiggleTreeSegment)) {
            jiggleTreeSegments.Remove(jiggleTreeSegment);
            jiggleRootLookup.Remove(jiggleTreeSegment.transform);
        }
        if (jiggleTreeSegment.parent!=null) RemoveJiggleTreeSegment(jiggleTreeSegment.parent);
        if (jiggleTreeSegments.Count == 0) {
            jiggleTrees = null;
        }
    }

}
