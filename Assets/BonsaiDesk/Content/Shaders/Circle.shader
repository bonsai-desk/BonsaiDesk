Shader "Unlit/Circle"
{
    Properties
    {
        _Color("Color", color) = (1, 1, 1, 1)
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
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                // float4 goPositionWorld : TEXCOORD0;
                float3 vertexPositionLocal : TEXCOORD0;
            };

            float4 _Color;
            float _Alpha;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // o.goPositionWorld = mul(unity_ObjectToWorld, float4(0, 0, 0, 1));
                o.vertexPositionLocal = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // return float4(i.color, _Alpha);
                // float d = distance(i.goPositionWorld.xyz, i.vertexPositionWorld.xyz);
                float d = length(i.vertexPositionLocal);
                float a = step(0.5, 1 - d);
                return float4(_Color.rgb, lerp(0, a, _Alpha));
            }
            ENDCG
        }
    }
}