#ifndef UNIVERSAL_PARTICLES_UNLIT_INPUT_INCLUDED
#define UNIVERSAL_PARTICLES_UNLIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#if !defined(SHADER_API_PS5) && !defined(SHADER_API_PS4)
CBUFFER_START(UnityPerMaterial)
#endif

float4 _SoftParticleFadeParams;
float4 _CameraFadeParams;
float4 _BaseMap_ST;
half4 _BaseColor;
half4 _BaseColorAddSubDiff;
half _DistortionStrengthScaled;
half _DistortionBlend;

float4 _ConeTipData, _ConeAxis;
float4 _ExtraGeoData;
float3 _BoundsCenter, _BoundsExtents;
float3 _MeshBoundsCenter, _MeshBoundsExtents;
float _Border, _DistanceFallOff;
float3 _FallOff;

half4 _Color;
float4 _AreaExtents;
half4 _ParticleLightColor;
float _ParticleDistanceAtten;
float _NearClipDistance;

#if !defined(SHADER_API_PS5) && !defined(SHADER_API_PS4)
CBUFFER_END
#endif


struct AttributesParticle
{
    float4 vertex : POSITION;
    half4 color : COLOR;
    float2 texcoords : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsParticle
{
    half4 color                     : COLOR;
    float2 texcoord                 : TEXCOORD0;

    float4 positionWS               : TEXCOORD1;
#if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
    float4 projectedPosition        : TEXCOORD6;
#endif

    float3 vertexSH                 : TEXCOORD8; // SH
    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
};

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Particles.hlsl"

#define SOFT_PARTICLE_NEAR_FADE _SoftParticleFadeParams.x
#define SOFT_PARTICLE_INV_FADE_DISTANCE _SoftParticleFadeParams.y

#define CAMERA_NEAR_FADE _CameraFadeParams.x
#define CAMERA_INV_FADE_DISTANCE _CameraFadeParams.y

half4 SampleAlbedo(float2 uv, float3 blendUv, half4 color, float4 particleColor, float4 projectedPosition, TEXTURE2D_PARAM(albedoMap, sampler_albedoMap))
{
    half4 albedo = BlendTexture(TEXTURE2D_ARGS(albedoMap, sampler_albedoMap), uv, blendUv) * color;

    // No distortion Support
    half4 colorAddSubDiff = half4(0, 0, 0, 0);
#if defined (_COLORADDSUBDIFF_ON)
    colorAddSubDiff = _BaseColorAddSubDiff;
#endif
    albedo = MixParticleColor(albedo, particleColor, colorAddSubDiff);

    albedo.rgb = AlphaModulate(albedo.rgb, albedo.a);

#if defined(_SOFTPARTICLES_ON)
    albedo = SOFT_PARTICLE_MUL_ALBEDO(albedo, SoftParticles(SOFT_PARTICLE_NEAR_FADE, SOFT_PARTICLE_INV_FADE_DISTANCE, projectedPosition));
#endif

 #if defined(_FADING_ON)
     ALBEDO_MUL *= CameraFade(CAMERA_NEAR_FADE, CAMERA_INV_FADE_DISTANCE, projectedPosition);
 #endif

    return albedo;
}

#endif // UNIVERSAL_PARTICLES_PBR_INCLUDED
