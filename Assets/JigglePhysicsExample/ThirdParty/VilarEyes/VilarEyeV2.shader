// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vilar/EyeV2"
{
	Properties
	{
		_Albedo("Albedo", 2D) = "white" {}
		_BumpMap("BumpMap", 2D) = "bump" {}
		_NormalPower("NormalPower", Range( 0 , 1)) = 1
		_EmissionPower("EmissionPower", Range( 0 , 1)) = 0
		_Scelera("Scelera", Color) = (0.6470588,0.6185122,0.6185122,0)
		_ParallaxHeight("ParallaxHeight", 2D) = "white" {}
		_StylizedReflection("StylizedReflection", CUBE) = "black" {}
		_Blood("Blood", Color) = (0.4705882,0.3737024,0.3737024,0)
		_IrisRing("IrisRing", Color) = (1,0,0,1)
		_Specular("Specular", Range( 0 , 1)) = 0
		_Smooth("Smooth", Range( 0 , 1)) = 0
		_Depth("Depth", Range( 0 , 1)) = 0.5236971
		_IrisBlend("IrisBlend", Range( 0 , 0.3)) = 0
		_IrisSize("IrisSize", Range( 0 , 1)) = 0
		_PupilDialationFrequency("PupilDialationFrequency", Range( 0 , 1)) = 0
		_TwitchMagnitude("TwitchMagnitude", Range( 0 , 1)) = 0.1
		_TwitchShiftyness("TwitchShiftyness", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
			float2 uv_texcoord;
			float3 viewDir;
		};

		uniform samplerCUBE _StylizedReflection;
		uniform sampler2D _BumpMap;
		uniform float _TwitchMagnitude;
		uniform float _TwitchShiftyness;
		uniform float _IrisSize;
		uniform float _NormalPower;
		uniform float _IrisBlend;
		uniform sampler2D _Albedo;
		uniform sampler2D _ParallaxHeight;
		uniform float _Depth;
		uniform float _PupilDialationFrequency;
		uniform float4 _Scelera;
		uniform float4 _Blood;
		uniform float4 _IrisRing;
		uniform float _EmissionPower;
		uniform float _Specular;
		uniform float _Smooth;


		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		float2 ParallaxOcclusionDialated600( float3 normalWorld, sampler2D heightMap, float2 uvs, float3 viewWorld, float3 viewDirTan, float parallax, float refPlane, float currentDialation, float irisSize )
		{
			float2 dx = ddx(uvs);
			float2 dy = ddy(uvs);
			float minSamples = 8;
			float maxSamples = 16;
			float3 result = 0;
			int stepIndex = 0;
			int numSteps = ( int )lerp( (float)maxSamples, (float)minSamples, (float)dot( normalWorld, viewWorld ) );
			float layerHeight = 1.0 / numSteps;
			float2 plane = parallax * ( viewDirTan.xy / viewDirTan.z );
			uvs += refPlane * plane;
			float2 deltaTex = -plane * layerHeight;
			float2 prevTexOffset = 0;
			float prevRayZ = 1.0f;
			float prevHeight = 0.0f;
			float2 currTexOffset = deltaTex;
			float currRayZ = 1.0f - layerHeight;
			float currHeight = 0.0f;
			float intersection = 0;
			float2 finalTexOffset = 0;
			float2 dialatedUV = 0;
			float dialatedCenterDist = 0;
			while ( stepIndex < numSteps + 1 )
			{
				dialatedUV = uvs + currTexOffset - float2(0.5,0.5);
				dialatedCenterDist = length(dialatedUV);
				dialatedCenterDist = max(0, currentDialation + dialatedCenterDist * (irisSize - currentDialation) / irisSize);
				dialatedUV = normalize(dialatedUV) * dialatedCenterDist;
				dialatedUV +=  float2(0.5,0.5);
				currHeight = tex2Dgrad( heightMap, dialatedUV, dx, dy ).r;
				if ( currHeight > currRayZ )
				{
					stepIndex = numSteps + 1;
				}
				else
				{
					stepIndex++;
					prevTexOffset = currTexOffset;
					prevRayZ = currRayZ;
					prevHeight = currHeight;
					currTexOffset += deltaTex;
					currRayZ -= layerHeight;
				}
			}
			int sectionSteps = 2;
			int sectionIndex = 0;
			float newZ = 0;
			float newHeight = 0;
			while ( sectionIndex < sectionSteps )
			{
				intersection = ( prevHeight - prevRayZ ) / ( prevHeight - currHeight + currRayZ - prevRayZ );
				finalTexOffset = prevTexOffset + intersection * deltaTex;
				dialatedUV = uvs + finalTexOffset - float2(0.5,0.5);
				dialatedCenterDist = length(dialatedUV);
				dialatedCenterDist = max(0, currentDialation + dialatedCenterDist * (irisSize - currentDialation) / irisSize);
				dialatedUV = normalize(dialatedUV) * dialatedCenterDist;
				dialatedUV +=  float2(0.5,0.5);
				newZ = prevRayZ - intersection * layerHeight;
				newHeight = tex2Dgrad( heightMap, dialatedUV, dx, dy ).r;
				if ( newHeight > newZ )
				{
					currTexOffset = finalTexOffset;
					currHeight = newHeight;
					currRayZ = newZ;
					deltaTex = intersection * deltaTex;
					layerHeight = intersection * layerHeight;
				}
				else
				{
					prevTexOffset = finalTexOffset;
					prevHeight = newHeight;
					prevRayZ = newZ;
					deltaTex = ( 1 - intersection ) * deltaTex;
					layerHeight = ( 1 - intersection ) * layerHeight;
				}
				sectionIndex++;
			}
			return dialatedUV;
		}


		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			o.Normal = float3(0,0,1);
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float mulTime398 = _Time.y * 0.8;
			float temp_output_410_0 = round( mulTime398 );
			float2 temp_cast_3 = (temp_output_410_0).xx;
			float simplePerlin2D389 = snoise( temp_cast_3 );
			float2 temp_cast_4 = (( temp_output_410_0 + 123.234 )).xx;
			float simplePerlin2D403 = snoise( temp_cast_4 );
			float3 appendResult395 = (float3(( -0.5 + simplePerlin2D389 ) , ( -0.5 + simplePerlin2D403 ) , 0.0));
			float temp_output_418_0 = round( ( mulTime398 + 0.5 ) );
			float2 temp_cast_5 = (temp_output_418_0).xx;
			float simplePerlin2D425 = snoise( temp_cast_5 );
			float2 temp_cast_6 = (( temp_output_418_0 + 123.234 )).xx;
			float simplePerlin2D426 = snoise( temp_cast_6 );
			float3 appendResult424 = (float3(( -0.5 + simplePerlin2D425 ) , ( -0.5 + simplePerlin2D426 ) , 0.0));
			float3 lerpResult429 = lerp( appendResult395 , appendResult424 , saturate( ( fmod( mulTime398 , 1.0 ) * 14.0 ) ));
			float mulTime445 = _Time.y * ( 0.2 + ( 0.3 * _TwitchShiftyness ) );
			float2 temp_cast_7 = (mulTime445).xx;
			float simplePerlin2D447 = snoise( temp_cast_7 );
			float3 lerpResult453 = lerp( lerpResult429 , float3(0,0,0) , saturate( ( ( simplePerlin2D447 + -( _TwitchShiftyness + -0.5 ) ) * 14.0 ) ));
			float3 temp_output_411_0 = ( _TwitchMagnitude * lerpResult453 );
			float2 temp_output_5_0_g8 = ( ( ( float3( frac( i.uv_texcoord ) ,  0.0 ) + temp_output_411_0 ).xy + float2( 0,0 ) ) + float2( -0.5,-0.5 ) );
			float2 temp_output_6_0_g8 = ( temp_output_5_0_g8 * ( 1.0 / _IrisSize ) );
			float2 temp_output_650_29 = saturate( ( temp_output_6_0_g8 + float2( 0.5,0.5 ) ) );
			float smoothstepResult12_g8 = smoothstep( ( 0.45 - _IrisBlend ) , 0.45 , length( temp_output_6_0_g8 ));
			float temp_output_650_30 = smoothstepResult12_g8;
			float3 lerpResult646 = lerp( UnpackScaleNormal( tex2D( _BumpMap, ( float3( temp_output_650_29 ,  0.0 ) + temp_output_411_0 ).xy ), _NormalPower ) , float3(0,0,1) , temp_output_650_30);
			#if defined(LIGHTMAP_ON) && ( UNITY_VERSION < 560 || ( defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) ) )//aselc
			float4 ase_lightColor = 0;
			#else //aselc
			float4 ase_lightColor = _LightColor0;
			#endif //aselc
			float3 normalWorld600 = float3( 0,0,0 );
			sampler2D heightMap600 = _ParallaxHeight;
			float2 uvs600 = temp_output_650_29;
			float3 viewWorld600 = ase_worldViewDir;
			float3 viewDirTan600 = i.viewDir;
			float parallax600 = _Depth;
			float refPlane600 = 0.0;
			float mulTime578 = _Time.y * ( 0.3 * _PupilDialationFrequency );
			float2 temp_cast_14 = (mulTime578).xx;
			float simplePerlin2D580 = snoise( temp_cast_14 );
			float temp_output_602_0 = (0.0 + (simplePerlin2D580 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0));
			float smoothstepResult605 = smoothstep( 0.5 , ( 0.5 + 0.02 ) , distance( temp_output_650_29 , float2( 0.5,0.5 ) ));
			float lerpResult603 = lerp( ( -0.5 * ( 0.5 * temp_output_602_0 ) ) , 0.0 , smoothstepResult605);
			float currentDialation600 = lerpResult603;
			float irisSize600 = 0.5;
			float2 localParallaxOcclusionDialated600 = ParallaxOcclusionDialated600( normalWorld600 , heightMap600 , uvs600 , viewWorld600 , viewDirTan600 , parallax600 , refPlane600 , currentDialation600 , irisSize600 );
			float4 tex2DNode2 = tex2D( _Albedo, localParallaxOcclusionDialated600 );
			float smoothstepResult22_g8 = smoothstep( 0.0 , 0.5 , length( temp_output_5_0_g8 ));
			float4 lerpResult636 = lerp( _Scelera , _Blood , smoothstepResult22_g8);
			float4 lerpResult638 = lerp( tex2DNode2 , lerpResult636 , temp_output_650_30);
			float4 lerpResult639 = lerp( lerpResult638 , _IrisRing , ( ( ( -cos( ( smoothstepResult12_g8 * 6.283 ) ) + 1.0 ) * 0.5 ) * _IrisRing.a ));
			o.Albedo = ( ( texCUBElod( _StylizedReflection, float4( ( float3(-1,-1,1) * reflect( mul( unity_WorldToCamera, float4( ase_worldViewDir , 0.0 ) ).xyz , mul( unity_WorldToCamera, float4( (WorldNormalVector( i , lerpResult646 )) , 0.0 ) ).xyz ) ), (float)0) ).r * ase_lightColor * 1.5 ) + lerpResult639 ).rgb;
			float4 color641 = IsGammaSpace() ? float4(0,0,0,0) : float4(0,0,0,0);
			float4 lerpResult640 = lerp( ( tex2DNode2 * _EmissionPower ) , color641 , temp_output_650_30);
			o.Emission = lerpResult640.rgb;
			float3 temp_cast_17 = (_Specular).xxx;
			o.Specular = temp_cast_17;
			o.Smoothness = _Smooth;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardSpecular keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.viewDir = IN.tSpace0.xyz * worldViewDir.x + IN.tSpace1.xyz * worldViewDir.y + IN.tSpace2.xyz * worldViewDir.z;
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandardSpecular o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandardSpecular, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18912
7;187;1583;565;7304.525;3671.249;7.579555;True;True
Node;AmplifyShaderEditor.CommentaryNode;496;-5738.991,-2220.465;Inherit;False;2829.95;1395.959;;49;411;412;445;447;453;454;452;429;424;395;443;451;406;436;404;450;422;455;423;421;396;403;433;456;425;420;397;389;426;408;419;434;458;437;417;459;441;418;409;460;410;461;427;462;398;428;457;446;444;Twitch Noise;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;444;-5688.491,-1726.348;Float;False;Constant;_Float20;Float 20;17;0;Create;True;0;0;0;False;0;False;0.8;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;428;-5105.644,-1663.117;Float;False;Constant;_Float17;Float 17;17;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;398;-5488.44,-1720.959;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;457;-5178.437,-1027.651;Float;False;Property;_TwitchShiftyness;TwitchShiftyness;16;0;Create;True;0;0;0;False;0;False;0;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;446;-5046.162,-1124.706;Float;False;Constant;_Float21;Float 21;17;0;Create;True;0;0;0;False;0;False;0.3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;427;-4951.642,-1720.867;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;461;-4850.673,-1082.304;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;462;-4864.673,-1164.307;Float;False;Constant;_Float24;Float 24;17;0;Create;True;0;0;0;False;0;False;0.2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RoundOpNode;410;-4795.925,-1987.664;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;417;-4854.707,-1532.605;Float;False;Constant;_Float10;Float 10;16;0;Create;True;0;0;0;False;0;False;123.234;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;460;-4686.673,-1132.307;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;441;-5024.021,-1357.227;Float;False;Constant;_Float18;Float 18;17;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;409;-4850.832,-1907.062;Float;False;Constant;_Float14;Float 14;16;0;Create;True;0;0;0;False;0;False;123.234;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RoundOpNode;418;-4799.8,-1613.207;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;459;-4589.673,-946.3027;Float;False;Constant;_Float22;Float 22;18;0;Create;True;0;0;0;False;0;False;-0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;408;-4631.082,-1946.252;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;458;-4436.55,-1025.159;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;445;-4556.146,-1127.748;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FmodOpNode;437;-4835.515,-1394.925;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;419;-4634.956,-1571.795;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;434;-4856.555,-1290.302;Float;False;Constant;_Float19;Float 19;17;0;Create;True;0;0;0;False;0;False;14;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;389;-4485.7,-2067.099;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;456;-4307.438,-1029.651;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;447;-4373.762,-1133.375;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;433;-4701.546,-1348.405;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;420;-4452.724,-1773.562;Float;False;Constant;_Float11;Float 11;16;0;Create;True;0;0;0;False;0;False;-0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;403;-4477.73,-1921.346;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;397;-4448.85,-2148.019;Float;False;Constant;_Float9;Float 9;16;0;Create;True;0;0;0;False;0;False;-0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;425;-4489.574,-1692.642;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;426;-4481.604,-1546.887;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;396;-4224.062,-2131.311;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;421;-4243.127,-1507.757;Float;False;Constant;_Float16;Float 16;16;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;455;-4161.167,-1072.065;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;404;-4220.893,-1987.158;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;406;-4239.254,-1882.215;Float;False;Constant;_Float5;Float 5;16;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;436;-4552.051,-1350;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;422;-4227.936,-1756.854;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;423;-4224.767,-1612.7;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;450;-4176.926,-968.7739;Float;False;Constant;_Float23;Float 23;17;0;Create;True;0;0;0;False;0;False;14;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;451;-4021.916,-1026.877;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;443;-3962.402,-1426.74;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;424;-4060.859,-1679.392;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DynamicAppendNode;395;-4056.985,-2053.849;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector3Node;454;-3694.041,-1497.089;Float;False;Constant;_Vector6;Vector 6;12;0;Create;True;0;0;0;False;0;False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SaturateNode;452;-3872.42,-1028.469;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;429;-3692.518,-1913.104;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;453;-3424.751,-1515.666;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;354;-2544.498,-1530.722;Inherit;False;2796.808;1067.613;Comment;24;606;603;574;572;573;605;582;577;576;604;602;580;578;575;581;468;600;277;149;279;29;280;150;632;ComputedUVs;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;412;-3426.62,-1628.825;Float;False;Property;_TwitchMagnitude;TwitchMagnitude;15;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;652;-2568.864,-2574.694;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FractNode;659;-2305.833,-2583.862;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;411;-3098.742,-1583.677;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;581;-2208.292,-1055.723;Float;False;Constant;_Float2;Float 2;18;0;Create;True;0;0;0;False;0;False;0.3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;468;-2493.207,-1004.077;Float;False;Property;_PupilDialationFrequency;PupilDialationFrequency;14;0;Create;True;0;0;0;False;0;False;0;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;610;-2613.717,-2348.637;Float;False;Property;_IrisSize;IrisSize;13;0;Create;True;0;0;0;False;0;False;0;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;616;-2611.575,-2437.144;Float;False;Property;_IrisBlend;IrisBlend;12;0;Create;True;0;0;0;False;0;False;0;0.1;0;0.3;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;575;-2057.91,-1045.866;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;654;-2152.377,-2587.117;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleTimeNode;578;-1921.257,-1045.023;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;650;-1861.564,-2486.867;Inherit;False;GenerateIris;-1;;8;5388df8d18d3b53479b5c1077ed2ed90;0;3;31;FLOAT2;0,0;False;25;FLOAT;0;False;24;FLOAT;0;False;4;FLOAT;30;FLOAT2;29;FLOAT;27;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;580;-1758.939,-1049.501;Inherit;False;Simplex2D;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;653;-1454.189,-2448.193;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;569;-1153.328,-2255.341;Inherit;False;Property;_NormalPower;NormalPower;2;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;22;-840.3798,-2341.5;Inherit;True;Property;_BumpMap;BumpMap;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;632;-1379.292,-723.2701;Inherit;False;Constant;_Float12;Float 12;23;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;602;-1519.687,-1121.872;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;648;-730.6127,-2133.972;Inherit;False;Constant;_Vector0;Vector 0;19;0;Create;True;0;0;0;False;0;False;0,0,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LerpOp;646;-421.202,-2222.707;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;606;-1132.445,-666.7697;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.02;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;353;-145.9124,-2266.921;Inherit;False;1532.296;565.0349;;13;298;289;299;297;303;359;360;325;357;355;326;358;323;FakeReflection;1,1,1,1;0;0
Node;AmplifyShaderEditor.DistanceOpNode;604;-1135.754,-800.9288;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;576;-1123.973,-1041.447;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;577;-1139.127,-1128.67;Float;False;Constant;_Float8;Float 8;13;0;Create;True;0;0;0;False;0;False;-0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;582;-973.5299,-1096.272;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;605;-963.6726,-798.0129;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;323;98.47964,-2125.746;Float;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldNormalVector;326;86.70059,-1971.703;Inherit;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldToCameraMatrix;358;60.06961,-2202.033;Inherit;False;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.TexturePropertyNode;149;-474.9874,-1290.944;Float;True;Property;_ParallaxHeight;ParallaxHeight;5;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;357;341.6707,-1992.234;Inherit;False;2;2;0;FLOAT4x4;0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;355;343.7676,-2096.934;Inherit;False;2;2;0;FLOAT4x4;0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;279;-423.4268,-829.7342;Float;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;150;-398.6554,-998.3173;Float;False;Constant;_Float13;Float 13;16;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;603;-734.1035,-1009.291;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-520.7863,-1090.895;Float;False;Property;_Depth;Depth;11;0;Create;True;0;0;0;False;0;False;0.5236971;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;280;-438.2218,-677.6832;Float;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CustomExpressionNode;600;-56.60433,-922.2889;Float;False;float2 dx = ddx(uvs)@$float2 dy = ddy(uvs)@$float minSamples = 8@$float maxSamples = 16@$float3 result = 0@$int stepIndex = 0@$int numSteps = ( int )lerp( (float)maxSamples, (float)minSamples, (float)dot( normalWorld, viewWorld ) )@$float layerHeight = 1.0 / numSteps@$float2 plane = parallax * ( viewDirTan.xy / viewDirTan.z )@$uvs += refPlane * plane@$float2 deltaTex = -plane * layerHeight@$float2 prevTexOffset = 0@$float prevRayZ = 1.0f@$float prevHeight = 0.0f@$float2 currTexOffset = deltaTex@$float currRayZ = 1.0f - layerHeight@$float currHeight = 0.0f@$float intersection = 0@$float2 finalTexOffset = 0@$float2 dialatedUV = 0@$float dialatedCenterDist = 0@$while ( stepIndex < numSteps + 1 )${$	dialatedUV = uvs + currTexOffset - float2(0.5,0.5)@$	dialatedCenterDist = length(dialatedUV)@$	dialatedCenterDist = max(0, currentDialation + dialatedCenterDist * (irisSize - currentDialation) / irisSize)@$	dialatedUV = normalize(dialatedUV) * dialatedCenterDist@$	dialatedUV +=  float2(0.5,0.5)@$	currHeight = tex2Dgrad( heightMap, dialatedUV, dx, dy ).r@$	if ( currHeight > currRayZ )$	{$		stepIndex = numSteps + 1@$	}$	else$	{$		stepIndex++@$		prevTexOffset = currTexOffset@$		prevRayZ = currRayZ@$		prevHeight = currHeight@$		currTexOffset += deltaTex@$		currRayZ -= layerHeight@$	}$}$int sectionSteps = 2@$int sectionIndex = 0@$float newZ = 0@$float newHeight = 0@$while ( sectionIndex < sectionSteps )${$	intersection = ( prevHeight - prevRayZ ) / ( prevHeight - currHeight + currRayZ - prevRayZ )@$	finalTexOffset = prevTexOffset + intersection * deltaTex@$	dialatedUV = uvs + finalTexOffset - float2(0.5,0.5)@$	dialatedCenterDist = length(dialatedUV)@$	dialatedCenterDist = max(0, currentDialation + dialatedCenterDist * (irisSize - currentDialation) / irisSize)@$	dialatedUV = normalize(dialatedUV) * dialatedCenterDist@$	dialatedUV +=  float2(0.5,0.5)@$	newZ = prevRayZ - intersection * layerHeight@$	newHeight = tex2Dgrad( heightMap, dialatedUV, dx, dy ).r@$	if ( newHeight > newZ )$	{$		currTexOffset = finalTexOffset@$		currHeight = newHeight@$		currRayZ = newZ@$		deltaTex = intersection * deltaTex@$		layerHeight = intersection * layerHeight@$	}$	else$	{$		prevTexOffset = finalTexOffset@$		prevHeight = newHeight@$		prevRayZ = newZ@$		deltaTex = ( 1 - intersection ) * deltaTex@$		layerHeight = ( 1 - intersection ) * layerHeight@$	}$	sectionIndex++@$}$return dialatedUV@;2;Create;9;True;normalWorld;FLOAT3;0,0,0;In;;Float;False;True;heightMap;SAMPLER2D;0.0;In;;Float;False;True;uvs;FLOAT2;0,0;In;;Float;False;True;viewWorld;FLOAT3;0,0,0;In;;Float;False;True;viewDirTan;FLOAT3;0,0,0;In;;Float;False;True;parallax;FLOAT;0;In;;Float;False;True;refPlane;FLOAT;0;In;;Float;False;True;currentDialation;FLOAT;0;In;;Float;False;True;irisSize;FLOAT;0;In;;Float;False;Parallax Occlusion Dialated;True;False;0;;False;9;0;FLOAT3;0,0,0;False;1;SAMPLER2D;0.0;False;2;FLOAT2;0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;634;1007.668,-2574.487;Inherit;False;Property;_Blood;Blood;7;0;Create;True;0;0;0;False;0;False;0.4705882,0.3737024,0.3737024,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;633;981.9465,-2754.731;Inherit;False;Property;_Scelera;Scelera;4;0;Create;True;0;0;0;False;0;False;0.6470588,0.6185122,0.6185122,0;0.2452829,0.2452829,0.2452829,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector3Node;360;513.4648,-2212.633;Float;False;Constant;_Vector3;Vector 3;12;0;Create;True;0;0;0;False;0;False;-1,-1,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ReflectOpNode;325;520.9183,-2056.01;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.IntNode;303;728.3558,-2006.904;Float;False;Constant;_Int0;Int 0;16;0;Create;True;0;0;0;False;0;False;0;0;False;0;1;INT;0
Node;AmplifyShaderEditor.SamplerNode;2;1091.645,-1613.473;Inherit;True;Property;_Albedo;Albedo;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;635;1025.234,-2989.386;Inherit;False;Property;_IrisRing;IrisRing;8;0;Create;True;0;0;0;False;0;False;1,0,0,1;0.3396226,0.3396226,0.3396226,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;359;726.5669,-2127.433;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;636;1288.219,-2625.693;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;33;1140.05,-1237.327;Float;False;Property;_EmissionPower;EmissionPower;3;0;Create;True;0;0;0;False;0;False;0;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;637;1375.69,-3131.718;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LightColorNode;297;1030.249,-1921.555;Inherit;False;0;3;COLOR;0;FLOAT3;1;FLOAT;2
Node;AmplifyShaderEditor.SamplerNode;289;892.7515,-2116.069;Inherit;True;Property;_StylizedReflection;StylizedReflection;6;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;black;LockedToCube;False;Object;-1;MipLevel;Cube;8;0;SAMPLERCUBE;;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;638;1478.639,-2627.165;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;299;1035.337,-1795.259;Float;False;Constant;_Float4;Float 4;16;0;Create;True;0;0;0;False;0;False;1.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;298;1243.748,-1942.396;Inherit;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;1495.141,-1349.938;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;641;1400.996,-1126.084;Inherit;False;Constant;_Color0;Color 0;25;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;639;1675.353,-2958.159;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;573;-1488.169,-857.3438;Float;False;Constant;_Float26;Float 26;18;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;640;1667.333,-1279.192;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;30;1995.318,-1157.401;Float;False;Property;_Smooth;Smooth;10;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;288;1875.034,-1773.312;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;164;1992.839,-1236.776;Float;False;Property;_Specular;Specular;9;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CustomExpressionNode;277;-53.15267,-1294.319;Float;False;float2 dx = ddx(uvs)@$float2 dy = ddy(uvs)@$float minSamples = 8@$float maxSamples = 16@$float3 result = 0@$int stepIndex = 0@$int numSteps = ( int )lerp( (float)maxSamples, (float)minSamples, (float)dot( normalWorld, viewWorld ) )@$float layerHeight = 1.0 / numSteps@$float2 plane = parallax * ( viewDirTan.xy / viewDirTan.z )@$uvs += refPlane * plane@$float2 deltaTex = -plane * layerHeight@$float2 prevTexOffset = 0@$float prevRayZ = 1.0f@$float prevHeight = 0.0f@$float2 currTexOffset = deltaTex@$float currRayZ = 1.0f - layerHeight@$float currHeight = 0.0f@$float intersection = 0@$float2 finalTexOffset = 0@$while ( stepIndex < numSteps + 1 )${$	currHeight = tex2Dgrad( heightMap, uvs + currTexOffset, dx, dy ).r@$	if ( currHeight > currRayZ )$	{$		stepIndex = numSteps + 1@$	}$	else$	{$		stepIndex++@$		prevTexOffset = currTexOffset@$		prevRayZ = currRayZ@$		prevHeight = currHeight@$		currTexOffset += deltaTex@$		currRayZ -= layerHeight@$	}$}$int sectionSteps = 2@$int sectionIndex = 0@$float newZ = 0@$float newHeight = 0@$while ( sectionIndex < sectionSteps )${$	intersection = ( prevHeight - prevRayZ ) / ( prevHeight - currHeight + currRayZ - prevRayZ )@$	finalTexOffset = prevTexOffset + intersection * deltaTex@$	newZ = prevRayZ - intersection * layerHeight@$	newHeight = tex2Dgrad( heightMap, uvs + finalTexOffset, dx, dy ).r@$	if ( newHeight > newZ )$	{$		currTexOffset = finalTexOffset@$		currHeight = newHeight@$		currRayZ = newZ@$		deltaTex = intersection * deltaTex@$		layerHeight = intersection * layerHeight@$	}$	else$	{$		prevTexOffset = finalTexOffset@$		prevHeight = newHeight@$		prevRayZ = newZ@$		deltaTex = ( 1 - intersection ) * deltaTex@$		layerHeight = ( 1 - intersection ) * layerHeight@$	}$	sectionIndex++@$}$return uvs + finalTexOffset@;2;Create;7;True;normalWorld;FLOAT3;0,0,0;In;;Float;False;True;heightMap;SAMPLER2D;0.0;In;;Float;False;True;uvs;FLOAT2;0,0;In;;Float;False;True;viewWorld;FLOAT3;0,0,0;In;;Float;False;True;viewDirTan;FLOAT3;0,0,0;In;;Float;False;True;parallax;FLOAT;0;In;;Float;False;True;refPlane;FLOAT;0;In;;Float;False;Parallax Occlusion Custom;True;False;0;;False;7;0;FLOAT3;0,0,0;False;1;SAMPLER2D;0.0;False;2;FLOAT2;0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT;0;False;6;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;572;-1487.169,-931.3439;Float;False;Constant;_Float3;Float 3;18;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;574;-1309.168,-969.3444;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;683;2590.219,-1452.336;Float;False;True;-1;2;ASEMaterialInspector;0;0;StandardSpecular;Vilar/EyeV2;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;398;0;444;0
WireConnection;427;0;398;0
WireConnection;427;1;428;0
WireConnection;461;0;446;0
WireConnection;461;1;457;0
WireConnection;410;0;398;0
WireConnection;460;0;462;0
WireConnection;460;1;461;0
WireConnection;418;0;427;0
WireConnection;408;0;410;0
WireConnection;408;1;409;0
WireConnection;458;0;457;0
WireConnection;458;1;459;0
WireConnection;445;0;460;0
WireConnection;437;0;398;0
WireConnection;437;1;441;0
WireConnection;419;0;418;0
WireConnection;419;1;417;0
WireConnection;389;0;410;0
WireConnection;456;0;458;0
WireConnection;447;0;445;0
WireConnection;433;0;437;0
WireConnection;433;1;434;0
WireConnection;403;0;408;0
WireConnection;425;0;418;0
WireConnection;426;0;419;0
WireConnection;396;0;397;0
WireConnection;396;1;389;0
WireConnection;455;0;447;0
WireConnection;455;1;456;0
WireConnection;404;0;397;0
WireConnection;404;1;403;0
WireConnection;436;0;433;0
WireConnection;422;0;420;0
WireConnection;422;1;425;0
WireConnection;423;0;420;0
WireConnection;423;1;426;0
WireConnection;451;0;455;0
WireConnection;451;1;450;0
WireConnection;443;0;436;0
WireConnection;424;0;422;0
WireConnection;424;1;423;0
WireConnection;424;2;421;0
WireConnection;395;0;396;0
WireConnection;395;1;404;0
WireConnection;395;2;406;0
WireConnection;452;0;451;0
WireConnection;429;0;395;0
WireConnection;429;1;424;0
WireConnection;429;2;443;0
WireConnection;453;0;429;0
WireConnection;453;1;454;0
WireConnection;453;2;452;0
WireConnection;659;0;652;0
WireConnection;411;0;412;0
WireConnection;411;1;453;0
WireConnection;575;0;581;0
WireConnection;575;1;468;0
WireConnection;654;0;659;0
WireConnection;654;1;411;0
WireConnection;578;0;575;0
WireConnection;650;31;654;0
WireConnection;650;25;616;0
WireConnection;650;24;610;0
WireConnection;580;0;578;0
WireConnection;653;0;650;29
WireConnection;653;1;411;0
WireConnection;22;1;653;0
WireConnection;22;5;569;0
WireConnection;602;0;580;0
WireConnection;646;0;22;0
WireConnection;646;1;648;0
WireConnection;646;2;650;30
WireConnection;606;0;632;0
WireConnection;604;0;650;29
WireConnection;576;0;632;0
WireConnection;576;1;602;0
WireConnection;582;0;577;0
WireConnection;582;1;576;0
WireConnection;605;0;604;0
WireConnection;605;1;632;0
WireConnection;605;2;606;0
WireConnection;326;0;646;0
WireConnection;357;0;358;0
WireConnection;357;1;326;0
WireConnection;355;0;358;0
WireConnection;355;1;323;0
WireConnection;603;0;582;0
WireConnection;603;2;605;0
WireConnection;600;1;149;0
WireConnection;600;2;650;29
WireConnection;600;3;279;0
WireConnection;600;4;280;0
WireConnection;600;5;29;0
WireConnection;600;6;150;0
WireConnection;600;7;603;0
WireConnection;600;8;632;0
WireConnection;325;0;355;0
WireConnection;325;1;357;0
WireConnection;2;1;600;0
WireConnection;359;0;360;0
WireConnection;359;1;325;0
WireConnection;636;0;633;0
WireConnection;636;1;634;0
WireConnection;636;2;650;27
WireConnection;637;0;650;0
WireConnection;637;1;635;4
WireConnection;289;1;359;0
WireConnection;289;2;303;0
WireConnection;638;0;2;0
WireConnection;638;1;636;0
WireConnection;638;2;650;30
WireConnection;298;0;289;1
WireConnection;298;1;297;0
WireConnection;298;2;299;0
WireConnection;32;0;2;0
WireConnection;32;1;33;0
WireConnection;639;0;638;0
WireConnection;639;1;635;0
WireConnection;639;2;637;0
WireConnection;640;0;32;0
WireConnection;640;1;641;0
WireConnection;640;2;650;30
WireConnection;288;0;298;0
WireConnection;288;1;639;0
WireConnection;277;1;149;0
WireConnection;277;2;650;29
WireConnection;277;3;279;0
WireConnection;277;4;280;0
WireConnection;277;5;29;0
WireConnection;277;6;150;0
WireConnection;574;0;602;0
WireConnection;574;1;572;0
WireConnection;574;2;573;0
WireConnection;683;0;288;0
WireConnection;683;2;640;0
WireConnection;683;3;164;0
WireConnection;683;4;30;0
ASEEND*/
//CHKSM=DE11158103C88F631A0A833FA7A41BF322717D65