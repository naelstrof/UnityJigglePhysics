using Unity.Jobs;
using UnityEngine;

// TODO: One IJobParallelForTransform for each jiggle tree so that it represents a single transform root
// NOT an IJobParallelForTransform for each bone
public class JiggleTree {
    public Transform[] bones;
    public JiggleBoneSimulatedPoint[] points;
    
    public JiggleJobDoubleBuffer jiggleJob;
    public bool hasJobHandle;
    public JobHandle jobHandle;

    public JiggleTree(Transform[] bones, JiggleBoneSimulatedPoint[] points) {
        var boneCount = bones.Length;
        var pointCount = points.Length;
        this.bones = new Transform[boneCount];
        this.points = new JiggleBoneSimulatedPoint[pointCount];
        for (int i = 0; i < pointCount; i++) {
            this.points[i] = points[i];
        }
        for (int i = 0; i < boneCount; i++) {
            this.bones[i] = bones[i];
        }

        jiggleJob = new JiggleJobDoubleBuffer(this.bones, this.points);
    }

    public void Simulate() {
        if (hasJobHandle) {
            jobHandle.Complete();
        }
        jiggleJob.Flip();
        jiggleJob.ReadBones();
        jobHandle = jiggleJob.Schedule();
        hasJobHandle = true;
    }
}
