Shader "Unlit/PixelBlend"
{
    Properties
    {
        Albedo ("Albedo", 2D) = "white" {}
        Lights ("Lights", 2DArray) = "" {}
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

            int numLights;
            float lightLevels[64];

            v2f vert (v2f v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv2 = v.uv2;
                return o;
            }

            UNITY_DECLARE_TEX2DARRAY(Lights);

            fixed4 frag (v2f IN) : SV_Target
            {
                float4 albedo = tex2D(Albedo, IN.uv);
                if (numLights == 0)
                {
                    return albedo;
                }
                fixed4 col = 0;
                for(int i=0; i<numLights; i++)
                {
                    fixed4 tex = UNITY_SAMPLE_TEX2DARRAY(Lights, float3(IN.uv2, i));
                    tex.r = tex.r * pow(lightLevels[i], 1.0);
                    tex.b = tex.b * pow(lightLevels[i], 1.2);
                    tex.g = tex.g * pow(lightLevels[i], 1.5);
                    
                    col += lightLevels[i] * tex;
                }
                return albedo * col;
            }
            ENDCG
        }
    }
}
