using Unity.Collections;
using Unity.Jobs;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

// TODO: One IJobParallelForTransform for each jiggle tree so that it represents a single transform root
// NOT an IJobParallelForTransform for each bone
public class JiggleTree {
    public bool dirty;
    public Transform[] bones;
    public JiggleBoneSimulatedPoint[] points;

    public JiggleTree(Transform[] bones, JiggleBoneSimulatedPoint[] points) {
        this.bones = new Transform[bones.Length];
        this.points = new JiggleBoneSimulatedPoint[points.Length];
        for (int i = 0; i < bones.Length; i++) {
            this.bones[i] = bones[i];
        }
        for (int i = 0; i < points.Length; i++) {
            this.points[i] = points[i];
        }
    }
    
}
