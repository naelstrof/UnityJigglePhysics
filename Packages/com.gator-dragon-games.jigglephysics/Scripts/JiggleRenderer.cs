using System;
using System.Runtime.InteropServices;
using GatorDragonGames.JigglePhysics;
using Unity.Mathematics;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {
public static class JiggleRenderer {
    private static JiggleRenderInstancer.GPUChunk[] sphereChunks;
    private static Bounds colliderBounds;
    private static int colliderCount;
    
    private static JiggleRenderInstancer sphereInstancer;
    private static JiggleRenderInstancer planeInstancer;

    public static void OnEnable(JiggleJobs job) {
        sphereInstancer = new JiggleRenderInstancer();
        planeInstancer = new JiggleRenderInstancer();
        job.OnFinishSimulate += FlipData;
    }
    
    private static float4 ColorToFloat4(Color color) {
        return new float4(color.r, color.g, color.b, color.a);
    }
    
    private static void FlipData(JiggleJobs job, double realTime, double simulatedTime) {
        var sceneColliderCapacity = job.GetSceneColliderCapacity();
        var personalColliderCapacity = job.GetSceneColliderCapacity();
        var transformCapacity = job.GetTransformCapcity();
        
        int desiredChunkCount = sceneColliderCapacity + personalColliderCapacity + transformCapacity;
        if (desiredChunkCount == 0) {
            return;
        }

        job.GetColliders(out var personalColliders, out var sceneColliders, out var personalColliderCount, out var sceneColliderCount);
        if (sphereChunks == null || sphereChunks.Length != desiredChunkCount) {
            var newSphereChunks = new JiggleRenderInstancer.GPUChunk[desiredChunkCount];
            if (sphereChunks != null) {
                var oldLength = sphereChunks.Length;
                Array.Copy(sphereChunks, newSphereChunks, oldLength);
            }
            sphereChunks = newSphereChunks;
        }

        colliderCount = personalColliderCount + sceneColliderCount;
        
        Vector3 min = Vector3.one * 10000f;
        Vector3 max = Vector3.one * -10000f;

        for (int i = 0; i < personalColliderCount; i++) {
            var collider = personalColliders[i];
            min = Vector3.Min(min, collider.localToWorldMatrix.c3.xyz - new float3(1f)*collider.worldRadius);
            max = Vector3.Max(max, collider.localToWorldMatrix.c3.xyz + new float3(1f)*collider.worldRadius);
            var matrix = collider.localToWorldMatrix;
            var scaleAdjust = float4x4.Scale(collider.radius*2f);
            JiggleRenderInstancer.GPUChunk chunk = new() {
                matrix = math.mul(matrix,scaleAdjust),
                color = ColorToFloat4(Color.darkOrange)
            };
            sphereChunks[i] = chunk;
        }

        for (int i = 0; i < sceneColliderCount; i++) {
            var collider = sceneColliders[i];
            min = Vector3.Min(min, collider.localToWorldMatrix.c3.xyz - new float3(1f)*collider.worldRadius);
            max = Vector3.Max(max, collider.localToWorldMatrix.c3.xyz + new float3(1f)*collider.worldRadius);
            var matrix = collider.localToWorldMatrix;
            var scaleAdjust = float4x4.Scale(2f*collider.radius);
            JiggleRenderInstancer.GPUChunk chunk = new JiggleRenderInstancer.GPUChunk() {
                matrix = math.mul(matrix, scaleAdjust),
                color = ColorToFloat4(Color.darkRed)
            };
            sphereChunks[i + personalColliderCount] = chunk;
        }
        colliderBounds = new Bounds(Vector3.zero, math.max(math.abs(max), math.abs(min))*2f);
    }

    public static void Render(JiggleJobs job, Material gpuInstanceMaterial, Mesh sphere, double time, float fixedDeltaTime) {
        if (sphereChunks == null) {
            return;
        }

        Vector3 min = colliderBounds.min;
        Vector3 max = colliderBounds.max;
        job.GetResults(out var poses, out var trees, out var poseCount, out var treeCount);
        int currentCount = colliderCount;
        for (int i = 0; i < treeCount; i++) {
            var tree = trees[i];
            for(int o=0;o<tree.pointCount;o++) {
                unsafe {
                    var point = tree.points[o];
                    var pose = poses[o + tree.transformIndexOffset];
                    if (pose.isVirtual) {
                        continue;
                    }
                    var radius = point.worldRadius;
                    JiggleRenderInstancer.GPUChunk chunk = new JiggleRenderInstancer.GPUChunk() {
                        matrix = float4x4.TRS(pose.position, pose.rotation, new float3(1f*radius*2f)),
                        color = ColorToFloat4(Color.lightSkyBlue),
                    };
                    min = Vector3.Min(min, pose.position - new float3(1f)*radius);
                    max = Vector3.Max(max, pose.position + new float3(1f)*radius);
                    sphereChunks[currentCount] = chunk;
                    currentCount++;
                }
            }
        }
        if (currentCount == 0) {
            return;
        }
        var newBounds = new Bounds(Vector3.zero, math.max(math.abs(max), math.abs(min))*2f);
        sphereInstancer.Render(newBounds, sphere, gpuInstanceMaterial, sphereChunks, currentCount);
    }

    public static void Dispose() {
        sphereInstancer?.Dispose();
        sphereInstancer = null;
        planeInstancer?.Dispose();
        planeInstancer = null;
    }
}

}