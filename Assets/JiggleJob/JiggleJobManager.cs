using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public static class JiggleJobManager {
    private static double accumulatedTime = 0f;
    private static double time = 0f;
    private static int frame = 0;
    private const double FIXED_DELTA_TIME = 1.0 / 30.0;
    
    private static List<JiggleTree> jiggleTrees;

    public static void AddJiggleTree(Transform[] bones) {
        jiggleTrees.Add(new JiggleTree() {
            bones = bones,
            jiggleJob = new JiggleJobDoubleBuffer() {
                jobA = new JiggleJob() {
                    bones = new Unity.Collections.NativeArray<Matrix4x4>(bones.Length, Unity.Collections.Allocator.Persistent),
                    previousOutput = new Unity.Collections.NativeArray<Matrix4x4>(bones.Length, Unity.Collections.Allocator.Persistent),
                    output = new Unity.Collections.NativeArray<Matrix4x4>(bones.Length, Unity.Collections.Allocator.Persistent),
                },
                jobB = new JiggleJob() {
                    bones = new Unity.Collections.NativeArray<Matrix4x4>(bones.Length, Unity.Collections.Allocator.Persistent),
                    previousOutput = new Unity.Collections.NativeArray<Matrix4x4>(bones.Length, Unity.Collections.Allocator.Persistent),
                    output = new Unity.Collections.NativeArray<Matrix4x4>(bones.Length, Unity.Collections.Allocator.Persistent),
                },
            }
        });
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() {
        accumulatedTime = 0f;
        jiggleTrees = new List<JiggleTree>();
        frame = 0;
    }

    public static void Pose() {
        foreach (var jiggleTree in jiggleTrees) {
            var jiggleJob = jiggleTree.jiggleJob;
            if (!jiggleJob.HasData()) {
                return;
            }
            var job = jiggleJob.previousJob;
            var boneCount = jiggleTree.bones.Length;
            frame++;
            for (int i = 0; i < boneCount; i++) {
                var prevPosition = job.previousOutput[i].GetPosition();
                var prevRotation = job.previousOutput[i].rotation;
            
                var newPosition = job.output[i].GetPosition();
                var newRotation = job.output[i].rotation;

                var diff = job.timeStamp - job.previousTimeStamp;
                if (diff == 0) {
                    throw new UnityException("Time difference is zero, cannot interpolate.");
                    return;
                }

                float timeCorrection = Time.fixedDeltaTime;
                double t = ((Time.timeAsDouble-timeCorrection) - job.previousTimeStamp) / diff;
                var position = Vector3.LerpUnclamped(prevPosition, newPosition, (float)t);
                var rotation = Quaternion.SlerpUnclamped(prevRotation, newRotation, (float)t);
                if (frame % 2 == 0) {
                    Debug.DrawRay(position + Vector3.up*Mathf.Repeat(Time.timeSinceLevelLoad,5f), Vector3.up, Color.magenta, 5f);
                } else {
                    Debug.DrawRay(position + Vector3.up*Mathf.Repeat(Time.timeSinceLevelLoad,5f), Vector3.up, Color.cyan, 5f);
                }

                jiggleTree.bones[i].SetPositionAndRotation(position, rotation);
            }
        }
    }

    private static void Simulate() {
        foreach (var jiggleTree in jiggleTrees) {
            if (jiggleTree.hasJobHandle) {
                jiggleTree.jobHandle.Complete();
            }

            jiggleTree.jiggleJob.Flip();
            var boneCount = jiggleTree.bones.Length;
            for (int i = 0; i < boneCount; i++) {
                jiggleTree.jiggleJob.currentJob.bones[i] = jiggleTree.bones[i].localToWorldMatrix;
            }

            jiggleTree.jiggleJob.currentJob.timeStamp = Time.timeAsDouble;
            jiggleTree.jobHandle = jiggleTree.jiggleJob.currentJob.Schedule();
            jiggleTree.hasJobHandle = true;
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
