// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Hidden/BOXOPHOBIC/Atmospherics/Height Fog Global"
{
	Properties
	{
		[HideInInspector]_IsStandardPipeline("_IsStandardPipeline", Float) = 0
		[HideInInspector]_HeightFogGlobal("_HeightFogGlobal", Float) = 1
		[HideInInspector]_IsHeightFogShader("_IsHeightFogShader", Float) = 1
		[HideInInspector]_TransparentQueue("_TransparentQueue", Int) = 3000
		[StyledBanner(Height Fog Global)]_TITLE("< TITLE >", Float) = 1

	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Overlay" "Queue"="Overlay" }
	LOD 0

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Front
		ColorMask RGBA
		ZWrite Off
		ZTest Always
		
		
		
		Pass
		{
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" "PreviewType"="Skybox" }
			CGPROGRAM

			

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "UnityStandardBRDF.cginc"
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma multi_compile AHF_DIRECTIONALMODE_OFF AHF_DIRECTIONALMODE_ON
			#pragma multi_compile AHF_NOISEMODE_OFF AHF_NOISEMODE_PROCEDURAL3D


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 worldPos : TEXCOORD0;
#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				float4 ase_texcoord1 : TEXCOORD1;
			};

			//This is a late directive
			
			uniform int _TransparentQueue;
			uniform half _TITLE;
			uniform half _HeightFogGlobal;
			uniform half _IsHeightFogShader;
			uniform half _IsStandardPipeline;
			uniform half4 AHF_FogColor;
			uniform half4 AHF_DirectionalColor;
			UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
			uniform float4 _CameraDepthTexture_TexelSize;
			uniform half AHF_DirectionalIntensity;
			uniform half AHF_DirectionalModeBlend;
			uniform half AHF_FogDistanceStart;
			uniform half AHF_FogDistanceEnd;
			uniform half3 AHF_FogAxisOption;
			uniform half AHF_FogHeightEnd;
			uniform half AHF_FogHeightStart;
			uniform half AHF_NoiseScale;
			uniform half3 AHF_NoiseSpeed;
			uniform half AHF_NoiseDistanceEnd;
			uniform half AHF_NoiseIntensity;
			uniform half AHF_NoiseModeBlend;
			uniform half AHF_SkyboxFogHeight;
			uniform half AHF_SkyboxFogFill;
			uniform half AHF_FogIntensity;
			float2 UnStereo( float2 UV )
			{
				#if UNITY_SINGLE_PASS_STEREO
				float4 scaleOffset = unity_StereoScaleOffset[ unity_StereoEyeIndex];
				UV.xy = (UV.xy - scaleOffset.zw) / scaleOffset.xy;
				#endif
				return UV;
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
			

			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float3 temp_cast_0 = (( _IsStandardPipeline * 0.0 )).xxx;
				
				float4 ase_clipPos = UnityObjectToClipPos(v.vertex);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord1 = screenPos;
				
				float3 vertexValue = float3(0, 0, 0);
				#if ASE_ABSOLUTE_VERTEX_POS
				vertexValue = v.vertex.xyz;
				#endif
				vertexValue = temp_cast_0;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				o.vertex = UnityObjectToClipPos(v.vertex);

#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
#endif
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
#ifdef ASE_NEEDS_FRAG_WORLD_POSITION
				float3 WorldPosition = i.worldPos;
#endif
				float3 temp_output_2_0_g753 = (AHF_FogColor).rgb;
				float3 gammaToLinear3_g753 = GammaToLinearSpace( temp_output_2_0_g753 );
				#ifdef UNITY_COLORSPACE_GAMMA
				float3 staticSwitch1_g753 = temp_output_2_0_g753;
				#else
				float3 staticSwitch1_g753 = gammaToLinear3_g753;
				#endif
				float3 temp_output_34_0_g746 = staticSwitch1_g753;
				float3 temp_output_2_0_g749 = (AHF_DirectionalColor).rgb;
				float3 gammaToLinear3_g749 = GammaToLinearSpace( temp_output_2_0_g749 );
				#ifdef UNITY_COLORSPACE_GAMMA
				float3 staticSwitch1_g749 = temp_output_2_0_g749;
				#else
				float3 staticSwitch1_g749 = gammaToLinear3_g749;
				#endif
				float4 screenPos = i.ase_texcoord1;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 UV38_g754 = ase_screenPosNorm.xy;
				float2 localUnStereo38_g754 = UnStereo( UV38_g754 );
				float2 break6_g754 = localUnStereo38_g754;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float clampDepth3_g747 = SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_grabScreenPosNorm.xy );
				float ifLocalVar7_g747 = 0;
				UNITY_BRANCH 
				if( _ProjectionParams.x > 0.0 )
				ifLocalVar7_g747 = ( 1.0 - clampDepth3_g747 );
				else if( _ProjectionParams.x < 0.0 )
				ifLocalVar7_g747 = clampDepth3_g747;
				#ifdef UNITY_REVERSED_Z
				float staticSwitch9_g747 = ifLocalVar7_g747;
				#else
				float staticSwitch9_g747 = ( 1.0 - ifLocalVar7_g747 );
				#endif
				float RawDepth89_g746 = staticSwitch9_g747;
				float temp_output_41_0_g754 = RawDepth89_g746;
				#ifdef UNITY_REVERSED_Z
				float staticSwitch5_g754 = ( 1.0 - temp_output_41_0_g754 );
				#else
				float staticSwitch5_g754 = temp_output_41_0_g754;
				#endif
				float3 appendResult11_g754 = (float3(break6_g754.x , break6_g754.y , staticSwitch5_g754));
				float4 appendResult16_g754 = (float4((appendResult11_g754*2.0 + -1.0) , 1.0));
				float4 break18_g754 = mul( unity_CameraInvProjection, appendResult16_g754 );
				float3 appendResult19_g754 = (float3(break18_g754.x , break18_g754.y , break18_g754.z));
				float4 appendResult27_g754 = (float4(( ( appendResult19_g754 / break18_g754.w ) * half3(1,1,-1) ) , 1.0));
				float4 break30_g754 = mul( unity_CameraToWorld, appendResult27_g754 );
				float3 appendResult31_g754 = (float3(break30_g754.x , break30_g754.y , break30_g754.z));
				float3 WorldPosition2_g746 = appendResult31_g754;
				float3 normalizeResult5_g750 = normalize( ( WorldPosition2_g746 - _WorldSpaceCameraPos ) );
				float3 worldSpaceLightDir = Unity_SafeNormalize(UnityWorldSpaceLightDir(WorldPosition));
				float dotResult6_g750 = dot( normalizeResult5_g750 , worldSpaceLightDir );
				half DirectionalMask30_g746 = ( (dotResult6_g750*0.5 + 0.5) * AHF_DirectionalIntensity * AHF_DirectionalModeBlend );
				float3 lerpResult40_g746 = lerp( temp_output_34_0_g746 , staticSwitch1_g749 , DirectionalMask30_g746);
				#if defined(AHF_DIRECTIONALMODE_OFF)
				float3 staticSwitch45_g746 = temp_output_34_0_g746;
				#elif defined(AHF_DIRECTIONALMODE_ON)
				float3 staticSwitch45_g746 = lerpResult40_g746;
				#else
				float3 staticSwitch45_g746 = temp_output_34_0_g746;
				#endif
				float temp_output_7_0_g752 = AHF_FogDistanceStart;
				half FogDistanceMask12_g746 = saturate( ( ( distance( WorldPosition2_g746 , _WorldSpaceCameraPos ) - temp_output_7_0_g752 ) / ( AHF_FogDistanceEnd - temp_output_7_0_g752 ) ) );
				float3 break12_g755 = ( WorldPosition2_g746 * AHF_FogAxisOption );
				float temp_output_7_0_g756 = AHF_FogHeightEnd;
				half FogHeightMask16_g746 = saturate( ( ( ( break12_g755.x + break12_g755.y + break12_g755.z ) - temp_output_7_0_g756 ) / ( AHF_FogHeightStart - temp_output_7_0_g756 ) ) );
				float temp_output_29_0_g746 = ( FogDistanceMask12_g746 * FogHeightMask16_g746 );
				float simplePerlin3D15_g757 = snoise( ( ( WorldPosition2_g746 * ( 1.0 / AHF_NoiseScale ) ) + ( -AHF_NoiseSpeed * _Time.y ) ) );
				float temp_output_7_0_g761 = AHF_NoiseDistanceEnd;
				half NoiseDistanceMask7_g746 = saturate( ( ( distance( WorldPosition2_g746 , _WorldSpaceCameraPos ) - temp_output_7_0_g761 ) / ( 0.0 - temp_output_7_0_g761 ) ) );
				float lerpResult20_g757 = lerp( 1.0 , (simplePerlin3D15_g757*0.5 + 0.5) , ( NoiseDistanceMask7_g746 * AHF_NoiseIntensity * AHF_NoiseModeBlend ));
				half NoiseSimplex3D24_g746 = lerpResult20_g757;
				#if defined(AHF_NOISEMODE_OFF)
				float staticSwitch42_g746 = temp_output_29_0_g746;
				#elif defined(AHF_NOISEMODE_PROCEDURAL3D)
				float staticSwitch42_g746 = ( temp_output_29_0_g746 * NoiseSimplex3D24_g746 );
				#else
				float staticSwitch42_g746 = temp_output_29_0_g746;
				#endif
				float3 normalizeResult25_g758 = normalize( WorldPosition2_g746 );
				float3 break22_g758 = ( normalizeResult25_g758 * AHF_FogAxisOption );
				float temp_output_7_0_g759 = AHF_SkyboxFogHeight;
				float lerpResult17_g758 = lerp( saturate( ( ( abs( ( break22_g758.x + break22_g758.y + break22_g758.z ) ) - temp_output_7_0_g759 ) / ( 0.0 - temp_output_7_0_g759 ) ) ) , 1.0 , AHF_SkyboxFogFill);
				half SkyboxFogHeightMask108_g746 = lerpResult17_g758;
				float temp_output_6_0_g748 = RawDepth89_g746;
				#ifdef UNITY_REVERSED_Z
				float staticSwitch11_g748 = temp_output_6_0_g748;
				#else
				float staticSwitch11_g748 = ( 1.0 - temp_output_6_0_g748 );
				#endif
				half SkyboxMask95_g746 = ( 1.0 - ceil( staticSwitch11_g748 ) );
				float lerpResult112_g746 = lerp( staticSwitch42_g746 , SkyboxFogHeightMask108_g746 , SkyboxMask95_g746);
				float temp_output_43_0_g746 = ( lerpResult112_g746 * AHF_FogIntensity );
				float4 appendResult114_g746 = (float4(staticSwitch45_g746 , temp_output_43_0_g746));
				
				
				finalColor = appendResult114_g746;
				return finalColor;
			}
			ENDCG
		}
	}
	
	
	
}
/*ASEBEGIN
Version=17802
1927;7;1906;1014;4004.13;5134.83;1;True;False
Node;AmplifyShaderEditor.RangedFloatNode;879;-3136,-4864;Half;False;Property;_HeightFogGlobal;_HeightFogGlobal;4;1;[HideInInspector];Create;False;0;0;True;0;1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;885;-2912,-4864;Half;False;Property;_IsHeightFogShader;_IsHeightFogShader;5;1;[HideInInspector];Create;False;0;0;True;0;1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;915;-3328,-4480;Inherit;False;Is Pipeline;0;;762;6a59a34c4be5db64ca90ee69227573b8;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;891;-2656,-4864;Float;False;Property;_TransparentQueue;_TransparentQueue;6;1;[HideInInspector];Create;False;0;0;True;0;3000;0;0;1;INT;0
Node;AmplifyShaderEditor.RangedFloatNode;892;-3328,-4864;Half;False;Property;_TITLE;< TITLE >;7;0;Create;True;0;0;True;1;StyledBanner(Height Fog Global);1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;924;-3328,-4608;Inherit;False;Base;-1;;746;7ce331de1e1cd8c4d83adad8f3b33ab6;2,99,1,116,1;0;3;FLOAT4;113;FLOAT3;86;FLOAT;87
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;383;-3072,-4608;Float;False;True;-1;2;;0;1;Hidden/BOXOPHOBIC/Atmospherics/Height Fog Global;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;True;2;5;False;-1;10;False;-1;0;5;False;-1;10;False;-1;True;0;False;-1;0;False;-1;True;False;True;1;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;594;True;7;False;595;True;False;0;False;500;1000;False;500;True;2;RenderType=Overlay=RenderType;Queue=Overlay=Queue=0;True;2;0;False;False;False;False;False;False;False;False;False;True;2;LightMode=ForwardBase;PreviewType=Skybox;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;1;True;False;;0
Node;AmplifyShaderEditor.CommentaryNode;880;-3328,-4992;Inherit;False;919.8825;100;Drawers;0;;1,0.475862,0,1;0;0
WireConnection;383;0;924;113
WireConnection;383;1;915;0
ASEEND*/
//CHKSM=47B1F5367F20C7A7F1B20497902347F2A29DA2BF