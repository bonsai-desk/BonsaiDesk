Shader "Unlit/Morph"
{
    Properties
    {
        _Color("Color", color) = (1, 1, 1, 1)
        _Paused("Paused", Range(0, 1)) = 0
        _Visibility("Visibility", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            //            "Queue" = "Transparent"
        }
        LOD 100

        Pass
        {
            //            ZWrite Off
            //            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
            float _Paused, _Visibility;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(lerp(v.vertex, v.vertex2, _Paused) * _Visibility);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}