#ifndef JIGGLE_CHUNKS
#define JIGGLE_CHUNKS

struct JiggleChunk {
    float4x4 chunkMatrix;
    float4 color;
};

StructuredBuffer<JiggleChunk> _JiggleChunks;

void GetJiggleInstance(uint instanceID, out float4x4 Mat, out float4 color) {
    JiggleChunk chunk = _JiggleChunks[instanceID];
    Mat = chunk.chunkMatrix;
    color = chunk.color;
}

void GetJiggleInstance_float(float instanceID, out float4x4 Mat, out float4 color) {
    GetJiggleInstance(instanceID, Mat, color);
}

#endif
