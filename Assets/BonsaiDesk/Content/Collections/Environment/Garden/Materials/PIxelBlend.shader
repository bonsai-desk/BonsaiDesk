Shader "Unlit/PixelBlend"
{
    Properties
    {
        Albedo ("Albedo", 2D) = "white" {}
        Lights ("Lights", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "Queue" = "Geometry"
        }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            sampler2D Albedo;
            float4 Albedo_ST;

            int numLights;
            float lightLevels[64];

            int numHoles;
            float2 holePositions[100];
            float holeRadii[100];

            static const float tableThickness = 0.03;
            static const float tolerance = 0.01;
            static const float tableHeight = 0.724 - (tableThickness / 2.0);

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.position);
                o.worldSpacePos = mul(unity_ObjectToWorld, v.position);
                o.uv = v.uv;
                o.uv2 = v.uv2;
                return o;
            }

            UNITY_DECLARE_TEX2DARRAY(Lights);

            fixed4 frag(v2f IN) : SV_Target
            {
                //calculate lighting
                fixed4 col = 0;
                int i;
                for (i = 0; i < numLights; i++)
                {
                    fixed4 tex = UNITY_SAMPLE_TEX2DARRAY(Lights, float3(IN.uv2, i));
                    tex.r = tex.r * pow(lightLevels[i], 1.0);
                    tex.b = tex.b * pow(lightLevels[i], 1.2);
                    tex.g = tex.g * pow(lightLevels[i], 1.5);

                    col += lightLevels[i] * tex;
                }

                //discard fragment if in hole
                int alpha = 1;
                for (i = 0; i < numHoles; i++)
                {
                    bool fragmentInHole = distance(holePositions[i], IN.worldSpacePos.xz) + 0.0005f < holeRadii[i];
                    bool fragmentInHeight = abs(IN.worldSpacePos.y - tableHeight) <= tableThickness / 2.0 + tolerance;
                    alpha *= !(fragmentInHole && fragmentInHeight);
                }
                clip(alpha - 1);

                //calculate return color
                float4 albedo = tex2D(Albedo, IN.uv);
                float4 returnColor = albedo * col;
                return lerp(returnColor, albedo, numLights == 0);
            }
            ENDCG
        }
    }
}