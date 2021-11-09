// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "UnityJigglePhysicsExample/Standard/TriplanarChecker"
{
	Properties
	{
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
		};

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float4 color7_g5 = IsGammaSpace() ? float4(0.2,0.2,0.2,0) : float4(0.03310476,0.03310476,0.03310476,0);
			float4 color8_g5 = IsGammaSpace() ? float4(0.6980392,0.6980392,0.6980392,0) : float4(0.4452012,0.4452012,0.4452012,0);
			float3 ase_worldPos = i.worldPos;
			float2 FinalUV13_g5 = ( float2( 1,1 ) * ( 0.5 + (ase_worldPos).xz ) );
			float2 temp_cast_0 = (0.5).xx;
			float2 temp_cast_1 = (1.0).xx;
			float4 appendResult16_g5 = (float4(ddx( FinalUV13_g5 ) , ddy( FinalUV13_g5 )));
			float4 UVDerivatives17_g5 = appendResult16_g5;
			float4 break28_g5 = UVDerivatives17_g5;
			float2 appendResult19_g5 = (float2(break28_g5.x , break28_g5.z));
			float2 appendResult20_g5 = (float2(break28_g5.x , break28_g5.z));
			float dotResult24_g5 = dot( appendResult19_g5 , appendResult20_g5 );
			float2 appendResult21_g5 = (float2(break28_g5.y , break28_g5.w));
			float2 appendResult22_g5 = (float2(break28_g5.y , break28_g5.w));
			float dotResult23_g5 = dot( appendResult21_g5 , appendResult22_g5 );
			float2 appendResult25_g5 = (float2(dotResult24_g5 , dotResult23_g5));
			float2 derivativesLength29_g5 = sqrt( appendResult25_g5 );
			float2 temp_cast_2 = (-1.0).xx;
			float2 temp_cast_3 = (1.0).xx;
			float2 clampResult57_g5 = clamp( ( ( ( abs( ( frac( ( FinalUV13_g5 + 0.25 ) ) - temp_cast_0 ) ) * 4.0 ) - temp_cast_1 ) * ( 0.35 / derivativesLength29_g5 ) ) , temp_cast_2 , temp_cast_3 );
			float2 break71_g5 = clampResult57_g5;
			float2 break55_g5 = derivativesLength29_g5;
			float4 lerpResult73_g5 = lerp( color7_g5 , color8_g5 , saturate( ( 0.5 + ( 0.5 * break71_g5.x * break71_g5.y * sqrt( saturate( ( 1.1 - max( break55_g5.x , break55_g5.y ) ) ) ) ) ) ));
			float3 ase_worldNormal = i.worldNormal;
			float dotResult16 = dot( ase_worldNormal , float3( 0,1,0 ) );
			float4 color7_g7 = IsGammaSpace() ? float4(0.2,0.2,0.2,0) : float4(0.03310476,0.03310476,0.03310476,0);
			float4 color8_g7 = IsGammaSpace() ? float4(0.6980392,0.6980392,0.6980392,0) : float4(0.4452012,0.4452012,0.4452012,0);
			float2 FinalUV13_g7 = ( float2( 1,1 ) * ( 0.5 + (ase_worldPos).zy ) );
			float2 temp_cast_4 = (0.5).xx;
			float2 temp_cast_5 = (1.0).xx;
			float4 appendResult16_g7 = (float4(ddx( FinalUV13_g7 ) , ddy( FinalUV13_g7 )));
			float4 UVDerivatives17_g7 = appendResult16_g7;
			float4 break28_g7 = UVDerivatives17_g7;
			float2 appendResult19_g7 = (float2(break28_g7.x , break28_g7.z));
			float2 appendResult20_g7 = (float2(break28_g7.x , break28_g7.z));
			float dotResult24_g7 = dot( appendResult19_g7 , appendResult20_g7 );
			float2 appendResult21_g7 = (float2(break28_g7.y , break28_g7.w));
			float2 appendResult22_g7 = (float2(break28_g7.y , break28_g7.w));
			float dotResult23_g7 = dot( appendResult21_g7 , appendResult22_g7 );
			float2 appendResult25_g7 = (float2(dotResult24_g7 , dotResult23_g7));
			float2 derivativesLength29_g7 = sqrt( appendResult25_g7 );
			float2 temp_cast_6 = (-1.0).xx;
			float2 temp_cast_7 = (1.0).xx;
			float2 clampResult57_g7 = clamp( ( ( ( abs( ( frac( ( FinalUV13_g7 + 0.25 ) ) - temp_cast_4 ) ) * 4.0 ) - temp_cast_5 ) * ( 0.35 / derivativesLength29_g7 ) ) , temp_cast_6 , temp_cast_7 );
			float2 break71_g7 = clampResult57_g7;
			float2 break55_g7 = derivativesLength29_g7;
			float4 lerpResult73_g7 = lerp( color7_g7 , color8_g7 , saturate( ( 0.5 + ( 0.5 * break71_g7.x * break71_g7.y * sqrt( saturate( ( 1.1 - max( break55_g7.x , break55_g7.y ) ) ) ) ) ) ));
			float dotResult21 = dot( ase_worldNormal , float3( 1,0,0 ) );
			float4 color7_g6 = IsGammaSpace() ? float4(0.2,0.2,0.2,0) : float4(0.03310476,0.03310476,0.03310476,0);
			float4 color8_g6 = IsGammaSpace() ? float4(0.6980392,0.6980392,0.6980392,0) : float4(0.4452012,0.4452012,0.4452012,0);
			float2 FinalUV13_g6 = ( float2( 1,1 ) * ( 0.5 + (ase_worldPos).xy ) );
			float2 temp_cast_8 = (0.5).xx;
			float2 temp_cast_9 = (1.0).xx;
			float4 appendResult16_g6 = (float4(ddx( FinalUV13_g6 ) , ddy( FinalUV13_g6 )));
			float4 UVDerivatives17_g6 = appendResult16_g6;
			float4 break28_g6 = UVDerivatives17_g6;
			float2 appendResult19_g6 = (float2(break28_g6.x , break28_g6.z));
			float2 appendResult20_g6 = (float2(break28_g6.x , break28_g6.z));
			float dotResult24_g6 = dot( appendResult19_g6 , appendResult20_g6 );
			float2 appendResult21_g6 = (float2(break28_g6.y , break28_g6.w));
			float2 appendResult22_g6 = (float2(break28_g6.y , break28_g6.w));
			float dotResult23_g6 = dot( appendResult21_g6 , appendResult22_g6 );
			float2 appendResult25_g6 = (float2(dotResult24_g6 , dotResult23_g6));
			float2 derivativesLength29_g6 = sqrt( appendResult25_g6 );
			float2 temp_cast_10 = (-1.0).xx;
			float2 temp_cast_11 = (1.0).xx;
			float2 clampResult57_g6 = clamp( ( ( ( abs( ( frac( ( FinalUV13_g6 + 0.25 ) ) - temp_cast_8 ) ) * 4.0 ) - temp_cast_9 ) * ( 0.35 / derivativesLength29_g6 ) ) , temp_cast_10 , temp_cast_11 );
			float2 break71_g6 = clampResult57_g6;
			float2 break55_g6 = derivativesLength29_g6;
			float4 lerpResult73_g6 = lerp( color7_g6 , color8_g6 , saturate( ( 0.5 + ( 0.5 * break71_g6.x * break71_g6.y * sqrt( saturate( ( 1.1 - max( break55_g6.x , break55_g6.y ) ) ) ) ) ) ));
			float dotResult31 = dot( ase_worldNormal , float3( 0,0,1 ) );
			o.Albedo = saturate( ( ( lerpResult73_g5 * abs( dotResult16 ) ) + ( lerpResult73_g7 * abs( dotResult21 ) ) + ( lerpResult73_g6 * abs( dotResult31 ) ) ) ).rgb;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows 

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
				float3 worldPos : TEXCOORD1;
				float3 worldNormal : TEXCOORD2;
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
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.worldNormal = worldNormal;
				o.worldPos = worldPos;
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
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = IN.worldNormal;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
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
25;189;1671;781;2896.023;708.7724;2.432278;True;True
Node;AmplifyShaderEditor.WorldPosInputsNode;3;-1572.942,-225.8824;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldNormalVector;9;-1366.103,355.8436;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SwizzleNode;15;-1342.801,-309.7083;Inherit;False;FLOAT2;0;2;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;27;-1341.534,-187.1434;Inherit;False;FLOAT2;2;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;20;-1342.931,-51.64333;Inherit;False;FLOAT2;0;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DotProductOpNode;31;-1132.196,614.3189;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;21;-1112.609,492.03;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;1,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;16;-1119.609,359.0301;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,1,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;29;-1100.594,-181.5717;Inherit;False;Checkerboard;-1;;7;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;22;-949.6089,483.03;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;30;-1098.072,35.26633;Inherit;False;Checkerboard;-1;;6;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;32;-958.1967,612.3188;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;2;-1096.022,-411.5119;Inherit;False;Checkerboard;-1;;5;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;17;-960.6089,344.0302;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;-782.6528,-394.7073;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;36;-771.9108,38.61564;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;35;-772.8691,-158.9568;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;37;-492.041,-189.834;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;38;-247.7069,21.37286;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;UnityJigglePhysicsExample/Standard/TriplanarChecker;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;15;0;3;0
WireConnection;27;0;3;0
WireConnection;20;0;3;0
WireConnection;31;0;9;0
WireConnection;21;0;9;0
WireConnection;16;0;9;0
WireConnection;29;1;27;0
WireConnection;22;0;21;0
WireConnection;30;1;20;0
WireConnection;32;0;31;0
WireConnection;2;1;15;0
WireConnection;17;0;16;0
WireConnection;34;0;2;0
WireConnection;34;1;17;0
WireConnection;36;0;30;0
WireConnection;36;1;32;0
WireConnection;35;0;29;0
WireConnection;35;1;22;0
WireConnection;37;0;34;0
WireConnection;37;1;35;0
WireConnection;37;2;36;0
WireConnection;38;0;37;0
WireConnection;0;0;38;0
ASEEND*/
//CHKSM=AB164816472A90AB8177FCDAC828FAF4F46AF7AB