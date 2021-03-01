Shader "Unlit/Ambient"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Sky ("Texture", 2D) = "white" {}
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            sampler2D _Sky;
            float4 _Sky_ST;

            v2f vert (appdata v)
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
                float4 mainTex = tex2D(_MainTex, i.uv);
                float4 sky = tex2D(_Sky, i.uv);
                fixed4 col = mainTex * sky;
                return col;
            }
            ENDCG
        }
    }
}
