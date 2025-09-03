using System.Runtime.InteropServices;
using GatorDragonGames.JigglePhysics;
using Unity.Mathematics;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {
public static class JiggleRenderer {
    private static MaterialPropertyBlock materialPropertyBlock;
    private static GraphicsBuffer chunkBuffer;
    private static GPUChunk[] chunks;
    private static Bounds bounds;
    private static readonly int JiggleChunks = Shader.PropertyToID("_JiggleChunks");
    private static int bufferCapacity;
    private static int bufferCount;

    private struct GPUChunk {
        public float4x4 matrix;
    }

    private static void GenerateChunks(JiggleJobs job) {
        var bus = job.GetMemoryBus();
        int desiredChunkCount = bus.sceneColliderCapacity + bus.personalColliderCapacity;
        if (desiredChunkCount == 0) {
            return;
        }

        job.GetColliders(out var personalColliders, out var sceneColliders, out var personalColliderCount, out var sceneColliderCount);
        if (chunkBuffer == null || bufferCapacity != desiredChunkCount) {
            chunkBuffer?.Release();
            chunkBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, desiredChunkCount, Marshal.SizeOf<GPUChunk>());
            chunks = new GPUChunk[desiredChunkCount];
            bufferCapacity = desiredChunkCount;
        }

        bufferCount = personalColliderCount + sceneColliderCount;

        Vector3 min = Vector3.one * 10000f;
        Vector3 max = Vector3.one * -10000f;

        for (int i = 0; i < personalColliderCount; i++) {
            min = Vector3.Min(min, personalColliders[i].localToWorldMatrix.c3.xyz - personalColliders[i].worldRadius);
            max = Vector3.Max(max, personalColliders[i].localToWorldMatrix.c3.xyz + personalColliders[i].worldRadius);
            GPUChunk chunk = new GPUChunk() {
                matrix = personalColliders[i].localToWorldMatrix,
            };
            chunks[i] = chunk;
        }

        for (int i = 0; i < sceneColliderCount; i++) {
            min = Vector3.Min(min, sceneColliders[i].localToWorldMatrix.c3.xyz - sceneColliders[i].worldRadius);
            max = Vector3.Max(max, sceneColliders[i].localToWorldMatrix.c3.xyz + sceneColliders[i].worldRadius);
            GPUChunk chunk = new GPUChunk() {
                matrix = sceneColliders[i].localToWorldMatrix,
            };
            chunks[i + personalColliderCount] = chunk;
        }

        bounds = new Bounds(Vector3.zero, max - min);
        chunkBuffer.SetData(chunks);
    }

    public static void Render(JiggleJobs jobs, Material gpuInstanceMaterial, Mesh sphere) {
        GenerateChunks(jobs);
        if (chunkBuffer == null) {
            return;
        }
        materialPropertyBlock ??= new MaterialPropertyBlock();
        materialPropertyBlock.SetBuffer(JiggleChunks, chunkBuffer);
        var renderParams = new RenderParams(gpuInstanceMaterial) {
            worldBounds = bounds,
            matProps = materialPropertyBlock,
        };
        Graphics.RenderMeshPrimitives(renderParams, sphere, 0, bufferCount);
    }

    public static void Dispose() {
        chunkBuffer?.Release();
        chunkBuffer = null;
    }
}

}