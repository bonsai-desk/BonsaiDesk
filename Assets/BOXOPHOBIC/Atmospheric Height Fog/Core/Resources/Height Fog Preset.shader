// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "BOXOPHOBIC/Atmospherics/Height Fog Preset"
{
	Properties
	{
		[HideInInspector]_IsStandardPipeline("_IsStandardPipeline", Float) = 0
		[HideInInspector]_HeightFogPreset("_HeightFogPreset", Float) = 1
		[HideInInspector]_IsHeightFogShader("_IsHeightFogShader", Float) = 1
		[StyledBanner(Height Fog Preset)]_TITLE("< TITLE >", Float) = 1
		[StyledCategory(Fog)]_FOGG("[ FOGG]", Float) = 1
		_FogIntensity("Fog Intensity", Range( 0 , 1)) = 1
		[Enum(X Axis,0,Y Axis,1,Z Axis,2)][Space(10)]_FogAxisMode("FogAxisMode", Float) = 1
		_FogColor("Fog Color", Color) = (0.4411765,0.722515,1,1)
		_FogDistanceStart("Fog Distance Start", Float) = 0
		_FogDistanceEnd("Fog Distance End", Float) = 30
		_FogHeightStart("Fog Height Start", Float) = 0
		_FogHeightEnd("Fog Height End", Float) = 5
		[StyledCategory(Skybox)]_SKYBOXX("[ SKYBOXX ]", Float) = 1
		_SkyboxFogHeight("Skybox Fog Height", Range( 0 , 1)) = 1
		_SkyboxFogFill("Skybox Fog Fill", Range( 0 , 1)) = 0
		[StyledCategory(Directional)]_DIRECTIONALL("[ DIRECTIONALL ]", Float) = 1
		[Enum(Off,0,On,1)]_DirectionalMode("Directional Mode", Float) = 1
		[HideInInspector]_DirectionalModeBlend("_DirectionalModeBlend", Float) = 1
		_DirectionalIntensity("Directional Intensity", Range( 0 , 1)) = 1
		_DirectionalColor("Directional Color", Color) = (1,0.7793103,0.5,1)
		[StyledCategory(Noise)]_NOISEE("[ NOISEE ]", Float) = 1
		[Enum(Off,0,Procedural 3D,2)]_NoiseMode("Noise Mode", Float) = 2
		[HideInInspector]_NoiseModeBlend("_NoiseModeBlend", Float) = 1
		_NoiseIntensity("Noise Intensity", Range( 0 , 1)) = 0.5
		_NoiseDistanceEnd("Noise Distance End", Float) = 30
		_NoiseScale("Noise Scale", Float) = 10
		_NoiseSpeed("Noise Speed", Vector) = (0.5,0.5,0,0)

	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Overlay" "Queue"="Overlay" }
	LOD 0

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ColorMask RGBA
		ZWrite Off
		ZTest Always
		
		
		
		Pass
		{
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM

			

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				
			};

			uniform half _FogIntensity;
			uniform half4 _FogColor;
			uniform half _FogDistanceEnd;
			uniform half _FogHeightEnd;
			uniform half _FogHeightStart;
			uniform half _FogDistanceStart;
			uniform half _NOISEE;
			uniform half _TITLE;
			uniform half _DIRECTIONALL;
			uniform half _FogAxisMode;
			uniform half _FOGG;
			uniform half _SKYBOXX;
			uniform half _NoiseModeBlend;
			uniform half _IsHeightFogShader;
			uniform half3 _NoiseSpeed;
			uniform half _NoiseDistanceEnd;
			uniform half _DirectionalModeBlend;
			uniform half _HeightFogPreset;
			uniform half _SkyboxFogFill;
			uniform half3 _FogDirectionalDirection;
			uniform half _NoiseIntensity;
			uniform half _SkyboxFogHeight;
			uniform half _NoiseMode;
			uniform half _DirectionalMode;
			uniform half _DirectionalIntensity;
			uniform half4 _DirectionalColor;
			uniform half _NoiseScale;
			uniform half _IsStandardPipeline;

			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float3 temp_cast_0 = (( _IsStandardPipeline * 0.0 )).xxx;
				
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
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				
				
				finalColor = fixed4(1,1,1,1);
				return finalColor;
			}
			ENDCG
		}
	}
	
	
	
}
/*ASEBEGIN
Version=17602
1927;7;1906;1014;5563.548;6170.532;3.218835;True;False
Node;AmplifyShaderEditor.RangedFloatNode;644;-3328,-4736;Half;False;Property;_HeightFogPreset;_HeightFogPreset;4;1;[HideInInspector];Create;False;0;0;True;0;1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;266;-3040,-3840;Half;False;Property;_SkyboxFogFill;Skybox Fog Fill;17;0;Create;True;0;0;True;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;625;-1536,-4352;Half;False;Global;_FogDirectionalDirection;_FogDirectionalDirection;24;0;Create;True;0;0;True;0;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;634;-2592,-4096;Half;False;Property;_DirectionalModeBlend;_DirectionalModeBlend;20;1;[HideInInspector];Create;True;0;0;True;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;643;-3104,-4736;Half;False;Property;_IsHeightFogShader;_IsHeightFogShader;5;1;[HideInInspector];Create;False;0;0;True;0;1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;227;-3328,-3712;Half;False;Property;_NoiseSpeed;Noise Speed;29;0;Create;True;0;0;True;0;0.5,0.5,0;0.5,0.5,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;349;-2608,-3712;Half;False;Property;_NoiseDistanceEnd;Noise Distance End;27;0;Create;True;0;0;True;0;30;30;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;633;-2864,-4096;Half;False;Property;_DirectionalIntensity;Directional Intensity;21;0;Create;True;0;0;True;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;102;-3328,-4096;Half;False;Property;_DirectionalColor;Directional Color;22;0;Create;True;0;0;True;0;1,0.7793103,0.5,1;1,0.7793103,0.5,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;230;-2384,-3712;Half;False;Property;_NoiseScale;Noise Scale;28;0;Create;True;0;0;True;0;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;640;-3072,-4096;Half;False;Property;_DirectionalMode;Directional Mode;19;1;[Enum];Create;True;2;Off;0;On;1;0;True;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;345;-2896,-3712;Half;False;Property;_NoiseIntensity;Noise Intensity;26;0;Create;True;0;0;True;0;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;88;-3328,-3840;Half;False;Property;_SkyboxFogHeight;Skybox Fog Height;16;0;Create;True;0;0;True;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;639;-3072,-3712;Half;False;Property;_NoiseMode;Noise Mode;24;1;[Enum];Create;True;2;Off;0;Procedural 3D;2;0;True;0;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;635;-2224,-3712;Half;False;Property;_NoiseModeBlend;_NoiseModeBlend;25;1;[HideInInspector];Create;True;0;0;True;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;74;-1776,-4352;Half;False;Property;_FogHeightEnd;Fog Height End;14;0;Create;True;0;0;True;0;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;103;-1984,-4352;Half;False;Property;_FogHeightStart;Fog Height Start;13;0;Create;True;0;0;True;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;106;-2432,-4352;Half;False;Property;_FogDistanceStart;Fog Distance Start;11;0;Create;True;0;0;True;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;107;-2208,-4352;Half;False;Property;_FogDistanceEnd;Fog Distance End;12;0;Create;True;0;0;True;0;30;30;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;646;-1248,-4352;Inherit;False;Is Pipeline;0;;1;2b33d0c660fbdb24c98bea96428031b0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;278;-3072,-4352;Half;False;Property;_FogIntensity;Fog Intensity;8;0;Create;True;0;0;True;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;137;-3328,-4352;Half;False;Property;_FogColor;Fog Color;10;0;Create;True;0;0;True;0;0.4411765,0.722515,1,1;0.4411765,0.722515,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;645;-2752,-4352;Half;False;Property;_FogAxisMode;FogAxisMode;9;1;[Enum];Create;True;3;X Axis;0;Y Axis;1;Z Axis;2;0;True;1;Space(10);1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;626;-3168,-4864;Half;False;Property;_FOGG;[ FOGG];7;0;Create;True;0;0;True;1;StyledCategory(Fog);1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;627;-3024,-4864;Half;False;Property;_SKYBOXX;[ SKYBOXX ];15;0;Create;True;0;0;True;1;StyledCategory(Skybox);1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;628;-2848,-4864;Half;False;Property;_DIRECTIONALL;[ DIRECTIONALL ];18;0;Create;True;0;0;True;1;StyledCategory(Directional);1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;629;-2640,-4864;Half;False;Property;_NOISEE;[ NOISEE ];23;0;Create;True;0;0;True;1;StyledCategory(Noise);1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;632;-2480,-4864;Half;False;Property;_ADVANCEDD;[ ADVANCEDD ];30;0;Create;True;0;0;False;1;StyledCategory(Advanced);1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;558;-3328,-4864;Half;False;Property;_TITLE;< TITLE >;6;0;Create;True;0;0;True;1;StyledBanner(Height Fog Preset);1;1;1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;383;-1088,-4352;Float;False;True;-1;2;;0;1;BOXOPHOBIC/Atmospherics/Height Fog Preset;0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;True;2;5;False;-1;10;False;-1;0;5;False;-1;10;False;-1;True;0;False;-1;0;False;-1;True;False;True;2;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;2;False;594;True;7;False;595;True;False;0;False;500;0;False;500;True;2;RenderType=Overlay=RenderType;Queue=Overlay=Queue=0;True;2;0;False;False;False;False;False;False;False;False;False;True;1;LightMode=ForwardBase;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;1;True;False;;0
Node;AmplifyShaderEditor.CommentaryNode;612;-3328,-4480;Inherit;False;2427.742;100;Props;0;;0.497,1,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;557;-3328,-4992;Inherit;False;1022.024;100;Drawers / Settings;0;;1,0.4980392,0,1;0;0
WireConnection;383;1;646;0
ASEEND*/
//CHKSM=A260606CEE525102D8F23A7007584AC303A498B9