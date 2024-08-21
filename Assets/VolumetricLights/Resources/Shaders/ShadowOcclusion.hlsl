#ifndef VOLUMETRIC_LIGHTS_CUSTOM_SHADOW
#define VOLUMETRIC_LIGHTS_CUSTOM_SHADOW

#define USE_NATIVE_SHADOWMAP_TEST 0

#if USE_NATIVE_SHADOWMAP_TEST
    TEXTURE2D_SHADOW(_ShadowTexture);
    SAMPLER_CMP(sampler_ShadowTexture);
#else
    TEXTURE2D(_ShadowTexture);
    SAMPLER(sampler_ShadowTexture);
#endif

float4 _ShadowTexture_TexelSize;

float4x4 _ShadowMatrix;

TEXTURECUBE(_ShadowCubemap);
SAMPLER(sampler_ShadowCubemap);

TEXTURE2D(_TranslucencyTexture);
SAMPLER(sampler_TranslucencyTexture);
float4 _TranslucencyTexture_TexelSize;

float4 shadowTextureStart;
float4 shadowTextureEnd;
half3 _ShadowIntensity;
half4 _ShadowColor;

TEXTURE2D(_Cookie2D);
SAMPLER(sampler_Cookie2D);
float4 _Cookie2D_SS;
float2 _Cookie2D_Offset;


inline void ComputeShadowTextureCoords(float3 rayStart, float3 rayDir, float t0, float t1) {
    shadowTextureStart = mul(_ShadowMatrix, float4(rayStart + rayDir * t0, 1.0));
    shadowTextureEnd = mul(_ShadowMatrix, float4(rayStart + rayDir * t1, 1.0));
}

half TestShadowMap(float4 shadowCoords) {

#if USE_NATIVE_SHADOWMAP_TEST
    shadowCoords.z += 0.003;
    return SAMPLE_TEXTURE2D_SHADOW(_ShadowTexture, sampler_ShadowTexture, shadowCoords.xyz);
#else
    float shadowDepth = SAMPLE_TEXTURE2D_LOD(_ShadowTexture, sampler_ShadowTexture, shadowCoords.xy, 1).r;
    #if UNITY_REVERSED_Z
        shadowCoords.z = shadowCoords.w - shadowCoords.z;
        shadowDepth = shadowCoords.w - shadowDepth;
    #endif
    #if VL_POINT
        shadowCoords.z = clamp(shadowCoords.z, -shadowCoords.w, shadowCoords.w);
    #endif
    half shadowTest = shadowCoords.z<0 || shadowDepth > shadowCoords.z;
    return shadowTest;
 #endif
    
}

half3 UnitySpotCookie(float4 lightCoord) {
    half4 cookie = SAMPLE_TEXTURE2D_LOD(_Cookie2D, sampler_Cookie2D, lightCoord.xy * _Cookie2D_SS.xy + _Cookie2D_Offset + _Time.yy * _Cookie2D_SS.zw, 0);
    return cookie.rgb;
}


half3 SampleTranslucency(float4 shadowCoords) {
    float4 translucencyData = SAMPLE_TEXTURE2D_LOD(_TranslucencyTexture, sampler_TranslucencyTexture, shadowCoords.xy, 0); 
    float transpDepth = translucencyData.w;
    #if UNITY_REVERSED_Z
        shadowCoords.z = shadowCoords.w - shadowCoords.z;
        transpDepth = shadowCoords.w - transpDepth;
    #endif
    return transpDepth > shadowCoords.z ? 1.0.xxx : translucencyData.rgb;
}

half3 GetShadowTerm(float4 shadowCoords) {

    #if VL_SHADOWS_TRANSLUCENCY
        half3 s = SampleTranslucency(shadowCoords);
    #elif VL_SPOT_COOKIE
        half3 s = UnitySpotCookie(shadowCoords);
    #else
        half3 s = 1.0.xxx;
    #endif

    #if VL_SHADOWS || VL_SHADOWS_TRANSLUCENCY
	    half sm = TestShadowMap(shadowCoords);
	    sm = sm * _ShadowIntensity.x + _ShadowIntensity.y;
	    s *= sm;
    #endif

    return s;
}

half3 GetShadowAtten(float x) {
    float4 shadowCoords = lerp(shadowTextureStart, shadowTextureEnd, x);
    shadowCoords.xyz /= shadowCoords.w;
    return GetShadowTerm(shadowCoords);
}

half3 GetShadowAttenWS(float3 wpos) {
    float4 shadowCoords = mul(_ShadowMatrix, float4(wpos, 1.0));
    shadowCoords.xyz /= shadowCoords.w;
    return GetShadowTerm(shadowCoords);
}

#if VL_CAST_DIRECT_LIGHT

static const float2 PoissonOffsets[32] = {
	float2(0.06407013, 0.05409927),
	float2(0.7366577, 0.5789394),
	float2(-0.6270542, -0.5320278),
	float2(-0.4096107, 0.8411095),
	float2(0.6849564, -0.4990818),
	float2(-0.874181, -0.04579735),
	float2(0.9989998, 0.0009880066),
	float2(-0.004920578, -0.9151649),
	float2(0.1805763, 0.9747483),
	float2(-0.2138451, 0.2635818),
	float2(0.109845, 0.3884785),
	float2(0.06876755, -0.3581074),
	float2(0.374073, -0.7661266),
	float2(0.3079132, -0.1216763),
	float2(-0.3794335, -0.8271583),
	float2(-0.203878, -0.07715034),
	float2(0.5912697, 0.1469799),
	float2(-0.88069, 0.3031784),
	float2(0.5040108, 0.8283722),
	float2(-0.5844124, 0.5494877),
	float2(0.6017799, -0.1726654),
	float2(-0.5554981, 0.1559997),
	float2(-0.3016369, -0.3900928),
	float2(-0.5550632, -0.1723762),
	float2(0.925029, 0.2995041),
	float2(-0.2473137, 0.5538505),
	float2(0.9183037, -0.2862392),
	float2(0.2469421, 0.6718712),
	float2(0.3916397, -0.4328209),
	float2(-0.03576927, -0.6220032),
	float2(-0.04661255, 0.7995201),
	float2(0.4402924, 0.3640312),
};

float4 GetShadowCoordsWithOffset(float4 shadowCoords, float2 offset) {
    shadowCoords.xy += offset * _ShadowTexture_TexelSize.xy;
    return shadowCoords;
}

half3 GetShadowAttenWS_Soft(float3 wpos) {

    float4 shadowCoords = mul(_ShadowMatrix, float4(wpos, 1.0));
    shadowCoords.xyz /= shadowCoords.w;

    #if VL_SHADOWS_TRANSLUCENCY
        half3 s = SampleTranslucency(shadowCoords);
    #elif VL_SPOT_COOKIE
        half3 s = UnitySpotCookie(shadowCoords);
    #else
        half3 s = 1.0.xxx;
    #endif

    half3 sum = 0;

    #if VL_CAST_DIRECT_LIGHT
        shadowCoords.z += 0.003;
    #endif

   
    for (int k=0;k<DIRECT_LIGHT_SMOOTH_SAMPLES;k++) {
        float2 offset = PoissonOffsets[k] * DIRECT_LIGHT_SMOOTH_RADIUS;
        float4 coords = GetShadowCoordsWithOffset(shadowCoords, offset);
        sum += GetShadowTerm(coords);
    }
    return s * sum / DIRECT_LIGHT_SMOOTH_SAMPLES;
}

#endif // VL_CAST_DIRECT_LIGHT

#if VL_SHADOWS_CUBEMAP

    half GetShadowCubemapAtten(float3 wpos) {
        float3 dir = wpos - LIGHT_POS;
        float shadowDist = SAMPLE_TEXTURECUBE_LOD(_ShadowCubemap, sampler_ShadowCubemap, dir, 0).r;
        float dist = dot2(dir);
        half shadowTest = dist < shadowDist;
        half sm = shadowTest * _ShadowIntensity.x + _ShadowIntensity.y;
        return sm;
    }

    half GetShadowAttenParticlesWS(float3 wpos) {
        return GetShadowCubemapAtten(wpos);
    }
#else
    half3 GetShadowAttenParticlesWS(float3 wpos) {
        return GetShadowAttenWS(wpos);
    }
#endif


#endif // VOLUMETRIC_LIGHTS_CUSTOM_SHADOW


