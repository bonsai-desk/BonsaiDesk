Shader "Custom/TableAlt"
{
	Properties
	{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	}
	SubShader
	{
		Blend SrcAlpha OneMinusSrcAlpha
		Tags{ "RenderType" = "TransparentCutout" "Queue" = "AlphaTest"}
		LOD 200
		
		Pass {
			
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			
			int numHoles;
			float2 holePositions[100];
			float holeRadii[100];
			
			struct appdata
			{
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float4 worldSpacePos : TEXCOORD2;
			};

			struct Input
			{
				float2 uv_MainTex;
				float3 worldPos;
			};
			
			v2f vert (appdata vertex)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(vertex.position);
			    o.worldSpacePos = mul(unity_ObjectToWorld, vertex.position);
				o.uv = vertex.uv;
				o.uv2 = vertex.uv2;
				return o;
			}

			fixed4 frag (v2f IN) : SV_TARGET {
				fixed4 albedo = tex2D(_MainTex, IN.uv);
				
				for (int i = 0; i < numHoles; i++)
				{
					albedo.a *= distance(holePositions[i], IN.worldSpacePos.xz) + 0.0005f >= holeRadii[i];
				}

				clip(albedo.a - 1);
				
				return albedo;
				
			}

			ENDCG
		}
	}
		FallBack "Diffuse"
}
