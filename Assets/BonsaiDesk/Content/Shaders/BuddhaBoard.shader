Shader "Custom/BuddhaBoard"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		// _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
		SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		// Tags { "RenderType"="Opaque" }
		LOD 200
		ZWrite Off

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert alpha:fade

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		// sampler2D _MainTex;

		struct Input
		{
			float2 uv2;
			float2 uv3;
			fixed lerpValue;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void vert(inout appdata_full v, out Input o)
		{
			//time to wait before starting to fade
			half wait = 0.25;

			//time it takes to fade
			half time = 5.0;

			//0 is fully visible, 1 is completly faded
			half lerpValue = saturate((_Time.y - (v.texcoord2.x + wait)) * (1.0 / time));

			//shrink width of line
			v.vertex.xz -= v.texcoord1 * (lerpValue / 2.0);

			//move line down over time to prevent z fighting and have newest line on top
			v.vertex.y -= lerp(0, 0.00009, lerpValue);

			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.lerpValue = lerpValue;
		}

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
			UNITY_INSTANCING_BUFFER_END(Props)

			void surf(Input IN, inout SurfaceOutputStandard o)
			{
			// fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			// fixed4 canvasColor = fixed4(227.0 / 255.0, 234.0 / 255.0, 241.0 / 255.0, 1.0);
			// fixed4 canvasColor = fixed4(0.8901961, 0.9176471, 0.945098, 1.0);
			// fixed4 c = lerp(_Color, canvasColor, IN.lerpValue);
			// fixed4 c = canvasColor;

			fixed4 c = _Color;
			o.Albedo = c.rgb;

			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;

			o.Alpha = saturate(1.0 - IN.lerpValue);
			// o.Alpha = 1.0;
			// o.Alpha = c.a;
		}
		ENDCG
	}
		FallBack "Diffuse"
}