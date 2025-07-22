using Unity.Jobs;
using UnityEngine;

public class JiggleTree {
    public Transform[] bones;
    public JiggleJobDoubleBuffer jiggleJob;
    public bool hasJobHandle;
    public JobHandle jobHandle;
}
