using Unity.Jobs;
using UnityEngine;

// TODO: One IJobParallelForTransform for each jiggle tree so that it represents a single transform root
// NOT an IJobParallelForTransform for each bone
public class JiggleTree {
    public Transform[] bones;
    public JiggleJobDoubleBuffer jiggleJob;
    public bool hasJobHandle;
    public JobHandle jobHandle;

    public void Simulate() {
        if (hasJobHandle) {
            jobHandle.Complete();
        }
        jiggleJob.Flip();
        jiggleJob.SetBones(bones);
        jobHandle = jiggleJob.Schedule();
        hasJobHandle = true;
    }
}
