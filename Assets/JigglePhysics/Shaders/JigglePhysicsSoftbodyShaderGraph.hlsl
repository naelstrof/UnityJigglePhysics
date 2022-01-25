uniform float4 _JiggleInfos[16];


void JigglePhysicsSoftbody_half (half3 vertexPosition, half4 vertexColor, out half3 vertexOffset) {
    vertexOffset = float3(0,0,0);
    for(int i=0;i<8;i++) {
        float4 targetPosePositionRadius = _JiggleInfos[i*2];
        float4 verletPositionBlend = _JiggleInfos[i*2+1];

        half3 movement = (verletPositionBlend.xyz - targetPosePositionRadius.xyz);
        half dist = distance(vertexPosition, targetPosePositionRadius.xyz);
        half multi = 1-smoothstep(0,targetPosePositionRadius.w,dist);
        vertexOffset += movement * multi * verletPositionBlend.w * max(max(vertexColor.r, vertexColor.g), vertexColor.b);
    }
}

void JigglePhysicsSoftbody_float (float3 vertexPosition, float4 vertexColor, out float3 vertexOffset) {
    vertexOffset = float3(0,0,0);
    for(int i=0;i<8;i++) {
        float4 targetPosePositionRadius = _JiggleInfos[i*2];
        float4 verletPositionBlend = _JiggleInfos[i*2+1];

        float3 movement = (verletPositionBlend.xyz - targetPosePositionRadius.xyz);
        float dist = distance(vertexPosition, targetPosePositionRadius.xyz);
        float multi = 1-smoothstep(0,targetPosePositionRadius.w,dist);
        vertexOffset += movement * multi * verletPositionBlend.w * max(max(vertexColor.r, vertexColor.g), vertexColor.b);
    }
}