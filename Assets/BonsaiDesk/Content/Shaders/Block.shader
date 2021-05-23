Shader "Custom/Block"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _BreakTex("Break Texture", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _MaxHealth("MaxHealth", Range(0, 1)) = 1.0
        _DuplicateProgress("Duplicate Progress", Range(0, 1)) = 0.0
        _WholeDeleteProgress("Whole Delete Progress", Range(0, 1)) = 0.0
        _SaveProgress("Save Progress", Range(0, 1)) = 0.0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.5

        sampler2D _MainTex;
        sampler2D _BreakTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv2_BreakTex;
            float3 vertexPos;
            float3 normal;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        half _MaxHealth, _DuplicateProgress, _WholeDeleteProgress, _SaveProgress;

        int numDamagedBlocks;
        float4 damagedBlocks[10];

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.vertexPos = v.vertex.xyz;
            o.normal = v.normal;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            const fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color *
                lerp(fixed4(1, 1, 1, 1), fixed4(0, 0, 1, 1), _DuplicateProgress) *
                lerp(fixed4(1, 1, 1, 1), fixed4(1, 0, 0, 1), _WholeDeleteProgress) *
                lerp(fixed4(1, 1, 1, 1), fixed4(1, 1, 0, 1), _SaveProgress);

            const float3 checkBlockPos = IN.vertexPos.xyz + float3(0.5, 0.5, 0.5) - (IN.normal.xyz * 0.5);;
            const int xc = floor(checkBlockPos.x);
            const int yc = floor(checkBlockPos.y);
            const int zc = floor(checkBlockPos.z);

            //bool blockMatch = false;
            float health = 1.0;

            for (int i = 0; i < numDamagedBlocks; i++)
            {
                const float3 blockDamagePos = damagedBlocks[i].xyz;
                const int xd = floor(blockDamagePos.x);
                const int yd = floor(blockDamagePos.y);
                const int zd = floor(blockDamagePos.z);

                const bool thisBlockMatches = xc == xd && yc == yd && zc == zd;
                const float thisBlockHealth = damagedBlocks[i].w;
                health = lerp(health, thisBlockHealth, thisBlockMatches);
                //blockMatch = blockMatch || thisBlockMatches;
            }

            fixed2 uv2 = IN.uv2_BreakTex;
            health = clamp(health, 0, _MaxHealth);
            health = 1 - health;
            uv2.x += round(health * 10.0) / 11.0;
            fixed4 b = tex2D(_BreakTex, uv2);
            b = lerp(float4(1, 1, 1, 1), float4(b.rgb, 1), b.a);

            //b = lerp(float4(1, 1, 1, 1), b, blockMatch);

            o.Albedo = c.rgb * b.rgb;

            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}