using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

public static class JiggleJobManager {
    private static double accumulatedTime = 0f;
    private static double time = 0f;
    public const double FIXED_DELTA_TIME = 1.0 / 30.0;
    public const double FIXED_DELTA_TIME_SQUARED = FIXED_DELTA_TIME * FIXED_DELTA_TIME;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        accumulatedTime = 0f;
        time = 0f;
    }

    public static void SchedulePose() {
        var trees = JiggleRoot.GetJiggleTrees();
        var rootPositions = JiggleRoot.GetRootPositions();
        var currentTime = Time.timeAsDouble;
        for (int i = 0; i < trees.Length; i++) {
            trees[i].SchedulePose(currentTime, rootPositions[i]);
        }
    }
    
    public static void CompletePose() {
        var trees = JiggleRoot.GetJiggleTrees();
        foreach (var jiggleTree in trees) {
            jiggleTree.CompletePose();
        }
    }

    private static void Simulate(double currentTime) {
        var gravity = Physics.gravity;
        var trees = JiggleRoot.GetJiggleTrees();
        foreach (var jiggleTree in trees) {
            jiggleTree.Simulate(currentTime, gravity);
        }
    }
    
    public static void SampleAndStepSimulation(double deltaTime) {
        accumulatedTime += deltaTime;
        if (accumulatedTime < FIXED_DELTA_TIME) {
            return;
        }
        while (accumulatedTime >= FIXED_DELTA_TIME) {
            accumulatedTime -= FIXED_DELTA_TIME;
            time += FIXED_DELTA_TIME;
        }
        Simulate(time);
    }
}
