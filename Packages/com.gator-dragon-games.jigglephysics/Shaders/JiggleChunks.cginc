#ifndef JIGGLE_CHUNKS
#define JIGGLE_CHUNKS

struct JiggleChunk {
    float4x4 chunkMatrix;
};

StructuredBuffer<JiggleChunk> _JiggleChunks;

void GetJiggleInstance(uint instanceID, out float4x4 Mat) {
    Mat = _JiggleChunks[instanceID].chunkMatrix;
}

void GetJiggleInstance_float(float instanceID, out float4x4 Mat) {
    GetJiggleInstance(instanceID, Mat);
}

#endif
