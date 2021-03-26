Shader "Custom/Halframbert"
{
    Properties
    {

        _Color ("Albedo", Color) = (1,1,1,1)
        _MainTex ("Albedo Texture", 2D) = "white" {}
        _RampTex ("Ramp Texture", 2D) = "white" {}

        _k_s ("[ k_s ] Specular Mask", Range(0,1)) = 1
        _k_s_Tex ("[ k_s ] Specular Mask Texture", 2D) = "white" {}

        _f_s ("[ f_s ] Artist Tuned Fresnel Term", Range(0,5)) = 1
        _k_spec ("[ k_spec ] Exponent", Range(0,255)) = 20
        _k_spec_Tex ("[ k_spec ] Exponent Texture", 2D) = "white" {}

        // "f_r" rim Fresnel term (1 - (n.v))^4 generated below
        _k_r_Tex ("[ k_r ] Rim Mask for Problems", 2D) = "white" {}
        _k_rim ("[ k_rim ] Broad, Constant Exponent", Range(0,5000)) = 1

        _Alpha ("Alpha", Range(0,1)) = 0.5
        _Beta ("Beta", Range(0,1)) = 0.5
        _Gamma ("Gamma", Range(0,2)) = 1

        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
    }
    SubShader
    {
        Blend SrcAlpha OneMinusSrcAlpha

        Tags
        {
            "Queue" = "Geometry"
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ForwardBase"
            }

            Blend [_SrcBlend] [_DstBlend]

            CGPROGRAM
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON

            #pragma vertex MyVertexProgram
            #pragma fragment MyFragmentProgram

            #include "UnityCG.cginc"
            #include "UnityStandardBRDF.cginc"

            float4 _Color;
            sampler2D _MainTex;
            sampler2D _RampTex;
            float4 _MainTex_ST;

            float _k_s;
            sampler2D _k_s_Tex;

            float _f_s;
            float _k_spec;
            sampler2D _k_spec_Tex;

            float _f_r;
            sampler2D _k_r_Tex;
            float _k_rim;

            float _Alpha;
            float _Beta;
            float _Gamma;

            struct VertexData
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            Interpolators MyVertexProgram(VertexData v)
            {
                Interpolators i;
                i.position = UnityObjectToClipPos(v.position);
                i.worldPos = mul(unity_ObjectToWorld, v.position);
                i.uv = TRANSFORM_TEX(v.uv, _MainTex);
                i.normal = normalize(UnityObjectToWorldNormal(v.normal));
                return i;
            }

            float4 MyFragmentProgram(Interpolators i) : SV_TARGET
            {
                i.normal = normalize(i.normal);
                float3 lightDir = _WorldSpaceLightPos0.xyz;
                float3 lightColor = _LightColor0.rgb;

                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 reflectionDir = reflect(-lightDir, i.normal);

                float3 albedo = _Color * tex2D(_MainTex, i.uv).rgb;

                float VdotR = dot(viewDir, reflectionDir);
                float NdotV = dot(i.normal, viewDir);

                float _f_r = pow(1 - NdotV, 4);
                float phongSpec = _f_s * pow(VdotR, _k_spec * tex2D(_k_spec_Tex, i.uv));
                float rimCoeff = _f_r * tex2D(_k_r_Tex, i.uv);
                float phongRim = rimCoeff * pow(VdotR, _k_rim);
                float phong = lightColor * _k_s * tex2D(_k_s_Tex, i.uv) * max(phongSpec, phongRim);

                float3 rim = dot(i.normal, float3(0, 1, 0)) * rimCoeff * ShadeSH9(float4(viewDir, 1));

                float NdotL = dot(lightDir, i.normal);
                float diff = pow(_Alpha * NdotL + _Beta, _Gamma);
                float3 ramp = 2 * tex2D(_RampTex, float2(diff, diff)).rgb;
                float3 litRamp = lightColor * ramp;
                float3 ambient = ShadeSH9(float4(i.normal, 1));
                float3 warpedDiffuse = albedo * (ambient + litRamp);


                return float4(rim + max(0, phong) + warpedDiffuse, _Color.a);
            }
            ENDCG
        }
    }
}