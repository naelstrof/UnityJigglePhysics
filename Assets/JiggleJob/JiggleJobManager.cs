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
            frame++;
            jiggleTree.Pose();
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
