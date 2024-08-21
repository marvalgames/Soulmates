#ifndef VOLUMETRIC_LIGHTS_COMMONS_URP
#define VOLUMETRIC_LIGHTS_COMMONS_URP

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
#include "Options.hlsl"

#ifndef SHADER_API_PS4
CBUFFER_START(UnityPerMaterial)
#endif

float4 _ConeTipData, _ConeAxis;
half4 _ExtraGeoData;
float3 _BoundsCenter, _BoundsExtents;
float3 _MeshBoundsCenter, _MeshBoundsExtents;
half4 _ToLightDir;

float jitter;
float _NoiseScale;
half _NoiseStrength, _NoiseFinalMultiplier;

float3 _FallOff;
half4 _Color;
float4 _AreaExtents;

float4 _RayMarchSettings;
int _RayMarchMaxSteps;
float4 _WindDirection;
half4 _LightColor;
half _Density;
half _Border, _DistanceFallOff;
float _NearClipDistance;
half3 _DirectLightData;
int _FlipDepthTexture;
float _Downscaling;

#ifndef SHADER_API_PS4
CBUFFER_END
#endif

sampler3D _NoiseTex;

#define FOG_STEPPING _RayMarchSettings.x
#define DITHERING _RayMarchSettings.y
#define JITTERING _RayMarchSettings.z
#define MIN_STEPPING _RayMarchSettings.w

#define DIRECT_LIGHT_MULTIPLIER _DirectLightData.x
#define DIRECT_LIGHT_SMOOTH_SAMPLES _DirectLightData.y
#define DIRECT_LIGHT_SMOOTH_RADIUS _DirectLightData.z

#if VL_CAST_DIRECT_LIGHT_ADDITIVE || VL_CAST_DIRECT_LIGHT_BLEND
    #define VL_CAST_DIRECT_LIGHT 1
#else
    #define VL_CAST_DIRECT_LIGHT 0
#endif

// Common URP code
//#define VR_ENABLED defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED) || defined(SINGLE_PASS_STEREO)

#if defined(USE_ALTERNATE_RECONSTRUCT_API) //|| VR_ENABLED 
   #define CLAMP_RAY_DEPTH(rayStart, uv, t1) ClampRayDepthAlt(rayStart, uv, t1)
#else
   #define CLAMP_RAY_DEPTH(rayStart, uv, t1) ClampRayDepth(rayStart, uv, t1)
#endif

TEXTURE2D(_CustomDepthTexture);
SAMPLER(sampler_CustomDepthTexture);

inline float GetRawDepth(float2 uv) {
    float sceneDepth = SampleSceneDepth(_FlipDepthTexture ? float2(uv.x, 1.0 - uv.y) : uv);
    #if VF2_DEPTH_PREPASS
        float customDepth = SAMPLE_DEPTH_TEXTURE(_CustomDepthTexture, sampler_CustomDepthTexture, uv );
        #if UNITY_REVERSED_Z
            sceneDepth = max(sceneDepth, customDepth);
        #else
            sceneDepth = min(sceneDepth, customDepth);
        #endif
    #endif
    return sceneDepth;
}


inline void ClampRayDepth(float3 rayStart, float2 uv, inout float t1) {

    #if UNITY_REVERSED_Z
        float depth = GetRawDepth(uv);
    #else
        float depth = GetRawDepth(uv);
        depth = depth * 2.0 - 1.0;
    #endif

    float3 wpos = ComputeWorldSpacePosition(uv, depth, unity_MatrixInvVP);

    // World position reconstruction (old code)
/*
    float depth  = GetRawDepth(uv);
    float4 raw   = mul(UNITY_MATRIX_I_VP, float4(uv * 2 - 1, depth, 1));
    float3 wpos  = raw.xyz / raw.w;
*/
    float z = distance(rayStart, wpos);
    t1 = min(t1, z);
} 


// Alternate reconstruct API; URP 7.4 doesn't set UNITY_MATRIX_I_VP correctly in VR, so we need to use this alternate method

inline float GetLinearEyeDepth(float2 uv) {
    float rawDepth = GetRawDepth(uv); // SampleSceneDepth(_FlipDepthTexture ? float2(uv.x, 1.0 - uv.y) : uv);
	float sceneLinearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
    #if defined(ORTHO_SUPPORT)
        #if UNITY_REVERSED_Z
              rawDepth = 1.0 - rawDepth;
        #endif
        float orthoDepth = lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
        sceneLinearDepth = lerp(sceneLinearDepth, orthoDepth, unity_OrthoParams.w);
    #endif
    return sceneLinearDepth;
}


void ClampRayDepthAlt(float3 rayStart, float2 uv, inout float t1) {
    float vz = GetLinearEyeDepth(uv);
    #if defined(ORTHO_SUPPORT)
        if (unity_OrthoParams.w) {
            t1 = min(t1, vz);
            return;
        }
    #endif
    float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
    float2 suv = uv;
    #if UNITY_SINGLE_PASS_STEREO
        // If Single-Pass Stereo mode is active, transform the
        // coordinates to get the correct output UV for the current eye.
        float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
        suv = (suv - scaleOffset.zw) / scaleOffset.xy;
    #endif
    float3 vpos = float3((suv * 2 - 1) / p11_22, -1) * vz;
    float4 wpos = mul(unity_CameraToWorld, float4(vpos, 1));
    float z = distance(rayStart, wpos.xyz);
    t1 = min(t1, z);
}

#if VL_CAST_DIRECT_LIGHT

TEXTURE2D_X(_GBuffer0);
TEXTURE2D_X(_GBuffer1);
TEXTURE2D_X(_GBuffer2);

struct GBufferData {
    half3 albedo, specular, normal;
};


    void GetGBufferData(float2 uv, out GBufferData gbuffer) {
        
        half4 pixelGBuffer0 = SAMPLE_TEXTURE2D_X_LOD(_GBuffer0, sampler_PointClamp, uv, 0);
        gbuffer.albedo = pixelGBuffer0.rgb;

        half3 pixelSpecular = SAMPLE_TEXTURE2D_X_LOD(_GBuffer1, sampler_PointClamp, uv, 0).rgb;

        uint materialFlags = UnpackMaterialFlags(pixelGBuffer0.a);
        if ((materialFlags & kMaterialFlagSpecularSetup) != 0) {
            gbuffer.specular = pixelSpecular;
        } else {
            gbuffer.specular = 0;
        }

        gbuffer.normal = SampleSceneNormals(uv);
    }

#endif

#endif // VOLUMETRIC_LIGHTS_COMMONS_URP

