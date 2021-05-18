Shader "Unlit/VideoCube"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _AccentColor ("Accent Color", Color) = (1, 1, 1, 1)
        _AspectRatio ("Aspect Ratio", float) = 1.7778
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float3 lerpDir : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float light : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color, _AccentColor;
            float _AspectRatio;

            v2f vert (appdata v)
            {
                v2f o;
                
                float ratio = max(_AspectRatio, 1.0);
                ratio = 1.0 / (ratio * 2.0);
                ratio = 0.5 - ratio;
                o.vertex = UnityObjectToClipPos(v.vertex + v.lerpDir * ratio);

                float3 normal = UnityObjectToWorldNormal(v.normal);
                o.light = saturate(dot(normal, normalize(float3(0.25, 0.45, 1)))) * 0.75;
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                col = lerp(col, _AccentColor, step(i.uv.x, 0)) * _Color;
                col *= 0.25 + i.light;
                return fixed4(col.rgb, 1);
            }
            ENDCG
        }
    }
}