using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

public static class JiggleJobManager {
    private static double accumulatedTime = 0f;
    private static double time = 0f;
    public const double FIXED_DELTA_TIME = 1.0 / 30.0;
    public const double FIXED_DELTA_TIME_SQUARED = FIXED_DELTA_TIME * FIXED_DELTA_TIME;

    private static JiggleJobs jobs;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        accumulatedTime = 0f;
        time = 0f;
    }
    
    public static void ScheduleUpdate(double deltaTime) {
        accumulatedTime += deltaTime;
        if (accumulatedTime < FIXED_DELTA_TIME) {
            jobs?.SchedulePoses(default);
            return;
        }
        while (accumulatedTime >= FIXED_DELTA_TIME) {
            accumulatedTime -= FIXED_DELTA_TIME;
            time += FIXED_DELTA_TIME;
        }
        jobs = JiggleTreeUtility.GetJiggleJobs();
        jobs.Simulate(time);
    }

    public static void CompleteUpdate() {
        jobs?.CompletePoses();
    }

    public static void OnDrawGizmos() {
        if (!Application.isPlaying) {
            return;
        }
        JiggleTreeUtility.GetJiggleJobs().OnDrawGizmos();
    }

}
