Shader "Unlit/Morph"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _Lerp("Lerp", Range(0, 1)) = 0
        _Fade("Fade", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
        }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            // #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 vertex2 : TANGENT;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float _Lerp, _Fade;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(lerp(v.vertex, v.vertex2, _Lerp));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return float4(_Color.rgb, lerp(0, _Color.a, _Fade));
            }
            ENDCG
        }
    }
}