Shader "Unlit/NormalTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : TEXTCOORD1;
                float3 worldPos : TEXTCOORD2;
                float4 modelVertex : TEXTCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.modelVertex = v.vertex;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = v.normal;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 p = mul(UNITY_MATRIX_MV, i.modelVertex).xyz;
                p = normalize(p);
                float3 viewDir = p;

                float3 normal = mul(UNITY_MATRIX_MV, float4(i.normal, 0)).xyz;
                normal = normalize(normal);
                float l = 1 - abs(dot(viewDir, normal));
                l = l * l * l * l * l * l * l * l * l * l;
                float edgeLight = l;

                normal = mul(UNITY_MATRIX_M, float4(i.normal, 0)).xyz;
                float light = dot(normal, float3(1, 0, 0));
                light = saturate(light);
                // l = light;

                float3 albedo = tex2D(_MainTex, i.uv).rgb;
                // l = albedo;

                // return float4(albedo * light * edgeLight, 1);
                edgeLight = step(0.01, edgeLight);

                float ambient = 0.4;
                float3 outColor = albedo * (light + ambient);
                return float4(lerp(outColor, float3(0, 0, 0), edgeLight), 1);
            }
            ENDCG
        }
    }
}