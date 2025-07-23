using Unity.Jobs;
using UnityEngine;

// TODO: One IJobParallelForTransform for each jiggle tree so that it represents a single transform root
// NOT an IJobParallelForTransform for each bone
public class JiggleTree {
    public Transform[] bones;
    public JiggleBoneParameters[] data;
    
    public JiggleJobDoubleBuffer jiggleJob;
    public bool hasJobHandle;
    public JobHandle jobHandle;

    public JiggleTree(Transform[] bones, JiggleBoneParameters[] data) {
        
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
