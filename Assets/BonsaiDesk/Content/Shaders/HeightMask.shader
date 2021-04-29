Shader "Unlit/HeightMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Cutoff", Float) = 0
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 worldSpacePos : TEXCOORD1;
            };

            float _Cutoff;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldSpacePos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                fixed4 col = fixed4(0.25,0.25,0.25,1);
                clip(IN.worldSpacePos.y - _Cutoff);
                col.rgb = GammaToLinearSpace(col);
                return col;
            }
            ENDCG
        }
    }
}
