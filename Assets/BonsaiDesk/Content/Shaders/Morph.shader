﻿Shader "Unlit/Morph"
{
    Properties
    {
        _Lerp("Lerp", Range(0, 1)) = 0
        _Alpha("Alpha", Range(0, 1)) = 1
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

            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float4 vertex2 : TANGENT;
                fixed4 color : COLOR;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 color : TEXCOORD0;
            };
            
            float _Lerp, _Alpha;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(lerp(v.vertex, v.vertex2, _Lerp));
                o.color = v.color.rgb;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return float4(i.color, _Alpha);
            }
            ENDCG
        }
    }
}