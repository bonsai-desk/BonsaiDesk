Shader "Custom/TableAlt"
{
	Properties
	{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 200

			CGPROGRAM
			
			#pragma surface surf Standard fullforwardshadows
			#pragma target 3.0

			sampler2D _MainTex;
			
			int numHoles;
			float2 holePositions[100];
			float holeRadii[100];

			struct Input
			{
				float2 uv_MainTex;
				float3 worldPos;
			};

			void surf(Input IN, inout SurfaceOutputStandard o)
			{
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
				o.Albedo = c.rgb;
				o.Alpha = c.a;

				for (int i = 0; i < numHoles; i++)
				{
					o.Alpha *= distance(holePositions[i], IN.worldPos.xz) + 0.0005f >= holeRadii[i];
				}

				clip(o.Alpha - 1);
			}
			ENDCG
		}
		FallBack "Diffuse"
}
