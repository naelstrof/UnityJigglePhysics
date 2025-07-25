using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public static class JiggleJobManager {
    private static double accumulatedTime = 0f;
    private static double time = 0f;
    private static int frame = 0;
    public const double FIXED_DELTA_TIME = 1.0 / 15.0;
    public const double FIXED_DELTA_TIME_SQUARED = FIXED_DELTA_TIME * FIXED_DELTA_TIME;
    
    private static List<JiggleTree> jiggleTrees;

    public static void AddJiggleTree(JiggleTree tree) {
        jiggleTrees.Add(tree);
        //jiggleTrees.Add(new JiggleTree() {
            //bones = bones,
            //jiggleJob = new JiggleJobDoubleBuffer(bones)
        //});
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        accumulatedTime = 0f;
        jiggleTrees = new List<JiggleTree>();
        frame = 0;
    }

    public static void Pose() {
        foreach (var jiggleTree in jiggleTrees) {
            var boneCount = jiggleTree.bones.Length;
            frame++;
            for (int i = 0; i < boneCount; i++) {
                var prevPosition = jiggleTree.previousSolve[i].GetPosition();
                var prevRotation = jiggleTree.previousSolve[i].rotation;

                var newPosition = jiggleTree.currentSolve[i].GetPosition();
                var newRotation = jiggleTree.currentSolve[i].rotation;

                var diff = jiggleTree.timeStamp - jiggleTree.previousTimeStamp;
                if (diff == 0) {
                    throw new UnityException("Time difference is zero, cannot interpolate.");
                    return;
                }

                // TODO: Revisit this issue after FEELING the solve in VR in context
                // The issue here is that we are having to operate 3 full frames in the past
                // which might be noticable latency
                var timeCorrection = FIXED_DELTA_TIME * 2f;
                double t = ((Time.timeAsDouble-timeCorrection) - jiggleTree.previousTimeStamp) / diff;
                var position = Vector3.LerpUnclamped(prevPosition, newPosition, (float)t);
                var rotation = Quaternion.SlerpUnclamped(prevRotation, newRotation, (float)t);
                //Debug.DrawRay(position + Vector3.up*Mathf.Repeat(Time.timeSinceLevelLoad,5f), Vector3.up, Color.magenta, 5f);

                jiggleTree.bones[i].SetPositionAndRotation(position, rotation);
            }
        }
    }

    private static void Simulate() {
        foreach (var jiggleTree in jiggleTrees) {
            jiggleTree.Simulate();
        }
    }
    
    public static void Update(double deltaTime) {
        accumulatedTime += deltaTime;
        if (accumulatedTime < FIXED_DELTA_TIME) {
            return;
        }
        while (accumulatedTime >= FIXED_DELTA_TIME) {
            accumulatedTime -= FIXED_DELTA_TIME;
            time += FIXED_DELTA_TIME;
        }
        Simulate();
    }
}
