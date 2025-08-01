using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

public class JiggleRoot {
    private static List<JiggleRoot> jiggleRoots;
    private static Dictionary<Transform, JiggleRoot> jiggleRootLookup;
    private static bool rootDirty = false;
    private static JiggleTree[] jiggleTrees;
    private static readonly List<Transform> tempTransforms = new List<Transform>();
    private static readonly List<JiggleBoneSimulatedPoint> tempPoints = new List<JiggleBoneSimulatedPoint>();
    private Transform _transform;
    private JiggleTree _jiggleTree;
    public JiggleRig rig;
    private static JiggleJobBulkReadRoots jobBulkReadRoots;
    private static TransformAccessArray transformAccessArray;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        jiggleRoots = new List<JiggleRoot>();
        jiggleRootLookup = new Dictionary<Transform, JiggleRoot>();
        if (jiggleTrees != null) {
            foreach (var tree in jiggleTrees) {
                tree.Dispose();
            }
        }
        jiggleTrees = null;
        rootDirty = true;
    }

    private static bool GetParentJiggleRoot(Transform t, out JiggleRoot jiggleRoot) {
        var current = t;
        while (current.parent != null) {
            current = current.parent;
            if (jiggleRootLookup.TryGetValue(current, out var root)) {
                jiggleRoot = root;
                return true;
            }
        }
        jiggleRoot = null;
        return false;
    }
    
    private static bool GetJiggleRootByBone(Transform t, out JiggleRoot jiggleRoot) {
        if (jiggleRootLookup.TryGetValue(t, out JiggleRoot root)) {
            jiggleRoot = root;
            return true;
        }
        jiggleRoot = null;
        return false;
    }
    
    private static List<JiggleRoot> GetSuperRoots() {
        List<JiggleRoot> roots = new List<JiggleRoot>();
        List<JiggleRoot> parents = new List<JiggleRoot>();
        foreach (var root in jiggleRoots) {
            if (!GetParentJiggleRoot(root._transform, out var jiggleRoot)) { // No parent or only self
                roots.Add(root);
            }
        }
        return roots;
    }
    
    public static void GetRootPositions(out JobHandle handle, out NativeArray<float3> reads) {
        handle = jobBulkReadRoots.ScheduleReadOnly(transformAccessArray, 256);
        reads = jobBulkReadRoots.outputPositions;
    }
    
    private static void Visit(Transform t, List<Transform> transforms, List<JiggleBoneSimulatedPoint> points, int parentIndex, JiggleRoot lastRoot, out int newIndex) {
        if (GetJiggleRootByBone(t, out JiggleRoot newRoot)) {
            lastRoot = newRoot;
        }
        if (!lastRoot.rig.CheckExcluded(t)) {
            transforms.Add(t);
            var parameters = lastRoot.rig.GetJiggleBoneParameter(0.5f);
            if ((lastRoot.rig.rootExcluded && t == lastRoot._transform) || lastRoot.rig.CheckExcluded(t)) {
                parameters = new JiggleBoneParameters() {
                    angleElasticity = 1f,
                    lengthElasticity = 1f,
                    rootElasticity = 1f,
                    elasticitySoften = 0f
                };
            }

            var validChildren = GetValidChildren(t, lastRoot.rig);
            points.Add(new JiggleBoneSimulatedPoint() { // Regular point
                position = t.position,
                lastPosition = t.position,
                childenCount = validChildren.Count == 0 ? 1 : validChildren.Count,
                parameters = parameters,
                parentIndex = parentIndex,
                transformIndex = transforms.Count-1,
                animated = true,
            });
            newIndex = points.Count - 1;
            
            if (validChildren.Count == 0) {
                points.Add(new JiggleBoneSimulatedPoint() { // virtual projected tip
                    position = t.position,
                    lastPosition = t.position,
                    childenCount = 0,
                    parameters = lastRoot.rig.GetJiggleBoneParameter(1f),
                    parentIndex = newIndex,
                    transformIndex = -1,
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
                    Visit(child, transforms, points, newIndex, lastRoot, out int childIndex);
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
    
    public static JiggleTree[] GetJiggleTrees() {
        if (!rootDirty) {
            return jiggleTrees;
        }
        Profiler.BeginSample("JiggleRoot.GetJiggleTrees");
        // TODO: Cleanup previous trees, or reuse them.
        var newJiggleTrees = new List<JiggleTree>();
        var superRoots = GetSuperRoots();
        foreach (var superRoot in superRoots) {
            Profiler.BeginSample("JiggleRoot.FindExistingJiggleTree");
            // TODO: CHECK FOR DIRTY MEMBER, USE OLD JIGGLE TREE IF NOT DIRTY
            if (superRoot._jiggleTree!=null && !superRoot._jiggleTree.dirty) {
                newJiggleTrees.Add(superRoot._jiggleTree);
                Profiler.EndSample();
                continue;
            }
            Profiler.EndSample();
            tempTransforms.Clear();
            tempPoints.Clear();
            tempPoints.Add(new JiggleBoneSimulatedPoint() { // Back projected virtual root
                childenCount = 1,
                parameters = superRoot.rig.GetJiggleBoneParameter(0f),
                parentIndex = -1,
                transformIndex = -1,
                animated = false,
            });
            Visit(superRoot._transform, tempTransforms, tempPoints, 0, superRoot, out int childIndex);
            unsafe {
                var rootPoint = tempPoints[0];
                rootPoint.childrenIndices[0] = childIndex;
                tempPoints[0] = rootPoint;
            }
            var newJiggleTree = new JiggleTree(tempTransforms.ToArray(), tempPoints.ToArray());
            newJiggleTrees.Add(newJiggleTree);
            newJiggleTree.dirty = false;
            superRoot._jiggleTree = newJiggleTree;
        }
        rootDirty = false;
        if (jiggleTrees != null) {
            foreach (var jiggleTree in jiggleTrees) {
                if (!newJiggleTrees.Contains(jiggleTree)) {
                    jiggleTree.Dispose();
                }
            }
        }

        jiggleTrees = newJiggleTrees.ToArray();
        
        jobBulkReadRoots.Dispose();
        jobBulkReadRoots = new JiggleJobBulkReadRoots(jiggleTrees.Length);
        List<Transform> rootTransforms = new List<Transform>(jiggleTrees.Length);
        foreach (var superRoot in superRoots) {
            rootTransforms.Add(superRoot._transform);
        }
        if (transformAccessArray.isCreated) {
            transformAccessArray.Dispose();
        }
        transformAccessArray = new TransformAccessArray(rootTransforms.ToArray());
        Profiler.EndSample();
        return jiggleTrees;
    }

    public static void RemoveJiggleRoot(JiggleRoot jiggleRoot) {
        SetDirty(jiggleRoot);
        if (jiggleRoots.Contains(jiggleRoot)) {
            jiggleRoots.Remove(jiggleRoot);
            jiggleRootLookup.Remove(jiggleRoot._transform);
            rootDirty = true;
        }
        if (GetParentJiggleRoot(jiggleRoot._transform, out var parentJiggleRoot)) {
            RemoveJiggleRoot(parentJiggleRoot);
        }
        if (jiggleRoots.Count == 0) {
            foreach (var tree in jiggleTrees) {
                tree.Dispose();
            }

            jiggleTrees = null;
        }
    }

    public static void SetDirty(JiggleRoot jiggleRoot) {
        if (jiggleRoots.Contains(jiggleRoot)) {
            if (jiggleRoot._jiggleTree!=null) jiggleRoot._jiggleTree.dirty = true;
            GetParentJiggleRoot(jiggleRoot._transform, out var jiggleParent);
            SetDirty(jiggleParent);
            rootDirty = true;
        }
    }
    
    public JiggleRoot(Transform transform) {
        _transform = transform;
        jiggleRoots.Add(this);
        jiggleRootLookup.Add(_transform, this);
        rootDirty = true;
        SetDirty(this);
    }
    

}
