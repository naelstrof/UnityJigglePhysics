using System;
using System.Collections.Generic;
using UnityEngine;

public class MonobehaviourHider {

    [DisallowMultipleComponent]
    public class JiggleRoot : MonoBehaviour {
        private static List<JiggleRoot> jiggleRoots;
        private static bool dirty = false;
        private static List<JiggleTree> jiggleTrees;
        public JiggleRig rig;
        
        public static void SetDirty() => dirty = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() {
            jiggleRoots = new List<JiggleRoot>();
            if (jiggleTrees != null) {
                foreach (var tree in jiggleTrees) {
                    //tree.Dispose();
                }
            }
            jiggleTrees = new List<JiggleTree>();
            dirty = true;
        }

        
        private static List<JiggleRoot> GetSuperRoots() {
            List<JiggleRoot> roots = new List<JiggleRoot>();
            List<JiggleRoot> parents = new List<JiggleRoot>();
            foreach (var root in jiggleRoots) {
                root.GetComponentsInParent(false, parents);
                if (parents.Count <= 1) { // No parent or only self
                    roots.Add(root);
                }
            }
            return roots;
        }

        private static void Visit(Transform t, List<Transform> transforms, List<JiggleBoneSimulatedPoint> points, int parentIndex, JiggleRoot lastRoot, out int newIndex) {
            if (t.TryGetComponent(out JiggleRoot newRoot)) {
                lastRoot = newRoot;
            }
            transforms.Add(t);
            var parameters = lastRoot.rig.GetJiggleBoneParameter(0.5f);
            if (t == lastRoot.transform) {
                parameters = new JiggleBoneParameters() {
                    angleElasticity = 1f,
                    lengthElasticity = 1f,
                    rootElasticity = 1f,
                    elasticitySoften = 0f
                };
            }
            points.Add(new JiggleBoneSimulatedPoint() { // Regular point
                position = t.position,
                lastPosition = t.position,
                childenCount = t.childCount == 0 ? 1 : t.childCount,
                parameters = parameters,
                parentIndex = parentIndex,
                transformIndex = transforms.Count-1,
                animated = true,
            });
            newIndex = points.Count - 1;
            
            if (t.childCount == 0) {
                points.Add(new JiggleBoneSimulatedPoint() { // virtual projected tip
                    position = t.position,
                    lastPosition = t.position,
                    childenCount = 0,
                    parameters = lastRoot.rig.GetJiggleBoneParameter(1f),
                    parentIndex = newIndex,
                    transformIndex = -1,
                    animated = false,
                });
            } else {
                for (int i = 0; i < t.childCount; i++) {
                    var child = t.GetChild(i);
                    Visit(child, transforms, points, newIndex, lastRoot, out int childIndex);
                    unsafe { // WEIRD
                        var record = points[newIndex];
                        record.childrenIndices[i] = childIndex;
                        points[newIndex] = record;
                    }
                }
            }
        }
        
        public static List<JiggleTree> GetJiggleTrees() {
            if (!dirty) {
                return jiggleTrees;
            }
            // TODO: Cleanup previous trees, or reuse them.
            //jiggleTrees.Clear();
            var newJiggleTrees = new List<JiggleTree>();
            var superRoots = GetSuperRoots();
            foreach (var superRoot in superRoots) {
                // TODO: CHECK FOR DIRTY MEMBER, USE OLD JIGGLE TREE IF NOT DIRTY
                var found = false;
                foreach (var jiggleTree in jiggleTrees) {
                    if (jiggleTree.bones[0] == superRoot.transform) {
                        Debug.Log("FOUND EXISTING JIGGLE TREE");
                        if (!jiggleTree.dirty) {
                            newJiggleTrees.Add(jiggleTree);
                            found = true;
                            break;
                        }
                    }
                }
                if (found) continue;
                List<Transform> jiggleTreeTransforms = new List<Transform>();
                List<JiggleBoneSimulatedPoint> jiggleTreePoints = new List<JiggleBoneSimulatedPoint>();
                jiggleTreePoints.Add(new JiggleBoneSimulatedPoint() { // Back projected virtual root
                    childenCount = 1,
                    parameters = superRoot.rig.GetJiggleBoneParameter(0f),
                    parentIndex = -1,
                    transformIndex = -1,
                    animated = false,
                });
                Visit(superRoot.transform, jiggleTreeTransforms, jiggleTreePoints, 0, superRoot, out int childIndex);
                unsafe {
                    var rootPoint = jiggleTreePoints[0];
                    rootPoint.childrenIndices[0] = childIndex;
                    jiggleTreePoints[0] = rootPoint;
                }
                newJiggleTrees.Add(new JiggleTree(jiggleTreeTransforms.ToArray(), jiggleTreePoints.ToArray()));
                newJiggleTrees[^1].dirty = false;
            }
            dirty = false;
            jiggleTrees = newJiggleTrees;
            return jiggleTrees;
        }
        private void OnEnable() {
            jiggleRoots.Add(this);
            dirty = true;
        }
        private void OnDisable() {
            jiggleRoots.Remove(this);
            dirty = true;
        }
    }
}
