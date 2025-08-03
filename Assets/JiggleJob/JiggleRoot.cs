using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

public class JiggleJobs {
    private int treeCount;
    private int transformCount;
    
    public JiggleJobBulkTransformRead jobBulkTransformRead;
    public JiggleJobSimulate jobSimulate;
    public JiggleJobInterpolation jobInterpolation;
    public TransformAccessArray transformAccessArray;
    
    public JiggleJobTransformWrite jobTransformWrite;

    public int GetTreeCount() => treeCount;
    public int GetTransformCount() => transformCount;
    public void Dispose() {
        treeCount = 0;
        transformCount = 0;
        jobBulkTransformRead.Dispose();
        jobSimulate.Dispose();
        jobInterpolation.Dispose();
        transformAccessArray.Dispose();
        jobTransformWrite.Dispose();
    }

    public void Set(Transform[] transforms, JiggleTreeStruct[] trees, JiggleTransform[] poses, JiggleTransform[] localPoses) {
        treeCount = trees.Length;
        transformCount = transforms.Length;
        
        jobSimulate.Dispose();
        jobSimulate = new JiggleJobSimulate(trees, poses);
        
        jobInterpolation.Dispose();
        jobInterpolation = new JiggleJobInterpolation(poses);

        jobBulkTransformRead.Dispose();
        jobBulkTransformRead = new JiggleJobBulkTransformRead(jobSimulate, localPoses);
        
        jobTransformWrite = new JiggleJobTransformWrite(jobBulkTransformRead, jobInterpolation);
        
        if (transformAccessArray.isCreated) {
            transformAccessArray.Dispose();
        }
        transformAccessArray = new TransformAccessArray(transforms);
    }
}

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

    public static JiggleJobs jobs;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        jiggleRoots = new List<JiggleRoot>();
        jiggleRootLookup = new Dictionary<Transform, JiggleRoot>();
        jiggleTrees = null;
        rootDirty = true;
        jobs?.Dispose();
        jobs = new JiggleJobs();
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

    private class SuperRootSets {
        public Transform nonJiggleRoot;
        public List<JiggleRoot> superRoots;
    }

    private static List<SuperRootSets> GetSuperRootSets(List<JiggleRoot> superRoots) {
        var superRootSets = new List<SuperRootSets>();
        for (var index = 0; index < superRoots.Count; index++) {
            var setSuperRoots = new List<JiggleRoot>() { superRoots[index] };
            var nonJiggleRoot = FindNonJiggleRoot(setSuperRoots[0]._transform);
            if (nonJiggleRoot == null) {
                superRootSets.Add(new SuperRootSets() {
                    nonJiggleRoot = setSuperRoots[0]._transform,
                    superRoots = setSuperRoots
                });
                superRoots.RemoveAt(index);
                index--;
                continue;
            }
            if (index>=superRoots.Count) continue;
            for (int index2 = index+1; index2 < superRoots.Count; index2++) {
                var additionalSuperRoot = superRoots[index2];
                var nonJiggleRoot2 = FindNonJiggleRoot(additionalSuperRoot._transform);
                if (nonJiggleRoot2 == nonJiggleRoot) {
                    setSuperRoots.Add(additionalSuperRoot);
                    superRoots.RemoveAt(index2);
                    index--;
                    index2--;
                }
            }
            superRootSets.Add(new SuperRootSets() {
                nonJiggleRoot = nonJiggleRoot,
                superRoots = setSuperRoots
            });
        }
        return superRootSets;
    }
    
    private static Transform FindNonJiggleRoot(Transform t) {
        while (t.parent != null) {
            t = t.parent;
            if (t.TryGetComponent<Animator>(out var animator)) {
                return t;
            }
        }
        return null;
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
                hasTransform = true,
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
    
    public static JiggleJobs GetJiggleJobs() {
        if (!rootDirty) {
            return jobs;
        }
        Profiler.BeginSample("JiggleRoot.GetJiggleTrees");
        // TODO: Cleanup previous trees, or reuse them.
        var newJiggleTrees = new List<JiggleTree>();
        var superRoots = GetSuperRoots();
        var superRootSets = GetSuperRootSets(superRoots);
        foreach (var superRootSet in superRootSets) {
            Debug.Log(superRootSet.nonJiggleRoot.gameObject.name);
            foreach (var superRoot in superRootSet.superRoots) {
                Debug.Log(superRoot._transform.gameObject.name);
            }
        }
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
                hasTransform = false,
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
        jiggleTrees = newJiggleTrees.ToArray();
        Profiler.EndSample();
        

        List<JiggleTreeStruct> structs = new List<JiggleTreeStruct>(jiggleTrees.Length);
        List<Transform> jiggledTransforms = new  List<Transform>(jiggleTrees.Length);
        List<JiggleTransform> jiggleTransformPoses = new List<JiggleTransform>(jiggleTrees.Length);
        List<JiggleTransform> jiggleTransformLocalPoses = new List<JiggleTransform>(jiggleTrees.Length);
        foreach(var tree in jiggleTrees) {
            structs.Add(tree.GetStruct(jiggledTransforms, jiggleTransformPoses, jiggleTransformLocalPoses));
        }
        jobs.Set(jiggledTransforms.ToArray(), structs.ToArray(), jiggleTransformPoses.ToArray(), jiggleTransformLocalPoses.ToArray());
        return jobs;
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
