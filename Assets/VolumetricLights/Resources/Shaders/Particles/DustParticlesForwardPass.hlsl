#ifndef UNIVERSAL_PARTICLES_UNLIT_FORWARD_PASS_INCLUDED
#define UNIVERSAL_PARTICLES_UNLIT_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "../Primitives.hlsl"
#include "../ShadowOcclusion.hlsl"


void InitializeInputData(VaryingsParticle input, half3 normalTS, out InputData output)
{
    output = (InputData)0;

    output.positionWS = input.positionWS.xyz;

    output.normalWS = NormalizeNormalPerPixel(output.normalWS);

    output.fogCoord = (half)input.positionWS.w;
    output.vertexLighting = half3(0.0h, 0.0h, 0.0h);
    output.bakedGI = SampleSHPixel(input.vertexSH, output.normalWS);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

VaryingsParticle vertParticleUnlit(AttributesParticle input)
{
    VaryingsParticle output = (VaryingsParticle)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);

    // position ws is used to compute eye depth in vertFading
    output.positionWS.xyz = vertexInput.positionWS;
    output.positionWS.w = ComputeFogFactor(vertexInput.positionCS.z);
    output.clipPos = vertexInput.positionCS;
    output.color = input.color;

    float3 deltaPos = (float3)(output.positionWS.xyz - _WorldSpaceCameraPos.xyz);
    float distSqr = dot(deltaPos, deltaPos);
    float distAtten =  saturate(_ParticleDistanceAtten / distSqr);
    output.color.a *= distAtten;

    output.texcoord = input.texcoords.xy;

#if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
    output.projectedPosition = ComputeScreenPos(vertexInput.positionCS);
#endif

    return output;
}

half4 fragParticleUnlit(VaryingsParticle input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if VL_CUSTOM_BOUNDS
        if (!TestBounds(input.positionWS.xyz)) return 0;
    #endif

    float2 uv = input.texcoord;
    float3 blendUv = float3(0, 0, 0);

    float4 projectedPosition = float4(0,0,0,0);
#if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
    projectedPosition = input.projectedPosition;
#endif

    half4 albedo = SampleAlbedo(uv, blendUv, _BaseColor, input.color, projectedPosition, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));

    albedo *= _ParticleLightColor;

    albedo.a *= DistanceAttenuation(input.positionWS.xyz);

    albedo.a = saturate(albedo.a);

    #if VL_SHADOWS || VL_SPOT_COOKIE || VL_SHADOWS_CUBEMAP || VL_SHADOWS_TRANSLUCENCY
        albedo.rgb *= GetShadowAttenParticlesWS(input.positionWS.xyz);
    #endif

    half3 result = albedo.rgb;
    half fogFactor = input.positionWS.w;
    result = MixFogColor(result, half3(0, 0, 0), fogFactor);
    return half4(result, albedo.a);
}

#endif // UNIVERSAL_PARTICLES_UNLIT_FORWARD_PASS_INCLUDED
