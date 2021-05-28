Shader "Unlit/BlockObject"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _TextureArray("Texture Array", 2Darray) = "" {}
        _BreakTex("Break Texture", 2D) = "white" {}
        _MaxHealth("MaxHealth", Range(0, 1)) = 1.0
        _DuplicateProgress("Duplicate Progress", Range(0, 1)) = 0.0
        _WholeDeleteProgress("Whole Delete Progress", Range(0, 1)) = 0.0
        _SaveProgress("Save Progress", Range(0, 1)) = 0.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
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
                float3 uv_MainTex : TEXCOORD0;
                float2 uv_BreakTex : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 vertexPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 uv_MainTex : TEXCOORD3;
                float2 uv_BreakTex : TEXCOORD4;
            };

            sampler2D _BreakTex;

            fixed4 _Color;
            half _MaxHealth, _DuplicateProgress, _WholeDeleteProgress, _SaveProgress;

            int numDamagedBlocks;
            float4 damagedBlocks[10];

            UNITY_DECLARE_TEX2DARRAY(_TextureArray);

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertexPos = v.vertex.xyz;
                o.normal = v.normal;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv_MainTex = v.uv_MainTex;
                o.uv_BreakTex = v.uv_BreakTex;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 albedo = UNITY_SAMPLE_TEX2DARRAY(_TextureArray, i.uv_MainTex) * _Color *
                    lerp(fixed4(1, 1, 1, 1), fixed4(0, 0, 1, 1), _DuplicateProgress) *
                    lerp(fixed4(1, 1, 1, 1), fixed4(1, 0, 0, 1), _WholeDeleteProgress) *
                    lerp(fixed4(1, 1, 1, 1), fixed4(1, 1, 0, 1), _SaveProgress);

                const float3 normal = normalize(i.normal);
                const float3 worldNormal = normalize(i.worldNormal);
                
                const float3 checkBlockPos = i.vertexPos + float3(0.5, 0.5, 0.5) - (normal * 0.5);
                const int xc = floor(checkBlockPos.x);
                const int yc = floor(checkBlockPos.y);
                const int zc = floor(checkBlockPos.z);

                //bool blockMatch = false;
                float health = 1.0;

                for (int n = 0; n < numDamagedBlocks; n++)
                {
                    const float3 blockDamagePos = damagedBlocks[n].xyz;
                    const int xd = floor(blockDamagePos.x);
                    const int yd = floor(blockDamagePos.y);
                    const int zd = floor(blockDamagePos.z);

                    const bool thisBlockMatches = xc == xd && yc == yd && zc == zd;
                    const float thisBlockHealth = damagedBlocks[n].w;
                    health = lerp(health, thisBlockHealth, thisBlockMatches);
                }

                fixed2 uv2 = i.uv_BreakTex;
                health = clamp(health, 0, _MaxHealth);
                health = 1 - health;
                uv2.x += round(health * 10.0) / 11.0;
                fixed4 b = tex2D(_BreakTex, uv2);
                b = lerp(float4(1, 1, 1, 1), float4(b.rgb, 1), b.a);

                const float lightDir = float3(-0.5, -1, -0.1);
                const float lightCompare = -normalize(lightDir);
                float light = saturate(dot(worldNormal, lightCompare));
                light = 0.8 + 0.2 * light;

                albedo.rgb = albedo.rgb * b.rgb * light;

                return albedo;
            }
            ENDCG
        }
    }
}