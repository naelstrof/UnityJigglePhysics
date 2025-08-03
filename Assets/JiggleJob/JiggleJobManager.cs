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
    
    public static JobHandle handleBulkRead;
    public static bool hasHandleBulkRead;
    
    public static JobHandle handleSimulate;
    public static bool hasHandleSimulate;
    
    public static JobHandle handleTransformWrite;
    public static bool hasHandleTransformWrite;
    
    public static JobHandle handleInterpolate;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        accumulatedTime = 0f;
        time = 0f;
    }

    public static void SchedulePose() {
        var jobs = JiggleRoot.GetJiggleJobs();
        jobs.jobInterpolation.currentTime = Time.timeAsDouble;
        handleInterpolate = jobs.jobInterpolation.ScheduleParallel(jobs.GetTransformCount(), 128, default);
        
        if (hasHandleBulkRead) {
            handleTransformWrite = jobs.jobTransformWrite.Schedule(jobs.transformAccessArray, JobHandle.CombineDependencies(handleInterpolate, handleBulkRead));
        } else {
            handleTransformWrite = jobs.jobTransformWrite.Schedule(jobs.transformAccessArray, handleInterpolate);
        }

        hasHandleTransformWrite = true;
    }
    
    public static void CompletePose() {
        if (hasHandleTransformWrite) {
            handleTransformWrite.Complete();
        }
    }

    private static void Simulate(double currentTime) {
        var gravity = Physics.gravity;
        var jobs = JiggleRoot.GetJiggleJobs();

        if (hasHandleSimulate) {
            handleSimulate.Complete();
            jobs.jobInterpolation.previousTimeStamp = jobs.jobInterpolation.timeStamp;
            jobs.jobInterpolation.timeStamp = jobs.jobSimulate.timeStamp;

            var temp = jobs.jobInterpolation.previousPoses;
            jobs.jobInterpolation.previousPoses = jobs.jobInterpolation.currentPoses;
            jobs.jobInterpolation.currentPoses = jobs.jobSimulate.outputPoses;
            jobs.jobSimulate.outputPoses = temp;
        }
        
        handleBulkRead = jobs.jobBulkTransformRead.ScheduleReadOnly(jobs.transformAccessArray, 128);
        hasHandleBulkRead = true;

        jobs.jobSimulate.gravity = gravity;
        jobs.jobSimulate.timeStamp = currentTime;
        handleSimulate = jobs.jobSimulate.ScheduleParallel(jobs.GetTreeCount(), 1, handleBulkRead);
        hasHandleSimulate = true;
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
