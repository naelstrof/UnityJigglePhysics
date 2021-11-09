uniform float4 _SoftbodyArray[24];

void JigglePhysicsSoftbody_half (half3 vertexPos, half4 vertexColor, out half3 vertexOffset) {
    vertexOffset = vertexPos;
    for(int i=0;i<8;i++) {
        float4 colorMask = _SoftbodyArray[i*3];
        float4 zonePosRadius = _SoftbodyArray[i*3+1];
        float4 zoneVelocityAmp = _SoftbodyArray[i*3+2];
        if (zoneVelocityAmp.w > 0) {
            half mask = saturate(dot(colorMask,vertexColor));
            half dist = distance(zonePosRadius.xyz, vertexPos.xyz)/zonePosRadius.w;
            half effect = saturate(1-dist*dist)*mask;
            //return zoneVelocityAmp.xyz * effect * zoneVelocityAmp.w;
            vertexOffset += zoneVelocityAmp.xyz * effect * zoneVelocityAmp.w;
        }
    }
}

void JigglePhysicsSoftbody_float (float3 vertexPos, float4 vertexColor, out float3 vertexOffset) {
    vertexOffset = vertexPos;
    for(int i=0;i<8;i++) {
        float4 colorMask = _SoftbodyArray[i*3];
        float4 zonePosRadius = _SoftbodyArray[i*3+1];
        float4 zoneVelocityAmp = _SoftbodyArray[i*3+2];
        if (zoneVelocityAmp.w > 0) {
            float mask = saturate(dot(colorMask,vertexColor));
            float dist = distance(zonePosRadius.xyz, vertexPos.xyz)/zonePosRadius.w;
            float effect = saturate(1-dist*dist)*mask;
            //return zoneVelocityAmp.xyz * effect * zoneVelocityAmp.w;
            vertexOffset += zoneVelocityAmp.xyz * effect * zoneVelocityAmp.w;
        }
    }
}