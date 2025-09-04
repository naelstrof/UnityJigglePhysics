using System.Runtime.InteropServices;
using GatorDragonGames.JigglePhysics;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class JiggleRenderInstancer {
    public struct GPUChunk {
        public float4x4 matrix;
        public float4 color;
    }
    
    private static readonly int JiggleChunks = Shader.PropertyToID("_JiggleChunks");
    private GraphicsBuffer chunkBuffer;
    private int bufferCapacity;
    private int bufferCount;
    private MaterialPropertyBlock materialPropertyBlock;
    
    private void GenerateChunks(NativeArray<GPUChunk> chunks, int count) {
        int desiredChunkCount = chunks.Length;
        if (desiredChunkCount == 0) {
            return;
        }
        if (chunkBuffer == null || bufferCapacity != desiredChunkCount) {
            chunkBuffer?.Release();
            chunkBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, desiredChunkCount, Marshal.SizeOf<GPUChunk>());
            bufferCapacity = desiredChunkCount;
        }
        bufferCount = count;
        chunkBuffer.SetData(chunks, 0, 0, count);
    }
    
    public void Render(Bounds bounds, Mesh mesh, Material material, NativeArray<GPUChunk> chunks, int count) {
        GenerateChunks(chunks, count);
        if (chunkBuffer == null) {
            return;
        }
        materialPropertyBlock ??= new MaterialPropertyBlock();
        materialPropertyBlock.SetBuffer(JiggleChunks, chunkBuffer);
        var renderParams = new RenderParams(material) {
            worldBounds = bounds,
            matProps = materialPropertyBlock,
        };
        Graphics.RenderMeshPrimitives(renderParams, mesh, 0, bufferCount);
    }

    public void Dispose() {
        chunkBuffer?.Release();
    }
}
