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
		Stencil
		{
			Ref 222
			Comp NotEqual
			Pass Zero
		}
		
		
		Pass
		{
			Name "Unlit"
			//Tags { "LightMode"="ForwardBase" "PreviewType"="Skybox" }
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

			uniform half _IsHeightFogShader;
			uniform half _TITLE;
			uniform half _HeightFogGlobal;
			uniform int _TransparentQueue;
			uniform half _IsStandardPipeline;
			uniform half4 AHF_FogColorStart;
			uniform half4 AHF_FogColorEnd;
			UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
			uniform float4 _CameraDepthTexture_TexelSize;
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
			uniform half AHF_SkyboxFogHeight;
			uniform half AHF_SkyboxFogFalloff;
			uniform half AHF_SkyboxFogFill;
			uniform half AHF_SkyboxFogIntensity;
			uniform half AHF_FogIntensity;
			float2 UnStereo( float2 UV )
			{
				#if UNITY_SINGLE_PASS_STEREO
				float4 scaleOffset = unity_StereoScaleOffset[ unity_StereoEyeIndex];
				UV.xy = (UV.xy - scaleOffset.zw) / scaleOffset.xy;
				#endif
				return UV;
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
				float4 screenPos = i.ase_texcoord1;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 UV235_g1045 = ase_screenPosNorm.xy;
				float2 localUnStereo235_g1045 = UnStereo( UV235_g1045 );
				float2 break248_g1045 = localUnStereo235_g1045;
				float clampDepth227_g1045 = SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy );
				#ifdef UNITY_REVERSED_Z
				float staticSwitch250_g1045 = ( 1.0 - clampDepth227_g1045 );
				#else
				float staticSwitch250_g1045 = clampDepth227_g1045;
				#endif
				float3 appendResult244_g1045 = (float3(break248_g1045.x , break248_g1045.y , staticSwitch250_g1045));
				float4 appendResult220_g1045 = (float4((appendResult244_g1045*2.0 + -1.0) , 1.0));
				float4 break229_g1045 = mul( unity_CameraInvProjection, appendResult220_g1045 );
				float3 appendResult237_g1045 = (float3(break229_g1045.x , break229_g1045.y , break229_g1045.z));
				float4 appendResult233_g1045 = (float4(( ( appendResult237_g1045 / break229_g1045.w ) * half3(1,1,-1) ) , 1.0));
				float4 break245_g1045 = mul( unity_CameraToWorld, appendResult233_g1045 );
				float3 appendResult239_g1045 = (float3(break245_g1045.x , break245_g1045.y , break245_g1045.z));
				float3 WorldPositionFromDepth253_g1045 = appendResult239_g1045;
				float3 WorldPosition2_g1045 = WorldPositionFromDepth253_g1045;
				float temp_output_7_0_g1047 = AHF_FogDistanceStart;
				half FogDistanceMask12_g1045 = pow( abs( saturate( ( ( distance( WorldPosition2_g1045 , _WorldSpaceCameraPos ) - temp_output_7_0_g1047 ) / ( AHF_FogDistanceEnd - temp_output_7_0_g1047 ) ) ) ) , AHF_FogDistanceFalloff );
				float3 lerpResult258_g1045 = lerp( (AHF_FogColorStart).rgb , (AHF_FogColorEnd).rgb , ( saturate( ( FogDistanceMask12_g1045 - 0.5 ) ) * AHF_FogColorDuo ));
				float3 normalizeResult318_g1045 = normalize( ( WorldPosition2_g1045 - _WorldSpaceCameraPos ) );
				float dotResult145_g1045 = dot( normalizeResult318_g1045 , AHF_DirectionalDir );
				half DirectionalMask30_g1045 = pow( abs( ( (dotResult145_g1045*0.5 + 0.5) * AHF_DirectionalIntensity ) ) , AHF_DirectionalFalloff );
				float3 lerpResult40_g1045 = lerp( lerpResult258_g1045 , (AHF_DirectionalColor).rgb , DirectionalMask30_g1045);
				float3 temp_output_2_0_g1046 = lerpResult40_g1045;
				float3 gammaToLinear3_g1046 = GammaToLinearSpace( temp_output_2_0_g1046 );
				#ifdef UNITY_COLORSPACE_GAMMA
				float3 staticSwitch1_g1046 = temp_output_2_0_g1046;
				#else
				float3 staticSwitch1_g1046 = gammaToLinear3_g1046;
				#endif
				float3 temp_output_256_0_g1045 = staticSwitch1_g1046;
				half3 AHF_FogAxisOption181_g1045 = AHF_FogAxisOption;
				float3 break159_g1045 = ( WorldPosition2_g1045 * AHF_FogAxisOption181_g1045 );
				float temp_output_7_0_g1048 = AHF_FogHeightEnd;
				half FogHeightMask16_g1045 = pow( abs( saturate( ( ( ( break159_g1045.x + break159_g1045.y + break159_g1045.z ) - temp_output_7_0_g1048 ) / ( AHF_FogHeightStart - temp_output_7_0_g1048 ) ) ) ) , AHF_FogHeightFalloff );
				float lerpResult328_g1045 = lerp( ( FogDistanceMask12_g1045 * FogHeightMask16_g1045 ) , saturate( ( FogDistanceMask12_g1045 + FogHeightMask16_g1045 ) ) , AHF_FogLayersMode);
				float simplePerlin3D193_g1045 = snoise( ( ( WorldPosition2_g1045 * ( 1.0 / AHF_NoiseScale ) ) + ( -AHF_NoiseSpeed * _Time.y ) ) );
				float temp_output_7_0_g1050 = AHF_NoiseDistanceEnd;
				half NoiseDistanceMask7_g1045 = saturate( ( ( distance( WorldPosition2_g1045 , _WorldSpaceCameraPos ) - temp_output_7_0_g1050 ) / ( 0.0 - temp_output_7_0_g1050 ) ) );
				float lerpResult198_g1045 = lerp( 1.0 , (simplePerlin3D193_g1045*0.5 + 0.5) , ( NoiseDistanceMask7_g1045 * AHF_NoiseIntensity * AHF_NoiseModeBlend ));
				half NoiseSimplex3D24_g1045 = lerpResult198_g1045;
				#if defined(AHF_NOISEMODE_OFF)
				float staticSwitch42_g1045 = lerpResult328_g1045;
				#elif defined(AHF_NOISEMODE_PROCEDURAL3D)
				float staticSwitch42_g1045 = ( lerpResult328_g1045 * NoiseSimplex3D24_g1045 );
				#else
				float staticSwitch42_g1045 = lerpResult328_g1045;
				#endif
				float3 normalizeResult169_g1045 = normalize( WorldPosition2_g1045 );
				float3 break170_g1045 = ( normalizeResult169_g1045 * AHF_FogAxisOption181_g1045 );
				float temp_output_7_0_g1049 = AHF_SkyboxFogHeight;
				float saferPower309_g1045 = max( abs( saturate( ( ( abs( ( break170_g1045.x + break170_g1045.y + break170_g1045.z ) ) - temp_output_7_0_g1049 ) / ( 0.0 - temp_output_7_0_g1049 ) ) ) ) , 0.0001 );
				float lerpResult179_g1045 = lerp( pow( saferPower309_g1045 , AHF_SkyboxFogFalloff ) , 1.0 , AHF_SkyboxFogFill);
				half SkyboxFogHeightMask108_g1045 = ( lerpResult179_g1045 * AHF_SkyboxFogIntensity );
				float clampDepth118_g1045 = SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy );
				#ifdef UNITY_REVERSED_Z
				float staticSwitch123_g1045 = clampDepth118_g1045;
				#else
				float staticSwitch123_g1045 = ( 1.0 - clampDepth118_g1045 );
				#endif
				half SkyboxMask95_g1045 = ( 1.0 - ceil( staticSwitch123_g1045 ) );
				float lerpResult112_g1045 = lerp( staticSwitch42_g1045 , SkyboxFogHeightMask108_g1045 , SkyboxMask95_g1045);
				float temp_output_43_0_g1045 = ( lerpResult112_g1045 * AHF_FogIntensity );
				float4 appendResult114_g1045 = (float4(temp_output_256_0_g1045 , temp_output_43_0_g1045));
				
				
				finalColor = appendResult114_g1045;
				return finalColor;
			}
			ENDCG
		}
	}
	
	
	
}
/*ASEBEGIN
Version=18103
1927;1;1906;1020;5409.4;5525.235;2.429641;True;False
Node;AmplifyShaderEditor.RangedFloatNode;879;-3136,-4864;Half;False;Property;_HeightFogGlobal;_HeightFogGlobal;4;1;[HideInInspector];Create;False;0;0;True;0;False;1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;891;-2656,-4864;Float;False;Property;_TransparentQueue;_TransparentQueue;6;1;[HideInInspector];Create;False;0;0;True;0;False;3000;0;0;1;INT;0
Node;AmplifyShaderEditor.FunctionNode;1029;-3328,-4608;Inherit;False;Base;-1;;1045;13c50910e5b86de4097e1181ba121e0e;2,116,1,99,1;0;3;FLOAT4;113;FLOAT3;86;FLOAT;87
Node;AmplifyShaderEditor.FunctionNode;915;-3328,-4480;Inherit;False;Is Pipeline;0;;1044;2b33d0c660fbdb24c98bea96428031b0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;885;-2912,-4864;Half;False;Property;_IsHeightFogShader;_IsHeightFogShader;5;1;[HideInInspector];Create;False;0;0;True;0;False;1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;892;-3328,-4864;Half;False;Property;_TITLE;< TITLE >;7;0;Create;True;0;0;True;1;StyledBanner(Height Fog Global);False;1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;383;-3072,-4608;Float;False;True;-1;2;;0;1;Hidden/BOXOPHOBIC/Atmospherics/Height Fog Global;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;True;2;5;False;-1;10;False;-1;0;5;False;-1;10;False;-1;True;0;False;-1;0;False;-1;True;False;True;1;False;-1;True;True;True;True;True;0;False;-1;True;True;222;False;-1;255;False;-1;255;False;-1;6;False;-1;2;False;-1;0;False;-1;0;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;594;True;7;False;595;True;False;0;False;500;1000;False;500;True;2;RenderType=Overlay=RenderType;Queue=Overlay=Queue=0;True;2;0;False;False;False;False;False;False;False;False;False;True;2;LightMode=ForwardBase;PreviewType=Skybox;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;1;True;False;;0
Node;AmplifyShaderEditor.CommentaryNode;880;-3328,-4992;Inherit;False;919.8825;100;Drawers;0;;1,0.475862,0,1;0;0
WireConnection;383;0;1029;113
WireConnection;383;1;915;0
ASEEND*/
//CHKSM=BFDE70D7D1E42608460807DF4DA7EDF074083B59