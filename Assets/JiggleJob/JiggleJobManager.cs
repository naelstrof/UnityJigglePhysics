using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public static class JiggleJobManager {
    private static double accumulatedTime = 0f;
    private static double time = 0f;
    private static int frame = 0;
    public const double FIXED_DELTA_TIME = 1.0 / 30.0;
    public const double FIXED_DELTA_TIME_SQUARED = FIXED_DELTA_TIME * FIXED_DELTA_TIME;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        accumulatedTime = 0f;
        frame = 0;
    }

    public static void Pose() {
        var trees = MonobehaviourHider.JiggleRoot.GetJiggleTrees();
        foreach (var jiggleTree in trees) {
            frame++;
            jiggleTree.Pose();
        }
    }

    private static void Simulate() {
        var trees = MonobehaviourHider.JiggleRoot.GetJiggleTrees();
        foreach (var jiggleTree in trees) {
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
