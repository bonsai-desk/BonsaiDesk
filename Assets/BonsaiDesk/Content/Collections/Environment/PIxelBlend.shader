Shader "Unlit/PixelBlend"
{
    Properties
    {
        Albedo ("Albedo", 2D) = "white" {}
        T1 ("Texture", 2D) = "white" {}
        T2 ("Texture", 2D) = "white" {}
        _TimeScale ("TimeScale", Range(0, 1)) = 0
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

            struct v2f
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };
            
            sampler2D Albedo;
            float4 Albedo_ST;

            sampler2D T1;
            float4 T1_ST;

            sampler2D T2;
            float4 T2_ST;

            float _TimeScale;

            v2f vert (v2f v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv2 = v.uv2;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 fullbright = tex2D(Albedo, i.uv);
                float4 t1 = tex2D(T1, i.uv2);
                float4 t2 = tex2D(T2, i.uv2);
                float _Blend = 0.5 * sin(_Time.y * _TimeScale) + 0.5;
                //fixed4 col =  t1 * _Blend + t2 * (1 - _Blend);
                fixed4 col = fullbright * t1;
                return col;
            }
            ENDCG
        }
    }
}
