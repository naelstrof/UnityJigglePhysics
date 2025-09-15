using GatorDragonGames.JigglePhysics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine; 

namespace GatorDragonGames.JigglePhysics {
[BurstCompile]
public struct JiggleJobPrepareRender : IJob {
    [NativeDisableParallelForRestriction,ReadOnly]
    public NativeArray<JiggleCollider> personalColliders;
    [NativeDisableParallelForRestriction,ReadOnly]
    public NativeArray<JiggleCollider> sceneColliders;
    [NativeDisableParallelForRestriction,ReadOnly]
    public NativeArray<JiggleTransform> outputPoses;
    [NativeDisableParallelForRestriction,ReadOnly]
    public NativeArray<JiggleTreeJobData> trees;

    public int sceneColliderCount;
    public int personalColliderCount;
    public int transformCount;
    public int treeCount;
    
    public NativeArray<JiggleRenderInstancer.GPUChunk> sphereChunks;
    public NativeReference<Bounds> sphereBounds;
    public NativeReference<int> sphereCount;
    
    public void Execute() {
        float3 min = Vector3.one * 10000f;
        float3 max = Vector3.one * -10000f;

        for (int i = 0; i < personalColliderCount; i++) {
            var collider = personalColliders[i];
            if (collider.type != JiggleCollider.JiggleColliderType.Sphere) {
                continue;
            }
            min = math.min(min, collider.localToWorldMatrix.c3.xyz - new float3(1f)*collider.worldRadius);
            max = math.max(max, collider.localToWorldMatrix.c3.xyz + new float3(1f)*collider.worldRadius);
            var matrix = collider.localToWorldMatrix;
            var scaleAdjust = float4x4.Scale(collider.radius*2f);
            JiggleRenderInstancer.GPUChunk chunk = new() {
                matrix = math.mul(matrix,scaleAdjust),
                color = new float4(1f, 0.5490196f, 0f, 1f)
            };
            sphereChunks[i] = chunk;
        }

        for (int i = 0; i < sceneColliderCount; i++) {
            var collider = sceneColliders[i];
            if (collider.type != JiggleCollider.JiggleColliderType.Sphere) {
                continue;
            }
            min = math.min(min, collider.localToWorldMatrix.c3.xyz - new float3(1f)*collider.worldRadius);
            max = math.max(max, collider.localToWorldMatrix.c3.xyz + new float3(1f)*collider.worldRadius);
            var matrix = collider.localToWorldMatrix;
            var scaleAdjust = float4x4.Scale(2f*collider.radius);
            var chunk = new JiggleRenderInstancer.GPUChunk() {
                matrix = math.mul(matrix, scaleAdjust),
                color = new float4(0.5450981f, 0f, 0f, 1f)
            };
            sphereChunks[i + personalColliderCount] = chunk;
        }

        int currentCount = personalColliderCount + sceneColliderCount;
        for (var i = 0; i < treeCount; i++) {
            var tree = trees[i];
            for(var o=0;o<tree.pointCount;o++) {
                unsafe {
                    var point = tree.points[o];
                    var pose = outputPoses[o + (int)tree.transformIndexOffset];
                    if (pose.isVirtual) {
                        continue;
                    }
                    var radius = point.worldRadius;
                    JiggleRenderInstancer.GPUChunk chunk = new JiggleRenderInstancer.GPUChunk() {
                        matrix = float4x4.TRS(pose.position, pose.rotation, new float3(1f*radius*2f)),
                        color = new float4(0.5294118f, 0.8078432f, 0.9803922f, 1f),
                    };
                    min = math.min(min, pose.position - new float3(1f)*radius);
                    max = math.max(max, pose.position + new float3(1f)*radius);
                    sphereChunks[currentCount] = chunk;
                    currentCount++;
                }
            }
        }

        sphereCount.Value = currentCount;
        sphereBounds.Value = new Bounds(Vector3.zero, math.max(math.abs(max), math.abs(min))*2f);
    }

    public void Dispose() {
        if (sphereCount.IsCreated) {
            sphereCount.Dispose();
        }

        if (sphereBounds.IsCreated) {
            sphereBounds.Dispose();
        }
    }
}
}
