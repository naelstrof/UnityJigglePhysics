// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Unlit/KoboldKareSkybox"
{
	Properties
	{
		_StarFrequency("StarFrequency", Range( 0 , 500)) = 100
		_BlendOffset("BlendOffset", Range( 0 , 1)) = 0.25
		_StarPinpointy("StarPinpointy", Range( 1 , 1000)) = 0.9
		_StarVariance("StarVariance", Range( 0 , 1)) = 1
		_DarkSpotScale("DarkSpotScale", Range( 0 , 10)) = 1
		_SunSize("SunSize", Range( 0 , 1)) = 0.1
		_NightColor("NightColor", Color) = (0.06966787,0.008009965,0.1132075,0)
		_HDRSun("HDRSun", Range( 1 , 10)) = 2
		[Toggle(_CLOUDS_ON)] _CLOUDS("CLOUDS", Float) = 0
		_CloudHeight("Cloud Height", Range( 0.1 , 100)) = 0.1
		_CloudScale("CloudScale", Range( 0.1 , 100)) = 0
		_SecondaryNoiseScale("SecondaryNoiseScale", Range( 0 , 10)) = 1
		_CloudDensityOffset("CloudDensityOffset", Range( -10 , 10)) = 0
		_PhaseFactor("Phase Factor", Range( 0 , 1)) = 0.488
		_ForwardScattering("ForwardScattering", Range( 0 , 1)) = 0.811
		_BackScattering("BackScattering", Range( 0 , 1)) = 0.811
		_Brightness("Brightness", Range( 0 , 1)) = 0.811
		_CloudDistance("CloudDistance", Range( 10 , 1000)) = 1000
		_Eclipse("Eclipse", Range( 0 , 1)) = 0
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma multi_compile_local __ _CLOUDS_ON
		struct Input
		{
			float3 viewDir;
			float3 worldPos;
		};

		uniform float _DarkSpotScale;
		uniform float _StarFrequency;
		uniform float _StarPinpointy;
		uniform float _StarVariance;
		uniform float _BlendOffset;
		uniform float4 _NightColor;
		uniform float _Eclipse;
		uniform float _SunSize;
		uniform float _HDRSun;
		uniform float _Brightness;
		uniform float _CloudDistance;
		uniform float _CloudHeight;
		uniform float _CloudDensityOffset;
		uniform float _CloudScale;
		uniform float _SecondaryNoiseScale;
		uniform float _ForwardScattering;
		uniform float _BackScattering;
		uniform float _PhaseFactor;


		float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }

		float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }

		float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }

		float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }

		float snoise( float3 v )
		{
			const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
			float3 i = floor( v + dot( v, C.yyy ) );
			float3 x0 = v - i + dot( i, C.xxx );
			float3 g = step( x0.yzx, x0.xyz );
			float3 l = 1.0 - g;
			float3 i1 = min( g.xyz, l.zxy );
			float3 i2 = max( g.xyz, l.zxy );
			float3 x1 = x0 - i1 + C.xxx;
			float3 x2 = x0 - i2 + C.yyy;
			float3 x3 = x0 - 0.5;
			i = mod3D289( i);
			float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
			float4 j = p - 49.0 * floor( p / 49.0 );  // mod(p,7*7)
			float4 x_ = floor( j / 7.0 );
			float4 y_ = floor( j - 7.0 * x_ );  // mod(j,N)
			float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
			float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
			float4 h = 1.0 - abs( x ) - abs( y );
			float4 b0 = float4( x.xy, y.xy );
			float4 b1 = float4( x.zw, y.zw );
			float4 s0 = floor( b0 ) * 2.0 + 1.0;
			float4 s1 = floor( b1 ) * 2.0 + 1.0;
			float4 sh = -step( h, 0.0 );
			float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
			float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
			float3 g0 = float3( a0.xy, h.x );
			float3 g1 = float3( a0.zw, h.y );
			float3 g2 = float3( a1.xy, h.z );
			float3 g3 = float3( a1.zw, h.w );
			float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
			g0 *= norm.x;
			g1 *= norm.y;
			g2 *= norm.z;
			g3 *= norm.w;
			float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
			m = m* m;
			m = m* m;
			float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
			return 42.0 * dot( m, px);
		}


		float2 voronoihash45( float2 p )
		{
			
			p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
			return frac( sin( p ) *43758.5453);
		}


		float voronoi45( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
		{
			float2 n = floor( v );
			float2 f = frac( v );
			float F1 = 8.0;
			float F2 = 8.0; float2 mg = 0;
			for ( int j = -1; j <= 1; j++ )
			{
				for ( int i = -1; i <= 1; i++ )
			 	{
			 		float2 g = float2( i, j );
			 		float2 o = voronoihash45( n + g );
					o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
					float d = 0.5 * dot( r, r );
			 		if( d<F1 ) {
			 			F2 = F1;
			 			F1 = d; mg = g; mr = r; id = o;
			 		} else if( d<F2 ) {
			 			F2 = d;
			
			 		}
			 	}
			}
			return F1;
		}


		float2 voronoihash46( float2 p )
		{
			
			p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
			return frac( sin( p ) *43758.5453);
		}


		float voronoi46( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
		{
			float2 n = floor( v );
			float2 f = frac( v );
			float F1 = 8.0;
			float F2 = 8.0; float2 mg = 0;
			for ( int j = -1; j <= 1; j++ )
			{
				for ( int i = -1; i <= 1; i++ )
			 	{
			 		float2 g = float2( i, j );
			 		float2 o = voronoihash46( n + g );
					o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
					float d = 0.5 * dot( r, r );
			 		if( d<F1 ) {
			 			F2 = F1;
			 			F1 = d; mg = g; mr = r; id = o;
			 		} else if( d<F2 ) {
			 			F2 = d;
			
			 		}
			 	}
			}
			return F1;
		}


		struct Gradient
		{
			int type;
			int colorsLength;
			int alphasLength;
			float4 colors[8];
			float2 alphas[8];
		};


		Gradient NewGradient(int type, int colorsLength, int alphasLength, 
		float4 colors0, float4 colors1, float4 colors2, float4 colors3, float4 colors4, float4 colors5, float4 colors6, float4 colors7,
		float2 alphas0, float2 alphas1, float2 alphas2, float2 alphas3, float2 alphas4, float2 alphas5, float2 alphas6, float2 alphas7)
		{
			Gradient g;
			g.type = type;
			g.colorsLength = colorsLength;
			g.alphasLength = alphasLength;
			g.colors[ 0 ] = colors0;
			g.colors[ 1 ] = colors1;
			g.colors[ 2 ] = colors2;
			g.colors[ 3 ] = colors3;
			g.colors[ 4 ] = colors4;
			g.colors[ 5 ] = colors5;
			g.colors[ 6 ] = colors6;
			g.colors[ 7 ] = colors7;
			g.alphas[ 0 ] = alphas0;
			g.alphas[ 1 ] = alphas1;
			g.alphas[ 2 ] = alphas2;
			g.alphas[ 3 ] = alphas3;
			g.alphas[ 4 ] = alphas4;
			g.alphas[ 5 ] = alphas5;
			g.alphas[ 6 ] = alphas6;
			g.alphas[ 7 ] = alphas7;
			return g;
		}


		float4 SampleGradient( Gradient gradient, float time )
		{
			float3 color = gradient.colors[0].rgb;
			UNITY_UNROLL
			for (int c = 1; c < 8; c++)
			{
			float colorPos = saturate((time - gradient.colors[c-1].w) / ( 0.00001 + (gradient.colors[c].w - gradient.colors[c-1].w)) * step(c, (float)gradient.colorsLength-1));
			color = lerp(color, gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), gradient.type));
			}
			#ifndef UNITY_COLORSPACE_GAMMA
			color = half3(GammaToLinearSpaceExact(color.r), GammaToLinearSpaceExact(color.g), GammaToLinearSpaceExact(color.b));
			#endif
			float alpha = gradient.alphas[0].x;
			UNITY_UNROLL
			for (int a = 1; a < 8; a++)
			{
			float alphaPos = saturate((time - gradient.alphas[a-1].y) / ( 0.00001 + (gradient.alphas[a].y - gradient.alphas[a-1].y)) * step(a, (float)gradient.alphasLength-1));
			alpha = lerp(alpha, gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), gradient.type));
			}
			return float4(color, alpha);
		}


		float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
		{
			original -= center;
			float C = cos( angle );
			float S = sin( angle );
			float t = 1 - C;
			float m00 = t * u.x * u.x + C;
			float m01 = t * u.x * u.y - S * u.z;
			float m02 = t * u.x * u.z + S * u.y;
			float m10 = t * u.x * u.y + S * u.z;
			float m11 = t * u.y * u.y + C;
			float m12 = t * u.y * u.z - S * u.x;
			float m20 = t * u.x * u.z - S * u.y;
			float m21 = t * u.y * u.z + S * u.x;
			float m22 = t * u.z * u.z + C;
			float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
			return mul( finalMatrix, original ) + center;
		}


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


		float2 voronoihash344( float2 p )
		{
			
			p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
			return frac( sin( p ) *43758.5453);
		}


		float voronoi344( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
		{
			float2 n = floor( v );
			float2 f = frac( v );
			float F1 = 8.0;
			float F2 = 8.0; float2 mg = 0;
			for ( int j = -1; j <= 1; j++ )
			{
				for ( int i = -1; i <= 1; i++ )
			 	{
			 		float2 g = float2( i, j );
			 		float2 o = voronoihash344( n + g );
					o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
					float d = 0.5 * dot( r, r );
			 		if( d<F1 ) {
			 			F2 = F1;
			 			F1 = d; mg = g; mr = r; id = o;
			 		} else if( d<F2 ) {
			 			F2 = d;
			
			 		}
			 	}
			}
			return F1;
		}


		float2 voronoihash348( float2 p )
		{
			
			p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
			return frac( sin( p ) *43758.5453);
		}


		float voronoi348( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
		{
			float2 n = floor( v );
			float2 f = frac( v );
			float F1 = 8.0;
			float F2 = 8.0; float2 mg = 0;
			for ( int j = -1; j <= 1; j++ )
			{
				for ( int i = -1; i <= 1; i++ )
			 	{
			 		float2 g = float2( i, j );
			 		float2 o = voronoihash348( n + g );
					o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
					float d = 0.5 * dot( r, r );
			 		if( d<F1 ) {
			 			F2 = F1;
			 			F1 = d; mg = g; mr = r; id = o;
			 		} else if( d<F2 ) {
			 			F2 = d;
			
			 		}
			 	}
			}
			return F1;
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float simplePerlin3D73 = snoise( i.viewDir*_DarkSpotScale );
			simplePerlin3D73 = simplePerlin3D73*0.5 + 0.5;
			float time45 = 0.0;
			float2 voronoiSmoothId45 = 0;
			float3 appendResult8 = (float3(i.viewDir.x , 0.0 , i.viewDir.z));
			float3 normalizeResult7 = normalize( appendResult8 );
			float3 break9 = normalizeResult7;
			float temp_output_13_0 = asin( i.viewDir.y );
			float2 appendResult4 = (float2((0.0 + (atan2( break9.x , break9.z ) - ( -1.0 * UNITY_PI )) * (1.0 - 0.0) / (UNITY_PI - ( -1.0 * UNITY_PI ))) , (0.0 + (temp_output_13_0 - ( -1.0 * UNITY_PI )) * (1.0 - 0.0) / (UNITY_PI - ( -1.0 * UNITY_PI )))));
			float2 coords45 = ( appendResult4 * float2( 5,5 ) ) * _StarFrequency;
			float2 id45 = 0;
			float2 uv45 = 0;
			float voroi45 = voronoi45( coords45, time45, id45, uv45, 0, voronoiSmoothId45 );
			float mulTime186 = _Time.y * 2.0;
			float2 break7_g33 = id45;
			float lerpResult69_g33 = lerp( min( ( sin( ( mulTime186 + ( break7_g33.y * 100.0 ) ) ) + 1.5 ) , 1.0 ) , 1.0 , break7_g33.y);
			float temp_output_9_0_g33 = _StarVariance;
			float lerpResult50_g33 = lerp( ( _StarPinpointy * lerpResult69_g33 ) , 1000.0 , ( temp_output_9_0_g33 * break7_g33.y ));
			float temp_output_42_0_g33 = pow( ( 1.0 - saturate( voroi45 ) ) , lerpResult50_g33 );
			float lerpResult45_g33 = lerp( temp_output_42_0_g33 , ( temp_output_42_0_g33 * break7_g33.x ) , temp_output_9_0_g33);
			float time46 = 0.0;
			float2 voronoiSmoothId46 = 0;
			float2 appendResult22 = (float2(i.viewDir.x , i.viewDir.z));
			float2 coords46 = appendResult22 * _StarFrequency;
			float2 id46 = 0;
			float2 uv46 = 0;
			float voroi46 = voronoi46( coords46, time46, id46, uv46, 0, voronoiSmoothId46 );
			float2 break7_g32 = id46;
			float lerpResult69_g32 = lerp( min( ( sin( ( mulTime186 + ( break7_g32.y * 100.0 ) ) ) + 1.5 ) , 1.0 ) , 1.0 , break7_g32.y);
			float temp_output_9_0_g32 = _StarVariance;
			float lerpResult50_g32 = lerp( ( _StarPinpointy * lerpResult69_g32 ) , 1000.0 , ( temp_output_9_0_g32 * break7_g32.y ));
			float temp_output_42_0_g32 = pow( ( 1.0 - saturate( voroi46 ) ) , lerpResult50_g32 );
			float lerpResult45_g32 = lerp( temp_output_42_0_g32 , ( temp_output_42_0_g32 * break7_g32.x ) , temp_output_9_0_g32);
			float lerpResult36 = lerp( ( simplePerlin3D73 * lerpResult45_g33 ) , ( simplePerlin3D73 * lerpResult45_g32 ) , saturate( sign( ( abs( (-1.0 + (temp_output_13_0 - ( -1.0 * UNITY_PI )) * (1.0 - -1.0) / (UNITY_PI - ( -1.0 * UNITY_PI ))) ) - _BlendOffset ) ) ));
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = Unity_SafeNormalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 normalizeResult218 = normalize( ase_worldViewDir );
			#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aseld
			float3 ase_worldlightDir = 0;
			#else //aseld
			float3 ase_worldlightDir = normalize( UnityWorldSpaceLightDir( ase_worldPos ) );
			#endif //aseld
			float dotResult214 = dot( normalizeResult218 , ase_worldlightDir );
			float SunGradient223 = (0.0 + (dotResult214 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0));
			Gradient gradient242 = NewGradient( 0, 3, 2, float4( 0.3871961, 0.6081939, 0.8509804, 0 ), float4( 0.2666667, 0.5460913, 0.8509804, 0.04159609 ), float4( 0.3873883, 0.7686275, 0.7392303, 1 ), 0, 0, 0, 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
			float dotResult236 = dot( float3(0,1,0) , ase_worldlightDir );
			float LightIntensity240 = dotResult236;
			float4 lerpResult235 = lerp( _NightColor , SampleGradient( gradient242, SunGradient223 ) , saturate( LightIntensity240 ));
			Gradient gradient425 = NewGradient( 0, 3, 2, float4( 0.373637, 0.1864098, 0.6698113, 0 ), float4( 0.5921412, 0.2666667, 0.8509804, 0.04159609 ), float4( 0.254717, 0.002274259, 0, 1 ), 0, 0, 0, 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
			float temp_output_462_0 = frac( _Eclipse );
			float4 lerpResult427 = lerp( lerpResult235 , SampleGradient( gradient425, SunGradient223 ) , saturate( ( saturate( ( 1.0 - ( abs( ( temp_output_462_0 - 1.0 ) ) * 25.0 ) ) ) + saturate( ( 1.0 - ( temp_output_462_0 * 25.0 ) ) ) ) ));
			float temp_output_228_0 = ( _SunSize * _SunSize );
			float temp_output_3_0_g34 = ( temp_output_228_0 - SunGradient223 );
			float3 normalizeResult429 = normalize( ase_worldViewDir );
			float3 normalizeResult444 = normalize( float3(1,0,0) );
			float3 rotatedValue435 = RotateAroundAxis( float3( 0,0,0 ), ase_worldlightDir, normalizeResult444, ( _Eclipse * ( 2.0 * UNITY_PI ) ) );
			float dotResult431 = dot( normalizeResult429 , rotatedValue435 );
			float MoonGradient433 = (0.0 + (dotResult431 - -1.0) * (1.0 - 0.0) / (1.0 - -1.0));
			float temp_output_3_0_g37 = ( temp_output_228_0 - MoonGradient433 );
			float4 temp_cast_0 = (( saturate( ( temp_output_3_0_g37 / fwidth( temp_output_3_0_g37 ) ) ) * 10.0 )).xxxx;
			float4 temp_output_446_0 = saturate( ( ( saturate( ( ( lerpResult36 * SunGradient223 ) + lerpResult427 ) ) + ( saturate( ( temp_output_3_0_g34 / fwidth( temp_output_3_0_g34 ) ) ) * _HDRSun ) ) - temp_cast_0 ) );
			float temp_output_278_0 = ( _CloudHeight / saturate( -i.viewDir.y ) );
			float2 appendResult279 = (float2(i.viewDir.x , i.viewDir.z));
			float2 temp_output_280_0 = ( temp_output_278_0 * appendResult279 );
			float mulTime311 = _Time.y * 0.5;
			float mulTime313 = _Time.y * 2.0;
			float2 appendResult367 = (float2(mulTime311 , mulTime313));
			float simplePerlin2D364 = snoise( ( temp_output_280_0 + appendResult367 )*0.01 );
			simplePerlin2D364 = simplePerlin2D364*0.5 + 0.5;
			float lerpResult372 = lerp( 1.0 , simplePerlin2D364 , saturate( -_CloudDensityOffset ));
			float temp_output_290_0 = ( 1.0 / _CloudScale );
			float time344 = 0.0;
			float2 voronoiSmoothId344 = 0;
			float2 coords344 = ( temp_output_280_0 + mulTime311 ) * temp_output_290_0;
			float2 id344 = 0;
			float2 uv344 = 0;
			float voroi344 = voronoi344( coords344, time344, id344, uv344, 0, voronoiSmoothId344 );
			float time348 = 0.0;
			float2 voronoiSmoothId348 = 0;
			float mulTime368 = _Time.y * -2.0;
			float2 appendResult369 = (float2(mulTime368 , mulTime313));
			float2 coords348 = ( temp_output_280_0 + appendResult369 ) * ( temp_output_290_0 * _SecondaryNoiseScale );
			float2 id348 = 0;
			float2 uv348 = 0;
			float voroi348 = voronoi348( coords348, time348, id348, uv348, 0, voronoiSmoothId348 );
			float temp_output_301_0 = saturate( ( saturate( ( ( _CloudDistance - temp_output_278_0 ) / _CloudDistance ) ) * ( ( lerpResult372 * ( ( ( 1.0 - voroi344 ) * 1.35 ) - ( voroi348 * 0.35 ) ) ) + _CloudDensityOffset ) ) );
			float4 lerpResult360 = lerp( temp_output_446_0 , ( unity_FogColor * _Brightness ) , temp_output_301_0);
			float temp_output_2_0_g39 = _ForwardScattering;
			float temp_output_3_0_g39 = ( temp_output_2_0_g39 * temp_output_2_0_g39 );
			float dotResult327 = dot( i.viewDir , _WorldSpaceLightPos0.xyz );
			float temp_output_1_0_g38 = dotResult327;
			float temp_output_2_0_g40 = -_BackScattering;
			float temp_output_3_0_g40 = ( temp_output_2_0_g40 * temp_output_2_0_g40 );
			#ifdef _CLOUDS_ON
				float4 staticSwitch274 = ( lerpResult360 + ( temp_output_301_0 * ( _Brightness + ( ( ( ( ( 1.0 - temp_output_3_0_g39 ) / ( ( 4.0 * UNITY_PI ) * pow( ( ( temp_output_3_0_g39 + 1.0 ) - ( temp_output_2_0_g39 * 2.0 * temp_output_1_0_g38 ) ) , 1.5 ) ) ) * ( 1.0 - 0.5 ) ) + ( ( ( 1.0 - temp_output_3_0_g40 ) / ( ( 4.0 * UNITY_PI ) * pow( ( ( temp_output_3_0_g40 + 1.0 ) - ( temp_output_2_0_g40 * 2.0 * temp_output_1_0_g38 ) ) , 1.5 ) ) ) * 0.5 ) ) * _PhaseFactor ) ) ) );
			#else
				float4 staticSwitch274 = temp_output_446_0;
			#endif
			o.Emission = staticSwitch274.rgb;
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
				surfIN.viewDir = worldViewDir;
				surfIN.worldPos = worldPos;
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
Version=18934
296;450;1920;669;100.2154;576.3737;1;True;True
Node;AmplifyShaderEditor.CommentaryNode;144;-5265.016,-1993.213;Inherit;False;3002.487;1812.833;Stars;21;234;36;233;201;202;38;51;177;45;46;186;62;73;5;71;74;37;72;35;34;20;;0.1627161,0,0.5660378,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;20;-5189.351,-1771.759;Inherit;False;1610.303;733.9942;Horizontal Projection;12;8;7;9;11;6;12;10;4;2;14;13;44;;1,1,1,1;0;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;2;-5139.351,-1257.409;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;8;-4889.791,-1398.805;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;335;-1680.475,1342.332;Inherit;False;2435.871;1870.448;Clouds!;45;276;288;277;286;279;278;280;289;311;314;290;313;308;293;315;324;318;323;328;321;326;325;332;333;327;331;334;301;329;341;340;344;348;350;351;354;364;365;366;367;368;369;370;372;373;;1,1,1,1;0;0
Node;AmplifyShaderEditor.NormalizeNode;7;-4745.019,-1403.577;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;424;-7622.138,1597.424;Inherit;False;Property;_Eclipse;Eclipse;20;0;Create;True;0;0;0;False;0;False;0;0.03967071;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;212;-6103.257,858.411;Inherit;False;World;True;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FractNode;462;-7156.213,1603.666;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;276;-1630.475,1729.976;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.BreakToComponentsNode;9;-4566.837,-1441.759;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.PiNode;12;-4356.837,-1721.759;Inherit;False;1;0;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;423;-6243.606,1327.469;Inherit;False;False;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ASinOpNode;13;-4850.98,-1238.14;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;218;-5840.132,1048.077;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ATan2OpNode;6;-4315.473,-1438.577;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;288;-1442.865,1671.145;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;452;-7004.797,1480.4;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;11;-4360.019,-1651.759;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;214;-5633.021,1122.984;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;277;-1413.875,1544.576;Inherit;False;Property;_CloudHeight;Cloud Height;11;0;Create;True;0;0;0;False;0;False;0.1;41.03382;0.1;100;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;286;-1277.575,1676.618;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;10;-4094.337,-1504.805;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;14;-4194.465,-1244.765;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;34;-5215.015,-427.6236;Inherit;False;486.4854;238;Pole caps;2;21;22;;1,1,1,1;0;0
Node;AmplifyShaderEditor.AbsOpNode;453;-6823.407,1477.451;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;35;-4856.702,-1001.027;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-1;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;237;-6175.377,596.3954;Inherit;False;False;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;238;-6128.35,427.9334;Inherit;False;Constant;_Vector0;Vector 0;9;0;Create;True;0;0;0;False;0;False;0,1,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TFHCRemapNode;227;-5478.751,1120.861;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;21;-5165.015,-377.6235;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;279;-1028.143,1769.489;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;72;-4672.131,-785.8288;Inherit;False;Property;_BlendOffset;BlendOffset;1;0;Create;True;0;0;0;False;0;False;0.25;0.25;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;278;-1064.975,1565.976;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;4;-3846.09,-1309.914;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.AbsOpNode;37;-4557.787,-1002.082;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;236;-5769.145,521.482;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;455;-6875.667,1652.715;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;25;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;456;-6697.667,1452.715;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;25;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;434;-6414.226,1973.911;Inherit;False;Constant;_MoonAxis;MoonAxis;21;0;Create;True;0;0;0;False;0;False;1,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PiNode;437;-6539.012,2208.836;Inherit;False;1;0;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;459;-6577.758,1357.423;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-3817.184,-336.0929;Inherit;False;Property;_StarFrequency;StarFrequency;0;0;Create;True;0;0;0;False;0;False;100;30;0;500;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;289;-1243.145,1970.133;Inherit;False;Property;_CloudScale;CloudScale;12;0;Create;True;0;0;0;False;0;False;0;11.6;0.1;100;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;74;-4120.648,-1919.994;Inherit;False;Property;_DarkSpotScale;DarkSpotScale;5;0;Create;True;0;0;0;False;0;False;1;1.48;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;313;-1139.865,2436.758;Inherit;False;1;0;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;213;-4977.598,446.1521;Inherit;False;1002.534;522.598;SkyGradient;7;241;226;242;243;261;235;268;;0.1561944,0.8421995,0.8490566,1;0;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;71;-4235.708,-998.3391;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.25;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;311;-1102.507,2312.473;Inherit;False;1;0;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;368;-1135.975,2550.357;Inherit;False;1;0;FLOAT;-2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;-3708.631,-1302.978;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;5,5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;240;-5590.799,521.7595;Inherit;False;LightIntensity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;280;-784.5192,1712.917;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;223;-5209.387,1125.595;Inherit;False;SunGradient;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;457;-6671.667,1660.715;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;22;-4889.528,-355.0596;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GradientNode;242;-4844.118,492.2893;Inherit;False;0;3;2;0.3871961,0.6081939,0.8509804,0;0.2666667,0.5460913,0.8509804,0.04159609;0.3873883,0.7686275,0.7392303,1;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.VoronoiNode;45;-3422.465,-1310.074;Inherit;False;0;0;1;0;1;False;1;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;436;-6235.248,2221.068;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;369;-835.9746,2290.357;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;73;-3653.941,-1940.553;Inherit;False;Simplex3D;True;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;428;-6384.361,1687.49;Inherit;False;World;True;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;314;-611.9426,1562.015;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;290;-901.1451,1935.133;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;51;-3803.19,-770.3714;Inherit;False;Property;_StarPinpointy;StarPinpointy;2;0;Create;True;0;0;0;False;0;False;0.9;1000;1;1000;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;226;-4887.604,688.5129;Inherit;False;223;SunGradient;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;460;-6493.758,1622.423;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;186;-3277.316,-813.9222;Inherit;False;1;0;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SignOpNode;177;-4017.198,-995.4264;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;308;-1031.437,2154.677;Inherit;False;Property;_SecondaryNoiseScale;SecondaryNoiseScale;13;0;Create;True;0;0;0;False;0;False;1;1.387961;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;241;-4913.892,851.121;Inherit;False;240;LightIntensity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;444;-6128.19,1995.031;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;430;-6408.81,2370.948;Inherit;False;False;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.VoronoiNode;46;-3403.671,-537.8101;Inherit;False;0;0;1;0;1;False;1;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.SaturateNode;461;-6428.758,1485.423;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;62;-3816.437,-605.4595;Inherit;False;Property;_StarVariance;StarVariance;4;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;323;-447.1242,2262.06;Inherit;False;Property;_CloudDensityOffset;CloudDensityOffset;14;0;Create;True;0;0;0;False;0;False;0;-0.3;-10;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;202;-2886.912,-839.7602;Inherit;False;StarCalculation;-1;;33;0e4c725aa6385204a861c2e42fa9be1f;0;6;62;FLOAT;0;False;20;FLOAT;0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;8;FLOAT;0;False;9;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;454;-6274.897,1491.47;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;222;-3719.201,1697.753;Inherit;False;1065.025;493.8969;Sun;6;272;273;219;224;228;220;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;315;-615.843,2178.215;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;293;-751.9898,2032.413;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.GradientSampleNode;243;-4595.758,670.4139;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GradientNode;425;-4806.387,1115.844;Inherit;False;0;3;2;0.373637,0.1864098,0.6698113,0;0.5921412,0.2666667,0.8509804,0.04159609;0.254717,0.002274259,0,1;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.SaturateNode;261;-4693.128,852.7661;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;38;-3834.077,-963.4777;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;429;-5961.335,1810.856;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;268;-4587.354,490.2995;Inherit;False;Property;_NightColor;NightColor;7;0;Create;True;0;0;0;False;0;False;0.06966787,0.008009965,0.1132075,0;0.0696673,0.008009965,0.1132068,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VoronoiNode;344;-432.5872,1591.117;Inherit;False;0;0;1;0;1;False;1;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.FunctionNode;201;-2900.167,-565.2662;Inherit;False;StarCalculation;-1;;32;0e4c725aa6385204a861c2e42fa9be1f;0;6;62;FLOAT;0;False;20;FLOAT;0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;8;FLOAT;0;False;9;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;367;-844.2166,2484.383;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotateAboutAxisNode;435;-6012.023,2271.71;Inherit;False;False;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SaturateNode;458;-6108.02,1513.082;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;235;-4146.347,718.689;Inherit;False;3;0;COLOR;1,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.VoronoiNode;348;-402.1678,1850.754;Inherit;False;0;0;1;0;1;False;1;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.LerpOp;36;-2586.992,-961.0052;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;366;-591.1793,2347.883;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GradientSampleNode;426;-4501.245,1052.25;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;318;-211.6977,1641.811;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;220;-3552.74,1972.543;Inherit;False;Property;_SunSize;SunSize;6;0;Create;True;0;0;0;False;0;False;0.1;0.047;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;431;-5754.225,1885.763;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;233;-2626.315,-616.811;Inherit;False;223;SunGradient;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;370;-147.242,2276.812;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;350;-62.6345,1672.197;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1.35;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;224;-3506.186,1799.366;Inherit;False;223;SunGradient;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;427;-2941.44,456.2325;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;351;-211.1144,1853.887;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.35;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;432;-5599.955,1883.64;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;228;-3227.637,1947.791;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;340;-963.7635,1384.448;Inherit;False;Property;_CloudDistance;CloudDistance;19;0;Create;True;0;0;0;False;0;False;1000;300;10;1000;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;364;-224.6603,2013.645;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;0.01;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;373;59.758,2235.812;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;234;-2459.866,-763.9937;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;273;-3206.833,2090.284;Inherit;False;Property;_HDRSun;HDRSun;9;0;Create;True;0;0;0;False;0;False;2;3;1;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;354;-39.26369,1812.227;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;207;-1398.05,-140.1549;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;372;2.757996,2050.812;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;1;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;324;-425.4473,1392.332;Inherit;False;2;0;FLOAT;1000;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;219;-3094.561,1774.173;Inherit;False;Step Antialiasing;-1;;34;2a825e80dfb3290468194f83380797bd;0;2;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;433;-5329.291,1885.774;Inherit;False;MoonGradient;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;365;108.6955,1891.969;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;442;-5031.705,1831.451;Inherit;False;Step Antialiasing;-1;;37;2a825e80dfb3290468194f83380797bd;0;2;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;272;-2804.59,1854.843;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;162;-1200.527,-145.0415;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;341;-247.8835,1393.192;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;321;245.8572,1925.7;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;326;-93.22502,1537.986;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;271;-1005.214,-158.6466;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldSpaceLightPos;328;-825.2289,2715.703;Inherit;False;0;3;FLOAT4;0;FLOAT3;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;443;-4714.734,1767.121;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;325;247.1796,1677.094;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;327;-559.2997,2625.625;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;445;-1178.652,431.2706;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;334;-438.2663,2995.007;Inherit;False;Property;_Brightness;Brightness;18;0;Create;True;0;0;0;False;0;False;0.811;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;332;-450.2663,2787.007;Inherit;False;Property;_ForwardScattering;ForwardScattering;16;0;Create;True;0;0;0;False;0;False;0.811;0.8941177;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FogAndAmbientColorsNode;336;-195.0847,421.6528;Inherit;False;unity_FogColor;0;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;331;-440.4004,3096.78;Inherit;False;Property;_PhaseFactor;Phase Factor;15;0;Create;True;0;0;0;False;0;False;0.488;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;333;-453.2663,2894.007;Inherit;False;Property;_BackScattering;BackScattering;17;0;Create;True;0;0;0;False;0;False;0.811;0.8633384;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;329;-81.66835,2715.775;Inherit;False;Phase;-1;;38;71047d2c2279cc84e941cfb54eb2b413;0;5;1;FLOAT;0;False;4;FLOAT;0.811;False;5;FLOAT;0.33;False;6;FLOAT;1;False;7;FLOAT;0.488;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;446;-580.8799,280.9131;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;363;164.4435,521.9069;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;301;435.7755,1694.687;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;361;561.6156,1967.845;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;360;336.527,271.1392;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;260;-3745.911,787.3089;Inherit;False;1420.017;744.5098;Horizon Golden Hour;15;249;245;246;247;244;254;255;252;256;250;248;262;263;264;265;;1,0.4386806,0.2877358,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;358;587.3241,177.1078;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;262;-3443.3,983.4865;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;246;-3486.276,1366.645;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;248;-2838.898,880.7312;Inherit;False;Property;_GoldenHours;GoldenHours;8;0;Create;True;0;0;0;False;0;False;0.8490566,0.4905805,0.2843539,0;0.9245283,0.4536633,0.1875216,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;252;-2722.256,1166.634;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;264;-3312.723,950.7543;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;244;-3306.912,1254.254;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;255;-3012.897,1226.144;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;250;-3686.168,979.1771;Inherit;False;240;LightIntensity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;265;-2988.451,1031.056;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;254;-3161.788,1262.737;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;256;-2867.789,1222.359;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;245;-3695.911,1343.818;Inherit;False;World;True;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.StaticSwitch;274;517.5037,-245.8056;Inherit;False;Property;_CLOUDS;CLOUDS;10;0;Create;True;0;0;0;False;0;False;1;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;249;-2507.894,837.3089;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.Vector3Node;247;-3677.277,1134.644;Inherit;False;Constant;_Vector1;Vector 1;7;0;Create;True;0;0;0;False;0;False;0,-1,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;263;-3154.656,995.9233;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;468;956.373,-302.9604;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Unlit/KoboldKareSkybox;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;8;0;2;1
WireConnection;8;2;2;3
WireConnection;7;0;8;0
WireConnection;462;0;424;0
WireConnection;9;0;7;0
WireConnection;13;0;2;2
WireConnection;218;0;212;0
WireConnection;6;0;9;0
WireConnection;6;1;9;2
WireConnection;288;0;276;2
WireConnection;452;0;462;0
WireConnection;214;0;218;0
WireConnection;214;1;423;0
WireConnection;286;0;288;0
WireConnection;10;0;6;0
WireConnection;10;1;12;0
WireConnection;10;2;11;0
WireConnection;14;0;13;0
WireConnection;14;1;12;0
WireConnection;14;2;11;0
WireConnection;453;0;452;0
WireConnection;35;0;13;0
WireConnection;35;1;12;0
WireConnection;35;2;11;0
WireConnection;227;0;214;0
WireConnection;279;0;276;1
WireConnection;279;1;276;3
WireConnection;278;0;277;0
WireConnection;278;1;286;0
WireConnection;4;0;10;0
WireConnection;4;1;14;0
WireConnection;37;0;35;0
WireConnection;236;0;238;0
WireConnection;236;1;237;0
WireConnection;455;0;462;0
WireConnection;456;0;453;0
WireConnection;459;0;456;0
WireConnection;71;0;37;0
WireConnection;71;1;72;0
WireConnection;44;0;4;0
WireConnection;240;0;236;0
WireConnection;280;0;278;0
WireConnection;280;1;279;0
WireConnection;223;0;227;0
WireConnection;457;0;455;0
WireConnection;22;0;21;1
WireConnection;22;1;21;3
WireConnection;45;0;44;0
WireConnection;45;2;5;0
WireConnection;436;0;424;0
WireConnection;436;1;437;0
WireConnection;369;0;368;0
WireConnection;369;1;313;0
WireConnection;73;0;2;0
WireConnection;73;1;74;0
WireConnection;314;0;280;0
WireConnection;314;1;311;0
WireConnection;290;1;289;0
WireConnection;460;0;457;0
WireConnection;177;0;71;0
WireConnection;444;0;434;0
WireConnection;46;0;22;0
WireConnection;46;2;5;0
WireConnection;461;0;459;0
WireConnection;202;62;186;0
WireConnection;202;20;73;0
WireConnection;202;1;45;1
WireConnection;202;2;45;0
WireConnection;202;8;51;0
WireConnection;202;9;62;0
WireConnection;454;0;461;0
WireConnection;454;1;460;0
WireConnection;315;0;280;0
WireConnection;315;1;369;0
WireConnection;293;0;290;0
WireConnection;293;1;308;0
WireConnection;243;0;242;0
WireConnection;243;1;226;0
WireConnection;261;0;241;0
WireConnection;38;0;177;0
WireConnection;429;0;428;0
WireConnection;344;0;314;0
WireConnection;344;2;290;0
WireConnection;201;62;186;0
WireConnection;201;20;73;0
WireConnection;201;1;46;1
WireConnection;201;2;46;0
WireConnection;201;8;51;0
WireConnection;201;9;62;0
WireConnection;367;0;311;0
WireConnection;367;1;313;0
WireConnection;435;0;444;0
WireConnection;435;1;436;0
WireConnection;435;3;430;0
WireConnection;458;0;454;0
WireConnection;235;0;268;0
WireConnection;235;1;243;0
WireConnection;235;2;261;0
WireConnection;348;0;315;0
WireConnection;348;2;293;0
WireConnection;36;0;202;0
WireConnection;36;1;201;0
WireConnection;36;2;38;0
WireConnection;366;0;280;0
WireConnection;366;1;367;0
WireConnection;426;0;425;0
WireConnection;426;1;226;0
WireConnection;318;0;344;0
WireConnection;431;0;429;0
WireConnection;431;1;435;0
WireConnection;370;0;323;0
WireConnection;350;0;318;0
WireConnection;427;0;235;0
WireConnection;427;1;426;0
WireConnection;427;2;458;0
WireConnection;351;0;348;0
WireConnection;432;0;431;0
WireConnection;228;0;220;0
WireConnection;228;1;220;0
WireConnection;364;0;366;0
WireConnection;373;0;370;0
WireConnection;234;0;36;0
WireConnection;234;1;233;0
WireConnection;354;0;350;0
WireConnection;354;1;351;0
WireConnection;207;0;234;0
WireConnection;207;1;427;0
WireConnection;372;1;364;0
WireConnection;372;2;373;0
WireConnection;324;0;340;0
WireConnection;324;1;278;0
WireConnection;219;1;224;0
WireConnection;219;2;228;0
WireConnection;433;0;432;0
WireConnection;365;0;372;0
WireConnection;365;1;354;0
WireConnection;442;1;433;0
WireConnection;442;2;228;0
WireConnection;272;0;219;0
WireConnection;272;1;273;0
WireConnection;162;0;207;0
WireConnection;341;0;324;0
WireConnection;341;1;340;0
WireConnection;321;0;365;0
WireConnection;321;1;323;0
WireConnection;326;0;341;0
WireConnection;271;0;162;0
WireConnection;271;1;272;0
WireConnection;443;0;442;0
WireConnection;325;0;326;0
WireConnection;325;1;321;0
WireConnection;327;0;276;0
WireConnection;327;1;328;1
WireConnection;445;0;271;0
WireConnection;445;1;443;0
WireConnection;329;1;327;0
WireConnection;329;4;332;0
WireConnection;329;5;333;0
WireConnection;329;6;334;0
WireConnection;329;7;331;0
WireConnection;446;0;445;0
WireConnection;363;0;336;0
WireConnection;363;1;334;0
WireConnection;301;0;325;0
WireConnection;361;0;301;0
WireConnection;361;1;329;0
WireConnection;360;0;446;0
WireConnection;360;1;363;0
WireConnection;360;2;301;0
WireConnection;358;0;360;0
WireConnection;358;1;361;0
WireConnection;262;0;250;0
WireConnection;246;0;245;0
WireConnection;252;0;265;0
WireConnection;252;1;256;0
WireConnection;264;0;262;0
WireConnection;244;0;247;0
WireConnection;244;1;246;0
WireConnection;255;0;254;0
WireConnection;265;0;263;0
WireConnection;254;0;244;0
WireConnection;256;0;255;0
WireConnection;256;1;255;0
WireConnection;274;1;446;0
WireConnection;274;0;358;0
WireConnection;249;0;235;0
WireConnection;249;1;248;0
WireConnection;249;2;252;0
WireConnection;263;0;264;0
WireConnection;468;2;274;0
ASEEND*/
//CHKSM=13D26952D9FD43ED1C3352B3EED9A210D9F516E3