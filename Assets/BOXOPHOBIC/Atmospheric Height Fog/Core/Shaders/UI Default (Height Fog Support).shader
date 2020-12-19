// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "UI/Default (Height Fog Support)"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

	}

	SubShader
	{
		LOD 0

		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
		
		Stencil
		{
			Ref [_Stencil]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
			CompFront [_StencilComp]
			PassFront [_StencilOp]
			FailFront Keep
			ZFailFront Keep
			CompBack Always
			PassBack Keep
			FailBack Keep
			ZFailBack Keep
		}


		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		
		Pass
		{
			Name "Default"
		CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile __ UNITY_UI_CLIP_RECT
			#pragma multi_compile __ UNITY_UI_ALPHACLIP
			
			#include "UnityShaderVariables.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "UnityStandardBRDF.cginc"
			#pragma multi_compile AHF_DIRECTIONALMODE_OFF AHF_DIRECTIONALMODE_ON
			#pragma multi_compile AHF_NOISEMODE_OFF AHF_NOISEMODE_PROCEDURAL3D
			#pragma multi_compile __ AHF_ENABLED

			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				float4 ase_texcoord2 : TEXCOORD2;
			};
			
			uniform fixed4 _Color;
			uniform fixed4 _TextureSampleAdd;
			uniform float4 _ClipRect;
			uniform sampler2D _MainTex;
			//This is a late directive
			
			uniform float4 _MainTex_ST;
			uniform half4 AHF_FogColor;
			uniform half4 AHF_DirectionalColor;
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
			

			
			v2f vert( appdata_t IN  )
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID( IN );
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
				OUT.worldPosition = IN.vertex;
				float3 ase_worldPos = mul(unity_ObjectToWorld, IN.vertex).xyz;
				OUT.ase_texcoord2.xyz = ase_worldPos;
				
				
				//setting value to unused interpolator channels and avoid initialization warnings
				OUT.ase_texcoord2.w = 0;
				
				OUT.worldPosition.xyz +=  float3( 0, 0, 0 ) ;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = IN.texcoord;
				
				OUT.color = IN.color * _Color;
				return OUT;
			}

			fixed4 frag(v2f IN  ) : SV_Target
			{
				float2 uv_MainTex = IN.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float4 temp_output_4_0 = ( IN.color * ( tex2D( _MainTex, uv_MainTex ) + _TextureSampleAdd ) );
				float3 temp_output_81_0_g1 = (temp_output_4_0).rgb;
				float3 temp_output_2_0_g743 = (AHF_FogColor).rgb;
				float3 gammaToLinear3_g743 = GammaToLinearSpace( temp_output_2_0_g743 );
				#ifdef UNITY_COLORSPACE_GAMMA
				float3 staticSwitch1_g743 = temp_output_2_0_g743;
				#else
				float3 staticSwitch1_g743 = gammaToLinear3_g743;
				#endif
				float3 temp_output_34_0_g703 = staticSwitch1_g743;
				float3 temp_output_2_0_g744 = (AHF_DirectionalColor).rgb;
				float3 gammaToLinear3_g744 = GammaToLinearSpace( temp_output_2_0_g744 );
				#ifdef UNITY_COLORSPACE_GAMMA
				float3 staticSwitch1_g744 = temp_output_2_0_g744;
				#else
				float3 staticSwitch1_g744 = gammaToLinear3_g744;
				#endif
				float3 ase_worldPos = IN.ase_texcoord2.xyz;
				float3 WorldPosition2_g703 = ase_worldPos;
				float3 normalizeResult5_g749 = normalize( ( WorldPosition2_g703 - _WorldSpaceCameraPos ) );
				float3 worldSpaceLightDir = Unity_SafeNormalize(UnityWorldSpaceLightDir(ase_worldPos));
				float dotResult6_g749 = dot( normalizeResult5_g749 , worldSpaceLightDir );
				half DirectionalMask30_g703 = ( (dotResult6_g749*0.5 + 0.5) * AHF_DirectionalIntensity * AHF_DirectionalModeBlend );
				float3 lerpResult40_g703 = lerp( temp_output_34_0_g703 , staticSwitch1_g744 , DirectionalMask30_g703);
				#if defined(AHF_DIRECTIONALMODE_OFF)
				float3 staticSwitch45_g703 = temp_output_34_0_g703;
				#elif defined(AHF_DIRECTIONALMODE_ON)
				float3 staticSwitch45_g703 = lerpResult40_g703;
				#else
				float3 staticSwitch45_g703 = temp_output_34_0_g703;
				#endif
				float3 temp_output_88_86_g1 = staticSwitch45_g703;
				float temp_output_7_0_g748 = AHF_FogDistanceStart;
				half FogDistanceMask12_g703 = saturate( ( ( distance( WorldPosition2_g703 , _WorldSpaceCameraPos ) - temp_output_7_0_g748 ) / ( AHF_FogDistanceEnd - temp_output_7_0_g748 ) ) );
				float3 break12_g705 = ( WorldPosition2_g703 * AHF_FogAxisOption );
				float temp_output_7_0_g706 = AHF_FogHeightEnd;
				half FogHeightMask16_g703 = saturate( ( ( ( break12_g705.x + break12_g705.y + break12_g705.z ) - temp_output_7_0_g706 ) / ( AHF_FogHeightStart - temp_output_7_0_g706 ) ) );
				float temp_output_29_0_g703 = ( FogDistanceMask12_g703 * FogHeightMask16_g703 );
				float simplePerlin3D15_g704 = snoise( ( ( WorldPosition2_g703 * ( 1.0 / AHF_NoiseScale ) ) + ( -AHF_NoiseSpeed * _Time.y ) ) );
				float temp_output_7_0_g709 = AHF_NoiseDistanceEnd;
				half NoiseDistanceMask7_g703 = saturate( ( ( distance( WorldPosition2_g703 , _WorldSpaceCameraPos ) - temp_output_7_0_g709 ) / ( 0.0 - temp_output_7_0_g709 ) ) );
				float lerpResult20_g704 = lerp( 1.0 , (simplePerlin3D15_g704*0.5 + 0.5) , ( NoiseDistanceMask7_g703 * AHF_NoiseIntensity * AHF_NoiseModeBlend ));
				half NoiseSimplex3D24_g703 = lerpResult20_g704;
				#if defined(AHF_NOISEMODE_OFF)
				float staticSwitch42_g703 = temp_output_29_0_g703;
				#elif defined(AHF_NOISEMODE_PROCEDURAL3D)
				float staticSwitch42_g703 = ( temp_output_29_0_g703 * NoiseSimplex3D24_g703 );
				#else
				float staticSwitch42_g703 = temp_output_29_0_g703;
				#endif
				float temp_output_43_0_g703 = ( staticSwitch42_g703 * AHF_FogIntensity );
				float temp_output_88_87_g1 = temp_output_43_0_g703;
				float3 lerpResult82_g1 = lerp( temp_output_81_0_g1 , temp_output_88_86_g1 , temp_output_88_87_g1);
				#ifdef AHF_ENABLED
				float3 staticSwitch64_g1 = lerpResult82_g1;
				#else
				float3 staticSwitch64_g1 = temp_output_81_0_g1;
				#endif
				float4 appendResult9 = (float4(staticSwitch64_g1 , (temp_output_4_0).a));
				
				half4 color = appendResult9;
				
				#ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
				
				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif

				return color;
			}
		ENDCG
		}
	}
	
	
	
}
/*ASEBEGIN
Version=17602
1927;1;1906;1020;600.3783;591.9241;1;True;False
Node;AmplifyShaderEditor.TemplateShaderPropertyNode;2;-512,0;Inherit;False;0;0;_MainTex;Shader;0;5;SAMPLER2D;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;3;-320,0;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TemplateShaderPropertyNode;11;-320,192;Inherit;False;0;0;_TextureSampleAdd;Pass;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;12;-512,-256;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;10;64,64;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;256,-256;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SwizzleNode;6;448,-256;Inherit;False;FLOAT3;0;1;2;3;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;8;640,-256;Inherit;False;Apply Height Fog;-1;;1;950890317d4f36a48a68d150cdab0168;0;1;81;FLOAT3;0,0,0;False;3;FLOAT3;85;FLOAT3;86;FLOAT;87
Node;AmplifyShaderEditor.SwizzleNode;7;448,-160;Inherit;False;FLOAT;3;1;2;3;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;9;896,-256;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;1088,-256;Float;False;True;-1;2;;0;4;UI/Default (Height Fog Support);5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;True;2;5;False;-1;10;False;-1;0;1;False;-1;0;False;-1;False;False;True;2;False;-1;True;True;True;True;True;0;True;-9;True;True;0;True;-5;255;True;-8;255;True;-7;0;True;-4;0;True;-6;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;-1;True;0;True;-11;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;0;False;False;False;False;False;False;False;False;False;False;True;2;0;;0;0;Standard;0;0;1;True;False;;0
WireConnection;3;0;2;0
WireConnection;10;0;3;0
WireConnection;10;1;11;0
WireConnection;4;0;12;0
WireConnection;4;1;10;0
WireConnection;6;0;4;0
WireConnection;8;81;6;0
WireConnection;7;0;4;0
WireConnection;9;0;8;85
WireConnection;9;3;7;0
WireConnection;1;0;9;0
ASEEND*/
//CHKSM=073D17E8AA4BDF4422E647F169433C6BC425DD1E