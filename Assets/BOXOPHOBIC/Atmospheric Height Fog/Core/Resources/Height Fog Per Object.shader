// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "BOXOPHOBIC/Atmospherics/Height Fog Per Object"
{
	Properties
	{
		[HideInInspector]_HeightFogPerObject("_HeightFogPerObject", Float) = 1
		[HideInInspector]_IsStandardPipeline("_IsStandardPipeline", Float) = 0
		[HideInInspector]_IsHeightFogShader("_IsHeightFogShader", Float) = 1
		[HideInInspector]_TransparentQueue("_TransparentQueue", Int) = 3000
		[StyledBanner(Height Fog Per Object)]_TITLEE("< TITLEE >", Float) = 1
		[StyledCategory(Custom Alpha Inputs)]_CUSTOM("[ CUSTOM ]", Float) = 1
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("MainTex", 2D) = "white" {}

	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
	LOD 0

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend SrcAlpha OneMinusSrcAlpha , One One
		Cull Back
		ColorMask RGBA
		ZWrite Off
		ZTest LEqual
		
		
		
		Pass
		{
			Name "Unlit"
			//Tags { "LightMode"="ForwardBase" }
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
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#pragma multi_compile AHF_NOISEMODE_OFF AHF_NOISEMODE_PROCEDURAL3D


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float4 ase_texcoord : TEXCOORD0;
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

			uniform half _CUSTOM;
			uniform half _HeightFogPerObject;
			uniform half _IsHeightFogShader;
			uniform half _TITLEE;
			uniform int _TransparentQueue;
			uniform half _IsStandardPipeline;
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
			uniform half4 _Color;
			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
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

				int CustomVertexOffset918 = 0;
				float3 temp_cast_0 = (( CustomVertexOffset918 + ( _IsStandardPipeline * 0.0 ) )).xxx;
				
				o.ase_texcoord1.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord1.zw = 0;
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
				float3 WorldPosition2_g771 = WorldPosition;
				float temp_output_7_0_g860 = AHF_FogDistanceStart;
				half FogDistanceMask12_g771 = pow( abs( saturate( ( ( distance( WorldPosition2_g771 , _WorldSpaceCameraPos ) - temp_output_7_0_g860 ) / ( AHF_FogDistanceEnd - temp_output_7_0_g860 ) ) ) ) , AHF_FogDistanceFalloff );
				float3 lerpResult258_g771 = lerp( (AHF_FogColorStart).rgb , (AHF_FogColorEnd).rgb , ( saturate( ( FogDistanceMask12_g771 - 0.5 ) ) * AHF_FogColorDuo ));
				float3 normalizeResult318_g771 = normalize( ( WorldPosition2_g771 - _WorldSpaceCameraPos ) );
				float dotResult145_g771 = dot( normalizeResult318_g771 , AHF_DirectionalDir );
				half DirectionalMask30_g771 = pow( abs( ( (dotResult145_g771*0.5 + 0.5) * AHF_DirectionalIntensity ) ) , AHF_DirectionalFalloff );
				float3 lerpResult40_g771 = lerp( lerpResult258_g771 , (AHF_DirectionalColor).rgb , DirectionalMask30_g771);
				float3 temp_output_2_0_g859 = lerpResult40_g771;
				float3 gammaToLinear3_g859 = GammaToLinearSpace( temp_output_2_0_g859 );
				#ifdef UNITY_COLORSPACE_GAMMA
				float3 staticSwitch1_g859 = temp_output_2_0_g859;
				#else
				float3 staticSwitch1_g859 = gammaToLinear3_g859;
				#endif
				float3 temp_output_256_0_g771 = staticSwitch1_g859;
				half3 AHF_FogAxisOption181_g771 = AHF_FogAxisOption;
				float3 break159_g771 = ( WorldPosition2_g771 * AHF_FogAxisOption181_g771 );
				float temp_output_7_0_g861 = AHF_FogHeightEnd;
				half FogHeightMask16_g771 = pow( abs( saturate( ( ( ( break159_g771.x + break159_g771.y + break159_g771.z ) - temp_output_7_0_g861 ) / ( AHF_FogHeightStart - temp_output_7_0_g861 ) ) ) ) , AHF_FogHeightFalloff );
				float lerpResult328_g771 = lerp( ( FogDistanceMask12_g771 * FogHeightMask16_g771 ) , saturate( ( FogDistanceMask12_g771 + FogHeightMask16_g771 ) ) , AHF_FogLayersMode);
				float simplePerlin3D193_g771 = snoise( ( ( WorldPosition2_g771 * ( 1.0 / AHF_NoiseScale ) ) + ( -AHF_NoiseSpeed * _Time.y ) ) );
				float temp_output_7_0_g863 = AHF_NoiseDistanceEnd;
				half NoiseDistanceMask7_g771 = saturate( ( ( distance( WorldPosition2_g771 , _WorldSpaceCameraPos ) - temp_output_7_0_g863 ) / ( 0.0 - temp_output_7_0_g863 ) ) );
				float lerpResult198_g771 = lerp( 1.0 , (simplePerlin3D193_g771*0.5 + 0.5) , ( NoiseDistanceMask7_g771 * AHF_NoiseIntensity * AHF_NoiseModeBlend ));
				half NoiseSimplex3D24_g771 = lerpResult198_g771;
				#if defined(AHF_NOISEMODE_OFF)
				float staticSwitch42_g771 = lerpResult328_g771;
				#elif defined(AHF_NOISEMODE_PROCEDURAL3D)
				float staticSwitch42_g771 = ( lerpResult328_g771 * NoiseSimplex3D24_g771 );
				#else
				float staticSwitch42_g771 = lerpResult328_g771;
				#endif
				float temp_output_43_0_g771 = ( staticSwitch42_g771 * AHF_FogIntensity );
				float2 uv0_MainTex = i.ase_texcoord1.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float temp_output_1_0_g753 = tex2D( _MainTex, uv0_MainTex ).a;
				float temp_output_7_0_g752 = AHF_FogDistanceStart;
				float lerpResult3_g753 = lerp( temp_output_1_0_g753 , ceil( temp_output_1_0_g753 ) , saturate( ( ( distance( WorldPosition , _WorldSpaceCameraPos ) - temp_output_7_0_g752 ) / ( AHF_FogDistanceEnd - temp_output_7_0_g752 ) ) ));
				half CustomAlphaInputs897 = ( _Color.a * lerpResult3_g753 );
				float4 appendResult384 = (float4(temp_output_256_0_g771 , ( temp_output_43_0_g771 * CustomAlphaInputs897 )));
				
				
				finalColor = appendResult384;
				return finalColor;
			}
			ENDCG
		}
	}
	
	
	
}
/*ASEBEGIN
Version=18103
1927;1;1906;1020;4148.027;3949.353;1;True;False
Node;AmplifyShaderEditor.WorldPosInputsNode;943;-3328,-3584;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TextureCoordinatesNode;895;-3328,-3840;Inherit;False;0;892;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;892;-3072,-3840;Inherit;True;Property;_MainTex;MainTex;10;0;Create;False;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;MipBias;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;942;-3072,-3584;Inherit;False;Compute Fog Distance;-1;;751;a5f090963b8f9394a984ee752ce42488;0;1;13;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;913;-2688,-3840;Inherit;False;Handle Tex Alpha;-1;;753;92f31391e7f50294c9c2d8747c81d6b6;0;2;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;894;-3328,-4096;Half;False;Property;_Color;Color;9;0;Create;False;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;896;-2304,-4096;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;897;-2144,-4096;Half;False;CustomAlphaInputs;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;919;-3328,-3200;Float;False;Constant;_Int0;Int 0;5;0;Create;True;0;0;False;0;False;0;0;0;1;INT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;918;-3136,-3200;Half;False;CustomVertexOffset;-1;True;1;0;INT;0;False;1;INT;0
Node;AmplifyShaderEditor.FunctionNode;944;-3328,-4736;Inherit;False;Base;-1;;771;13c50910e5b86de4097e1181ba121e0e;2,116,0,99,0;0;3;FLOAT4;113;FLOAT3;86;FLOAT;87
Node;AmplifyShaderEditor.GetLocalVarNode;898;-3328,-4544;Inherit;False;897;CustomAlphaInputs;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;938;-3008,-4608;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;935;-2816,-4512;Inherit;False;Is Pipeline;1;;864;2b33d0c660fbdb24c98bea96428031b0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;920;-2816,-4608;Inherit;False;918;CustomVertexOffset;1;0;OBJECT;0;False;1;INT;0
Node;AmplifyShaderEditor.RangedFloatNode;890;-2688,-5248;Half;False;Property;_IsHeightFogShader;_IsHeightFogShader;5;1;[HideInInspector];Create;False;0;0;True;0;False;1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;923;-3328,-5248;Half;False;Property;_TITLEE;< TITLEE >;7;0;Create;True;0;0;True;1;StyledBanner(Height Fog Per Object);False;1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;922;-2432,-5248;Float;False;Property;_TransparentQueue;_TransparentQueue;6;1;[HideInInspector];Create;False;0;0;True;0;False;3000;0;0;1;INT;0
Node;AmplifyShaderEditor.RangedFloatNode;879;-2944,-5248;Half;False;Property;_HeightFogPerObject;_HeightFogPerObject;0;1;[HideInInspector];Create;False;0;0;True;0;False;1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;899;-3169,-5248;Half;False;Property;_CUSTOM;[ CUSTOM ];8;0;Create;True;0;0;True;1;StyledCategory(Custom Alpha Inputs);False;1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;936;-2512,-4592;Inherit;False;2;2;0;INT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;384;-2816,-4736;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;383;-2304,-4736;Float;False;True;-1;2;;0;1;BOXOPHOBIC/Atmospherics/Height Fog Per Object;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;True;2;5;False;-1;10;False;-1;4;1;False;-1;1;False;-1;True;0;False;-1;0;False;-1;True;False;True;0;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;594;True;0;False;595;True;False;10;True;890;10;True;890;True;2;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;2;0;False;False;False;False;False;False;False;False;False;True;1;LightMode=ForwardBase;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;1;True;False;;0
Node;AmplifyShaderEditor.CommentaryNode;916;-3328,-3328;Inherit;False;1409.549;100;Custom Vertex Offset / Add here your custom Vertex Offset;0;;0.684,1,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;891;-3328,-4224;Inherit;False;1418.51;100;Custom Alpha Inputs / Add here your custom Alpha Inputs;0;;0.684,1,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;939;-3328,-4864;Inherit;False;1405.154;100;Final Pass;0;;0.684,1,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;880;-3328,-5376;Inherit;False;1406.973;101;Drawers;0;;1,0.475862,0,1;0;0
WireConnection;892;1;895;0
WireConnection;942;13;943;0
WireConnection;913;1;892;4
WireConnection;913;2;942;0
WireConnection;896;0;894;4
WireConnection;896;1;913;0
WireConnection;897;0;896;0
WireConnection;918;0;919;0
WireConnection;938;0;944;87
WireConnection;938;1;898;0
WireConnection;936;0;920;0
WireConnection;936;1;935;0
WireConnection;384;0;944;86
WireConnection;384;3;938;0
WireConnection;383;0;384;0
WireConnection;383;1;936;0
ASEEND*/
//CHKSM=F74943620D06474AE029A1BD15340645926FE0B9