// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "PenetrationTechExample/TriplanarChecker"
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
			float4 color7_g1 = IsGammaSpace() ? float4(0.2,0.2,0.2,0) : float4(0.03310476,0.03310476,0.03310476,0);
			float4 color8_g1 = IsGammaSpace() ? float4(0.6980392,0.6980392,0.6980392,0) : float4(0.4452012,0.4452012,0.4452012,0);
			float3 ase_worldPos = i.worldPos;
			float2 FinalUV13_g1 = ( float2( 1,1 ) * ( 0.5 + (ase_worldPos).xz ) );
			float2 temp_cast_0 = (0.5).xx;
			float2 temp_cast_1 = (1.0).xx;
			float4 appendResult16_g1 = (float4(ddx( FinalUV13_g1 ) , ddy( FinalUV13_g1 )));
			float4 UVDerivatives17_g1 = appendResult16_g1;
			float4 break28_g1 = UVDerivatives17_g1;
			float2 appendResult19_g1 = (float2(break28_g1.x , break28_g1.z));
			float2 appendResult20_g1 = (float2(break28_g1.x , break28_g1.z));
			float dotResult24_g1 = dot( appendResult19_g1 , appendResult20_g1 );
			float2 appendResult21_g1 = (float2(break28_g1.y , break28_g1.w));
			float2 appendResult22_g1 = (float2(break28_g1.y , break28_g1.w));
			float dotResult23_g1 = dot( appendResult21_g1 , appendResult22_g1 );
			float2 appendResult25_g1 = (float2(dotResult24_g1 , dotResult23_g1));
			float2 derivativesLength29_g1 = sqrt( appendResult25_g1 );
			float2 temp_cast_2 = (-1.0).xx;
			float2 temp_cast_3 = (1.0).xx;
			float2 clampResult57_g1 = clamp( ( ( ( abs( ( frac( ( FinalUV13_g1 + 0.25 ) ) - temp_cast_0 ) ) * 4.0 ) - temp_cast_1 ) * ( 0.35 / derivativesLength29_g1 ) ) , temp_cast_2 , temp_cast_3 );
			float2 break71_g1 = clampResult57_g1;
			float2 break55_g1 = derivativesLength29_g1;
			float4 lerpResult73_g1 = lerp( color7_g1 , color8_g1 , saturate( ( 0.5 + ( 0.5 * break71_g1.x * break71_g1.y * sqrt( saturate( ( 1.1 - max( break55_g1.x , break55_g1.y ) ) ) ) ) ) ));
			float3 ase_worldNormal = i.worldNormal;
			float dotResult16 = dot( ase_worldNormal , float3( 0,1,0 ) );
			float4 color7_g2 = IsGammaSpace() ? float4(0.2,0.2,0.2,0) : float4(0.03310476,0.03310476,0.03310476,0);
			float4 color8_g2 = IsGammaSpace() ? float4(0.6980392,0.6980392,0.6980392,0) : float4(0.4452012,0.4452012,0.4452012,0);
			float2 FinalUV13_g2 = ( float2( 1,1 ) * ( 0.5 + (ase_worldPos).zy ) );
			float2 temp_cast_4 = (0.5).xx;
			float2 temp_cast_5 = (1.0).xx;
			float4 appendResult16_g2 = (float4(ddx( FinalUV13_g2 ) , ddy( FinalUV13_g2 )));
			float4 UVDerivatives17_g2 = appendResult16_g2;
			float4 break28_g2 = UVDerivatives17_g2;
			float2 appendResult19_g2 = (float2(break28_g2.x , break28_g2.z));
			float2 appendResult20_g2 = (float2(break28_g2.x , break28_g2.z));
			float dotResult24_g2 = dot( appendResult19_g2 , appendResult20_g2 );
			float2 appendResult21_g2 = (float2(break28_g2.y , break28_g2.w));
			float2 appendResult22_g2 = (float2(break28_g2.y , break28_g2.w));
			float dotResult23_g2 = dot( appendResult21_g2 , appendResult22_g2 );
			float2 appendResult25_g2 = (float2(dotResult24_g2 , dotResult23_g2));
			float2 derivativesLength29_g2 = sqrt( appendResult25_g2 );
			float2 temp_cast_6 = (-1.0).xx;
			float2 temp_cast_7 = (1.0).xx;
			float2 clampResult57_g2 = clamp( ( ( ( abs( ( frac( ( FinalUV13_g2 + 0.25 ) ) - temp_cast_4 ) ) * 4.0 ) - temp_cast_5 ) * ( 0.35 / derivativesLength29_g2 ) ) , temp_cast_6 , temp_cast_7 );
			float2 break71_g2 = clampResult57_g2;
			float2 break55_g2 = derivativesLength29_g2;
			float4 lerpResult73_g2 = lerp( color7_g2 , color8_g2 , saturate( ( 0.5 + ( 0.5 * break71_g2.x * break71_g2.y * sqrt( saturate( ( 1.1 - max( break55_g2.x , break55_g2.y ) ) ) ) ) ) ));
			float dotResult21 = dot( ase_worldNormal , float3( 1,0,0 ) );
			float4 color7_g3 = IsGammaSpace() ? float4(0.2,0.2,0.2,0) : float4(0.03310476,0.03310476,0.03310476,0);
			float4 color8_g3 = IsGammaSpace() ? float4(0.6980392,0.6980392,0.6980392,0) : float4(0.4452012,0.4452012,0.4452012,0);
			float2 FinalUV13_g3 = ( float2( 1,1 ) * ( 0.5 + (ase_worldPos).xy ) );
			float2 temp_cast_8 = (0.5).xx;
			float2 temp_cast_9 = (1.0).xx;
			float4 appendResult16_g3 = (float4(ddx( FinalUV13_g3 ) , ddy( FinalUV13_g3 )));
			float4 UVDerivatives17_g3 = appendResult16_g3;
			float4 break28_g3 = UVDerivatives17_g3;
			float2 appendResult19_g3 = (float2(break28_g3.x , break28_g3.z));
			float2 appendResult20_g3 = (float2(break28_g3.x , break28_g3.z));
			float dotResult24_g3 = dot( appendResult19_g3 , appendResult20_g3 );
			float2 appendResult21_g3 = (float2(break28_g3.y , break28_g3.w));
			float2 appendResult22_g3 = (float2(break28_g3.y , break28_g3.w));
			float dotResult23_g3 = dot( appendResult21_g3 , appendResult22_g3 );
			float2 appendResult25_g3 = (float2(dotResult24_g3 , dotResult23_g3));
			float2 derivativesLength29_g3 = sqrt( appendResult25_g3 );
			float2 temp_cast_10 = (-1.0).xx;
			float2 temp_cast_11 = (1.0).xx;
			float2 clampResult57_g3 = clamp( ( ( ( abs( ( frac( ( FinalUV13_g3 + 0.25 ) ) - temp_cast_8 ) ) * 4.0 ) - temp_cast_9 ) * ( 0.35 / derivativesLength29_g3 ) ) , temp_cast_10 , temp_cast_11 );
			float2 break71_g3 = clampResult57_g3;
			float2 break55_g3 = derivativesLength29_g3;
			float4 lerpResult73_g3 = lerp( color7_g3 , color8_g3 , saturate( ( 0.5 + ( 0.5 * break71_g3.x * break71_g3.y * sqrt( saturate( ( 1.1 - max( break55_g3.x , break55_g3.y ) ) ) ) ) ) ));
			float dotResult31 = dot( ase_worldNormal , float3( 0,0,1 ) );
			o.Albedo = saturate( ( ( lerpResult73_g1 * abs( dotResult16 ) ) + ( lerpResult73_g2 * abs( dotResult21 ) ) + ( lerpResult73_g3 * abs( dotResult31 ) ) ) ).rgb;
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
184;336;1675;788;2900.888;731.879;2.432278;True;False
Node;AmplifyShaderEditor.WorldPosInputsNode;3;-1572.942,-225.8824;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldNormalVector;9;-1366.103,355.8436;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SwizzleNode;15;-1342.801,-309.7083;Inherit;False;FLOAT2;0;2;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;27;-1341.534,-187.1434;Inherit;False;FLOAT2;2;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;20;-1342.931,-51.64333;Inherit;False;FLOAT2;0;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DotProductOpNode;31;-1132.196,614.3189;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;21;-1112.609,492.03;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;1,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;16;-1119.609,359.0301;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,1,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;32;-958.1967,612.3188;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;17;-960.6089,344.0302;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;2;-1096.022,-411.5119;Inherit;False;Checkerboard;-1;;1;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;29;-1100.594,-181.5717;Inherit;False;Checkerboard;-1;;2;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;30;-1098.072,35.26633;Inherit;False;Checkerboard;-1;;3;43dad715d66e03a4c8ad5f9564018081;0;4;1;FLOAT2;0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT2;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;22;-949.6089,483.03;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;-782.6528,-394.7073;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;36;-771.9108,38.61564;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;35;-772.8691,-158.9568;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;37;-492.041,-189.834;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;38;-247.7069,21.37286;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;PenetrationTechExample/TriplanarChecker;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;15;0;3;0
WireConnection;27;0;3;0
WireConnection;20;0;3;0
WireConnection;31;0;9;0
WireConnection;21;0;9;0
WireConnection;16;0;9;0
WireConnection;32;0;31;0
WireConnection;17;0;16;0
WireConnection;2;1;15;0
WireConnection;29;1;27;0
WireConnection;30;1;20;0
WireConnection;22;0;21;0
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
//CHKSM=06DCDDAFB6519195E0C703CCAC5FD230F6C83BE6