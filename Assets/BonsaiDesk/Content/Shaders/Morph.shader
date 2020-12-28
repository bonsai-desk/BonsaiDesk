Shader "Unlit/Morph"
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
            "Queue" = "Geometry"
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
                float4 color : TEXCOORD0;
            };
            
            float _Lerp, _Alpha;

            v2f vert(appdata v)
            {
                v2f o;
                float4 vertex = lerp(v.vertex, v.vertex2, _Lerp);
                o.vertex = UnityObjectToClipPos(vertex * _Alpha);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}