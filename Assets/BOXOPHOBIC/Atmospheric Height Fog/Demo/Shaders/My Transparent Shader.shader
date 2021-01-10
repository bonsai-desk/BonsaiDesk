// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Custom/My Transparent Shader"
{
	Properties
	{
		[HDR]_Color("Color", Color) = (1,0,0,0)
		[Space(10)]_NoiseIntensity("Noise Intensity", Range( 0 , 0.2)) = 0
		_NoiseScale("Noise Scale", Float) = 6
		_NoiseSpeed("Noise Speed", Vector) = (0.5,0.5,0,0)
		_VertexIntensity("Vertex Intensity", Range( 0 , 0.2)) = 0
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" "IsEmissive" = "true"  }
		Cull Back
		GrabPass{ }
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma multi_compile AHF_NOISEMODE_OFF AHF_NOISEMODE_PROCEDURAL3D
		#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
		#else
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
		#endif
		#pragma surface surf Standard alpha:fade keepalpha noshadow novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float4 screenPos;
			float3 worldNormal;
		};

		uniform half _NoiseScale;
		uniform half3 _NoiseSpeed;
		uniform float _VertexIntensity;
		uniform float4 _Color;
		ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
		uniform float _NoiseIntensity;
		uniform half4 AHF_FogColorStart;
		uniform half4 AHF_FogColorEnd;
		uniform half AHF_FogDistanceStart;
		uniform half AHF_FogDistanceEnd;
		uniform half AHF_FogDistanceFalloff;
		uniform half AHF_FogColorDuo;
		uniform half4 AHF_DirectionalColor;
		uniform half3 AHF_DirectionalDir;
		uniform half AHF_DirectionalIntensity;
		uniform half AHF_DirectionalFalloff;
		uniform half3 AHF_FogAxisOption;
		uniform half AHF_FogHeightEnd;
		uniform half AHF_FogHeightStart;
		uniform half AHF_FogHeightFalloff;
		uniform half AHF_FogLayersMode;
		uniform half AHF_NoiseScale;
		uniform half3 AHF_NoiseSpeed;
		uniform half AHF_NoiseDistanceEnd;
		uniform half AHF_NoiseIntensity;
		uniform half AHF_NoiseModeBlend;
		uniform half AHF_FogIntensity;


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


		inline float4 ASE_ComputeGrabScreenPos( float4 pos )
		{
			#if UNITY_UV_STARTS_AT_TOP
			float scale = -1.0;
			#else
			float scale = 1.0;
			#endif
			float4 o = pos;
			o.y = pos.w * 0.5f;
			o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
			return o;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float simplePerlin3D27 = snoise( ( ( ase_worldPos * _NoiseScale ) + ( -_NoiseSpeed * _Time.y ) ) );
			float3 ase_vertexNormal = v.normal.xyz;
			v.vertex.xyz += ( ( simplePerlin3D27 * _VertexIntensity ) * ase_vertexNormal );
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float3 ase_worldPos = i.worldPos;
			float simplePerlin3D27 = snoise( ( ( ase_worldPos * _NoiseScale ) + ( -_NoiseSpeed * _Time.y ) ) );
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ase_screenPos );
			float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
			float4 screenColor22 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,( ( _NoiseIntensity * simplePerlin3D27 ) + ase_grabScreenPosNorm ).xy);
			o.Albedo = saturate( ( _Color * screenColor22 ) ).rgb;
			float3 WorldPosition2_g836 = ase_worldPos;
			float temp_output_7_0_g860 = AHF_FogDistanceStart;
			half FogDistanceMask12_g836 = pow( abs( saturate( ( ( distance( WorldPosition2_g836 , _WorldSpaceCameraPos ) - temp_output_7_0_g860 ) / ( AHF_FogDistanceEnd - temp_output_7_0_g860 ) ) ) ) , AHF_FogDistanceFalloff );
			float3 lerpResult258_g836 = lerp( (AHF_FogColorStart).rgb , (AHF_FogColorEnd).rgb , ( saturate( ( FogDistanceMask12_g836 - 0.5 ) ) * AHF_FogColorDuo ));
			float3 normalizeResult318_g836 = normalize( ( WorldPosition2_g836 - _WorldSpaceCameraPos ) );
			float dotResult145_g836 = dot( normalizeResult318_g836 , AHF_DirectionalDir );
			half DirectionalMask30_g836 = pow( abs( ( (dotResult145_g836*0.5 + 0.5) * AHF_DirectionalIntensity ) ) , AHF_DirectionalFalloff );
			float3 lerpResult40_g836 = lerp( lerpResult258_g836 , (AHF_DirectionalColor).rgb , DirectionalMask30_g836);
			float3 temp_output_2_0_g859 = lerpResult40_g836;
			float3 gammaToLinear3_g859 = GammaToLinearSpace( temp_output_2_0_g859 );
			#ifdef UNITY_COLORSPACE_GAMMA
				float3 staticSwitch1_g859 = temp_output_2_0_g859;
			#else
				float3 staticSwitch1_g859 = gammaToLinear3_g859;
			#endif
			float3 temp_output_256_0_g836 = staticSwitch1_g859;
			float3 temp_output_92_86_g681 = temp_output_256_0_g836;
			half3 AHF_FogAxisOption181_g836 = AHF_FogAxisOption;
			float3 break159_g836 = ( WorldPosition2_g836 * AHF_FogAxisOption181_g836 );
			float temp_output_7_0_g861 = AHF_FogHeightEnd;
			half FogHeightMask16_g836 = pow( abs( saturate( ( ( ( break159_g836.x + break159_g836.y + break159_g836.z ) - temp_output_7_0_g861 ) / ( AHF_FogHeightStart - temp_output_7_0_g861 ) ) ) ) , AHF_FogHeightFalloff );
			float lerpResult328_g836 = lerp( ( FogDistanceMask12_g836 * FogHeightMask16_g836 ) , saturate( ( FogDistanceMask12_g836 + FogHeightMask16_g836 ) ) , AHF_FogLayersMode);
			float simplePerlin3D193_g836 = snoise( ( ( WorldPosition2_g836 * ( 1.0 / AHF_NoiseScale ) ) + ( -AHF_NoiseSpeed * _Time.y ) ) );
			float temp_output_7_0_g863 = AHF_NoiseDistanceEnd;
			half NoiseDistanceMask7_g836 = saturate( ( ( distance( WorldPosition2_g836 , _WorldSpaceCameraPos ) - temp_output_7_0_g863 ) / ( 0.0 - temp_output_7_0_g863 ) ) );
			float lerpResult198_g836 = lerp( 1.0 , (simplePerlin3D193_g836*0.5 + 0.5) , ( NoiseDistanceMask7_g836 * AHF_NoiseIntensity * AHF_NoiseModeBlend ));
			half NoiseSimplex3D24_g836 = lerpResult198_g836;
			#if defined(AHF_NOISEMODE_OFF)
				float staticSwitch42_g836 = lerpResult328_g836;
			#elif defined(AHF_NOISEMODE_PROCEDURAL3D)
				float staticSwitch42_g836 = ( lerpResult328_g836 * NoiseSimplex3D24_g836 );
			#else
				float staticSwitch42_g836 = lerpResult328_g836;
			#endif
			float temp_output_43_0_g836 = ( staticSwitch42_g836 * AHF_FogIntensity );
			float temp_output_92_87_g681 = temp_output_43_0_g836;
			float3 lerpResult82_g681 = lerp( float3( 0,0,0 ) , temp_output_92_86_g681 , temp_output_92_87_g681);
			o.Emission = lerpResult82_g681;
			o.Smoothness = 0.8;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = i.worldNormal;
			float fresnelNdotV79 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode79 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV79, 5.0 ) );
			o.Alpha = saturate( ( 1.0 - fresnelNode79 ) );
		}

		ENDCG
	}
}
/*ASEBEGIN
Version=18103
1927;1;1906;1020;3380.803;521.1696;3.741465;True;False
Node;AmplifyShaderEditor.Vector3Node;30;-1280,1664;Half;False;Property;_NoiseSpeed;Noise Speed;3;0;Create;True;0;0;False;0;False;0.5,0.5,0;0.5,0.5,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldPosInputsNode;39;-1280,1280;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;32;-1280,1440;Half;False;Property;_NoiseScale;Noise Scale;2;0;Create;True;0;0;False;0;False;6;1.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;31;-1280,1824;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;36;-1088,1664;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;35;-960,1344;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;37;-960,1664;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;33;-768,1536;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;41;0,1024;Inherit;False;Property;_NoiseIntensity;Noise Intensity;1;0;Create;True;0;0;False;1;Space(10);False;0;0.103;0;0.2;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;27;-640,1536;Inherit;False;Simplex3D;False;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;40;288,1024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GrabScreenPosition;23;256,1152;Inherit;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;24;448,1024;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.FresnelNode;79;1408,1664;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenColorNode;22;640,1152;Inherit;False;Global;_GrabScreen0;Grab Screen 0;1;0;Create;True;0;0;False;0;False;Object;-1;False;False;1;0;FLOAT2;0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;50;0,1728;Inherit;False;Property;_VertexIntensity;Vertex Intensity;4;0;Create;True;0;0;False;0;False;0;0.103;0;0.2;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;5;640,896;Inherit;False;Property;_Color;Color;0;1;[HDR];Create;True;0;0;False;0;False;1,0,0,0;1,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;896,896;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;384,1664;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;49;384,1792;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;80;1664,1664;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;81;1824,1664;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;48;704,1664;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;78;1408,1056;Inherit;False;Apply Height Fog;-1;;681;950890317d4f36a48a68d150cdab0168;0;1;81;FLOAT3;0,0,0;False;3;FLOAT3;85;FLOAT3;86;FLOAT;87
Node;AmplifyShaderEditor.SaturateNode;82;1056,896;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;55;1408,1280;Inherit;False;Constant;_Float6;Float 6;5;0;Create;True;0;0;False;0;False;0.8;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;2048,896;Float;False;True;-1;2;;0;0;Standard;Custom/My Transparent Shader;False;False;False;False;False;True;True;True;True;True;True;False;False;False;True;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.CommentaryNode;83;1408,1536;Inherit;False;588.5403;100;Edge Opacity;0;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;51;-1280,1152;Inherit;False;832.0697;100;Noise;0;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;76;0,768;Inherit;False;1182;100;Grab Screen Color;0;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;54;0,1536;Inherit;False;826.2407;100;Vertex Animaton;0;;1,1,1,1;0;0
WireConnection;36;0;30;0
WireConnection;35;0;39;0
WireConnection;35;1;32;0
WireConnection;37;0;36;0
WireConnection;37;1;31;0
WireConnection;33;0;35;0
WireConnection;33;1;37;0
WireConnection;27;0;33;0
WireConnection;40;0;41;0
WireConnection;40;1;27;0
WireConnection;24;0;40;0
WireConnection;24;1;23;0
WireConnection;22;0;24;0
WireConnection;44;0;5;0
WireConnection;44;1;22;0
WireConnection;45;0;27;0
WireConnection;45;1;50;0
WireConnection;80;0;79;0
WireConnection;81;0;80;0
WireConnection;48;0;45;0
WireConnection;48;1;49;0
WireConnection;82;0;44;0
WireConnection;0;0;82;0
WireConnection;0;2;78;85
WireConnection;0;4;55;0
WireConnection;0;9;81;0
WireConnection;0;11;48;0
ASEEND*/
//CHKSM=8E0F65C35DF7DC42365527BD2E094A48B3C2BB20