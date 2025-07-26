using System;
using System.Collections.Generic;
using UnityEngine;

public class MonobehaviourHider {

    [DisallowMultipleComponent]
    public class JiggleRoot : MonoBehaviour {
        private static List<JiggleRoot> jiggleRoots;
        private static bool dirty = false;
        private static List<JiggleTree> jiggleTrees;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() {
            jiggleRoots = new List<JiggleRoot>();
            jiggleTrees = new List<JiggleTree>();
        }

        private static List<JiggleRoot> GetSuperRoots() {
            List<JiggleRoot> roots = new List<JiggleRoot>();
            if (jiggleRoots.Count == 0) {
                return roots;
            }
            jiggleRoots.Sort(CompareRoots);
            int count = jiggleRoots[0].transform.hierarchyCount;
            foreach (var root in jiggleRoots) {
                if (count != root.transform.hierarchyCount) {
                    break;
                }
                roots.Add(root);
            }
            return roots;
        }

        private static void Visit(Transform t, List<Transform> transforms, List<JiggleBoneSimulatedPoint> points, int parentIndex, JiggleRoot lastRoot) {
            if (t.TryGetComponent(out JiggleRoot newRoot)) {
                lastRoot = newRoot;
            }
            transforms.Add(t);
            points.Add(new JiggleBoneSimulatedPoint() { // Regular point
                childenCount = t.childCount,
                parameters = lastRoot.rig.GetJiggleBoneParameter(0.5f),
                parentIndex = parentIndex,
                transformIndex = transforms.Count-1,
            });
            
            if (t.childCount == 0) {
                points.Add(new JiggleBoneSimulatedPoint() { // virtual projected tip
                    childenCount = 0,
                    parameters = lastRoot.rig.GetJiggleBoneParameter(1f),
                    parentIndex = points.Count-1,
                    transformIndex = -1,
                });
            } else {
                var currentIndex = points.Count - 1;
                for (int i = 0; i < t.childCount; i++) {
                    var child = t.GetChild(i);
                    Visit(child, transforms, points, currentIndex, lastRoot);
                }
            }
        }

        private static List<JiggleTree> GetJiggleTrees() {
            if (!dirty) {
                return jiggleTrees;
            }
            // TODO: Cleanup previous trees, or reuse them.
            jiggleTrees.Clear();
            var superRoots = GetSuperRoots();
            foreach (var superRoot in superRoots) {
                List<Transform> jiggleTreeTransforms = new List<Transform>();
                List<JiggleBoneSimulatedPoint> jiggleTreePoints = new List<JiggleBoneSimulatedPoint>();
                jiggleTreePoints.Add(new JiggleBoneSimulatedPoint() { // Back projected virtual root
                    childenCount = 1,
                    parameters = superRoot.rig.GetJiggleBoneParameter(0f),
                    parentIndex = -1,
                    transformIndex = -1,
                });
                Visit(superRoot.transform, jiggleTreeTransforms, jiggleTreePoints, 0, superRoot);
                jiggleTrees.Add(new JiggleTree(jiggleTreeTransforms.ToArray(), jiggleTreePoints.ToArray()));
            }
            dirty = false;
            return jiggleTrees;
        }

        private static int CompareRoots(JiggleRoot a, JiggleRoot b) {
            return a.transform.hierarchyCount.CompareTo(b.transform.hierarchyCount);
        }
        public JiggleRig rig;
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
