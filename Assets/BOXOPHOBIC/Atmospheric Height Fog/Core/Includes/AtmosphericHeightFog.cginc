/*

// Add the following directives to your shader for directional and noise support

#include "Assets/BOXOPHOBIC/Atmospheric Height Fog/Core/Library/AtmosphericHeightFog.cginc"
#pragma multi_compile AHF_NOISEMODE_OFF AHF_NOISEMODE_PROCEDURAL3D

// Apply Atmospheric Height Fog to transparent shaders like this
// Where finalColor is the shader output color, fogParams.rgb is the fog color and fogParams.a is the fog mask

float4 fogParams = GetAtmosphericHeightFog(i.worldPos);
return ApplyAtmosphericHeightFog(finalColor, fogParams);

*/

#ifndef ATMOSPHERIC_HEIGHT_FOG_INCLUDED
#define ATMOSPHERIC_HEIGHT_FOG_INCLUDED

#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"

uniform half4 AHF_FogColorStart;
uniform half4 AHF_FogColorEnd;
uniform half AHF_FogDistanceStart;
uniform half AHF_FogDistanceEnd;
uniform half AHF_FogDistanceFalloff;
uniform half AHF_FogColorDuo;
uniform half4 AHF_DirectionalColor;
uniform half3 AHF_DirectionalDir;
uniform half AHF_DirectionalIntensity;
uniform half AHF_DirectionalFalloff;
uniform half3 AHF_FogAxisOption;
uniform half AHF_FogHeightEnd;
uniform half AHF_FogHeightStart;
uniform half AHF_FogHeightFalloff;
uniform half AHF_FogLayersMode;
uniform half AHF_NoiseScale;
uniform half3 AHF_NoiseSpeed;
uniform half AHF_NoiseDistanceEnd;
uniform half AHF_NoiseIntensity;
uniform half AHF_NoiseModeBlend;
uniform half AHF_FogIntensity;

float3 mod3D289(float3 x) { return x - floor(x / 289.0) * 289.0; }
float4 mod3D289(float4 x) { return x - floor(x / 289.0) * 289.0; }
float4 permute(float4 x) { return mod3D289((x * 34.0 + 1.0) * x); }
float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - r * 0.85373472095314; }

float snoise(float3 v)
{
	const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
	float3 i = floor(v + dot(v, C.yyy));
	float3 x0 = v - i + dot(i, C.xxx);
	float3 g = step(x0.yzx, x0.xyz);
	float3 l = 1.0 - g;
	float3 i1 = min(g.xyz, l.zxy);
	float3 i2 = max(g.xyz, l.zxy);
	float3 x1 = x0 - i1 + C.xxx;
	float3 x2 = x0 - i2 + C.yyy;
	float3 x3 = x0 - 0.5;
	i = mod3D289(i);
	float4 p = permute(permute(permute(i.z + float4(0.0, i1.z, i2.z, 1.0)) + i.y + float4(0.0, i1.y, i2.y, 1.0)) + i.x + float4(0.0, i1.x, i2.x, 1.0));
	float4 j = p - 49.0 * floor(p / 49.0);  // mod(p,7*7)
	float4 x_ = floor(j / 7.0);
	float4 y_ = floor(j - 7.0 * x_);  // mod(j,N)
	float4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
	float4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;
	float4 h = 1.0 - abs(x) - abs(y);
	float4 b0 = float4(x.xy, y.xy);
	float4 b1 = float4(x.zw, y.zw);
	float4 s0 = floor(b0) * 2.0 + 1.0;
	float4 s1 = floor(b1) * 2.0 + 1.0;
	float4 sh = -step(h, 0.0);
	float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
	float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
	float3 g0 = float3(a0.xy, h.x);
	float3 g1 = float3(a0.zw, h.y);
	float3 g2 = float3(a1.xy, h.z);
	float3 g3 = float3(a1.zw, h.w);
	float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
	g0 *= norm.x;
	g1 *= norm.y;
	g2 *= norm.z;
	g3 *= norm.w;
	float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
	m = m * m;
	m = m * m;
	float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
	return 42.0 * dot(m, px);
}

// Returns the fog color and alpha based on world position
float4 GetAtmosphericHeightFog(float3 positionWS)
{
	float4 finalColor;

	float3 WorldPosition = positionWS;

	float3 WorldPosition2_g1 = WorldPosition;
	float temp_output_7_0_g860 = AHF_FogDistanceStart;
	half FogDistanceMask12_g1 = pow(abs(saturate(((distance(WorldPosition2_g1, _WorldSpaceCameraPos) - temp_output_7_0_g860) / (AHF_FogDistanceEnd - temp_output_7_0_g860)))), AHF_FogDistanceFalloff);
	float3 lerpResult258_g1 = lerp((AHF_FogColorStart).rgb, (AHF_FogColorEnd).rgb, (saturate((FogDistanceMask12_g1 - 0.5)) * AHF_FogColorDuo));
	float3 normalizeResult318_g1 = normalize((WorldPosition2_g1 - _WorldSpaceCameraPos));
	float dotResult145_g1 = dot(normalizeResult318_g1, AHF_DirectionalDir);
	half DirectionalMask30_g1 = pow(abs(((dotResult145_g1*0.5 + 0.5) * AHF_DirectionalIntensity)), AHF_DirectionalFalloff);
	float3 lerpResult40_g1 = lerp(lerpResult258_g1, (AHF_DirectionalColor).rgb, DirectionalMask30_g1);
	float3 temp_output_2_0_g859 = lerpResult40_g1;
	float3 gammaToLinear3_g859 = GammaToLinearSpace(temp_output_2_0_g859);
#ifdef UNITY_COLORSPACE_GAMMA
	float3 staticSwitch1_g859 = temp_output_2_0_g859;
#else
	float3 staticSwitch1_g859 = gammaToLinear3_g859;
#endif
	float3 temp_output_256_0_g1 = staticSwitch1_g859;
	half3 AHF_FogAxisOption181_g1 = AHF_FogAxisOption;
	float3 break159_g1 = (WorldPosition2_g1 * AHF_FogAxisOption181_g1);
	float temp_output_7_0_g861 = AHF_FogHeightEnd;
	half FogHeightMask16_g1 = pow(abs(saturate((((break159_g1.x + break159_g1.y + break159_g1.z) - temp_output_7_0_g861) / (AHF_FogHeightStart - temp_output_7_0_g861)))), AHF_FogHeightFalloff);
	float lerpResult328_g1 = lerp((FogDistanceMask12_g1 * FogHeightMask16_g1), saturate((FogDistanceMask12_g1 + FogHeightMask16_g1)), AHF_FogLayersMode);
	float simplePerlin3D193_g1 = snoise(((WorldPosition2_g1 * (1.0 / AHF_NoiseScale)) + (-AHF_NoiseSpeed * _Time.y)));
	float temp_output_7_0_g863 = AHF_NoiseDistanceEnd;
	half NoiseDistanceMask7_g1 = saturate(((distance(WorldPosition2_g1, _WorldSpaceCameraPos) - temp_output_7_0_g863) / (0.0 - temp_output_7_0_g863)));
	float lerpResult198_g1 = lerp(1.0, (simplePerlin3D193_g1*0.5 + 0.5), (NoiseDistanceMask7_g1 * AHF_NoiseIntensity * AHF_NoiseModeBlend));
	half NoiseSimplex3D24_g1 = lerpResult198_g1;
#if defined(AHF_NOISEMODE_OFF)
	float staticSwitch42_g1 = lerpResult328_g1;
#elif defined(AHF_NOISEMODE_PROCEDURAL3D)
	float staticSwitch42_g1 = (lerpResult328_g1 * NoiseSimplex3D24_g1);
#else
	float staticSwitch42_g1 = lerpResult328_g1;
#endif
	float temp_output_43_0_g1 = (staticSwitch42_g1 * AHF_FogIntensity);
	float4 appendResult114_g1 = (float4(temp_output_256_0_g1, temp_output_43_0_g1));


	finalColor = appendResult114_g1;
	return finalColor;
}

// Applies the fog
float3 ApplyAtmosphericHeightFog(float3 color, float4 fog)
{
	return float3(lerp(color.rgb, fog.rgb, fog.a));
}

float4 ApplyAtmosphericHeightFog(float4 color, float4 fog)
{
	return float4(lerp(color.rgb, fog.rgb, fog.a), color.a);
}

#endif
