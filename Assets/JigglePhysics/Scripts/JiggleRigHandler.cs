using System.Collections;
using System.Collections.Generic;
using JigglePhysics;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace JigglePhysics {
public static class JiggleRigHandler {
    private static bool initialized = false;
    private static HashSet<JiggleRigBuilder> builders = new HashSet<JiggleRigBuilder>();

    private static void Initialize() {
        if (initialized) {
            return;
        }

        var rootSystem = PlayerLoop.GetCurrentPlayerLoop();
        rootSystem = rootSystem.InjectAt<PostLateUpdate>(
            new PlayerLoopSystem() {
                updateDelegate = UpdateJiggleRigs,
                type = typeof(JiggleRigHandler)
            });
        PlayerLoop.SetPlayerLoop(rootSystem);
        initialized = true;
    }

    private static void UpdateJiggleRigs() {
        CachedSphereCollider.StartPass();
        foreach (var builder in builders) {
            builder.Advance(Time.deltaTime);
        }
        CachedSphereCollider.FinishedPass();
    }

    public static void AddBuilder(JiggleRigBuilder builder) {
        builders.Add(builder);
        Initialize();
    }

    public static void RemoveBuilder(JiggleRigBuilder builder) {
        builders.Remove(builder);
    }

    private static PlayerLoopSystem InjectAt<T>(this PlayerLoopSystem self, PlayerLoopSystem systemToInject) {
        // Have to do this silly index lookup because everything is an immutable struct and must be modified in-place.
        var postLateUpdateSystemIndex = FindIndexOfSubsystem<T>(self.subSystemList);
        if (postLateUpdateSystemIndex == -1) {
            throw new UnityException($"Failed to find PlayerLoopSystem with type{typeof(T)}");
        }
        List<PlayerLoopSystem> postLateUpdateSubsystems = new List<PlayerLoopSystem>(self.subSystemList[postLateUpdateSystemIndex].subSystemList);
        foreach (PlayerLoopSystem loop in postLateUpdateSubsystems) {
            if (loop.type != typeof(JiggleRigBuilder)) continue;
            Debug.LogWarning($"Tried to inject a PlayerLoopSystem ({systemToInject.type}) more than once! Ignoring the second injection.");
            return self; // Already injected!!!
        }
        postLateUpdateSubsystems.Insert(0,
            new PlayerLoopSystem() {
                updateDelegate = UpdateJiggleRigs,
                type = typeof(JiggleRigHandler)
            }
        );
        self.subSystemList[postLateUpdateSystemIndex].subSystemList = postLateUpdateSubsystems.ToArray();
        return self;
    }
    
    private static int FindIndexOfSubsystem<T>(PlayerLoopSystem[] list, int index = -1) {
        if (list == null) return -1;
        for (int i = 0; i < list.Length; i++) {
            if (list[i].type == typeof(T)) {
                return i;
            }
        }
        return -1;
    }
}

}