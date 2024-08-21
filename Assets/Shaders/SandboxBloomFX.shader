Shader "Skatanic/SandboxBloomFX"
{
    HLSLINCLUDE
    #pragma exclude_renderers gles
    #pragma multi_compile_local _ _USE_RGBM

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

    float4 _BlitTexture_TexelSize;

    TEXTURE2D_X(_SourceTexLowMip);
    float4 _SourceTexLowMip_TexelSize;

    float4 _Params; // x: scatter, y: clamp, z: threshold (linear), w: threshold knee

    #define Scatter             _Params.x
    #define ClampMax            _Params.y
    #define Threshold           _Params.z
    #define ThresholdKnee       _Params.w


    float4 _Bloom_Params;
    
    #define bloomIntensity _Bloom_Params.x
    #define tintRed   _Bloom_Params.y
    #define tintGreen  _Bloom_Params.z
    #define tintBlue  _Bloom_Params.w


    half4 EncodeHDR(half3 color)
    {
        #if _USE_RGBM
        half4 outColor = EncodeRGBM(color);
        #else
        half4 outColor = half4(color, 1.0);
        #endif

        #if UNITY_COLORSPACE_GAMMA
        return half4(sqrt(outColor.xyz), outColor.w); // linear to γ
        #else
        return outColor;
        #endif
    }

    half3 DecodeHDR(half4 color)
    {
        #if UNITY_COLORSPACE_GAMMA
        color.xyz *= color.xyz; // γ to linear
        #endif

        #if _USE_RGBM
        return DecodeRGBM(color);
        #else
        return color.xyz;
        #endif
    }

    half4 FragPrefilter(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

        #if defined(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
            uv = RemapFoveatedRenderingResolve(uv);
        #endif

        #if _BLOOM_HQ
            float texelSize = _BlitTexture_TexelSize.x;
            half4 A = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelSize * float2(-1.0, -1.0));
            half4 B = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelSize * float2(0.0, -1.0));
            half4 C = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelSize * float2(1.0, -1.0));
            half4 D = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelSize * float2(-0.5, -0.5));
            half4 E = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelSize * float2(0.5, -0.5));
            half4 F = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelSize * float2(-1.0, 0.0));
            half4 G = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            half4 H = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelSize * float2(1.0, 0.0));
            half4 I = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelSize * float2(-0.5, 0.5));
            half4 J = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelSize * float2(0.5, 0.5));
            half4 K = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelSize * float2(-1.0, 1.0));
            half4 L = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelSize * float2(0.0, 1.0));
            half4 M = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelSize * float2(1.0, 1.0));

            half2 div = (1.0 / 4.0) * half2(0.5, 0.125);

            half4 o = (D + E + I + J) * div.x;
            o += (A + B + G + F) * div.y;
            o += (B + C + H + G) * div.y;
            o += (F + G + L + K) * div.y;
            o += (G + H + M + L) * div.y;

            half3 color = o.xyz;
        #else
        half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).xyz;
        #endif

        // User controlled clamp to limit crazy high broken spec
        color = min(ClampMax, color);

        // Thresholding
        half brightness = Max3(color.r, color.g, color.b);
        half softness = clamp(brightness - Threshold + ThresholdKnee, 0.0, 2.0 * ThresholdKnee);
        softness = (softness * softness) / (4.0 * ThresholdKnee + 1e-4);
        half multiplier = max(brightness - Threshold, softness) / max(brightness, 1e-4);
        color *= multiplier;

        half3 tint = {tintRed, tintGreen, tintGreen};
         
        color *=  tint;

        // Clamp colors to positive once in prefilter. Encode can have a sqrt, and sqrt(-x) == NaN. Up/Downsample passes would then spread the NaN.
        color = max(color, 0);
        return EncodeHDR(color);
    }

    half4 FragBlurH(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float texelSize = _BlitTexture_TexelSize.x * 2.0;
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

        // 9-tap gaussian blur on the downsampled source
        half3 c0 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize * 4.0, 0.0)));
        half3 c1 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize * 3.0, 0.0)));
        half3 c2 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize * 2.0, 0.0)));
        half3 c3 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(texelSize * 1.0, 0.0)));
        half3 c4 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv));
        half3 c5 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize * 1.0, 0.0)));
        half3 c6 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize * 2.0, 0.0)));
        half3 c7 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize * 3.0, 0.0)));
        half3 c8 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(texelSize * 4.0, 0.0)));

        half3 color = c0 * 0.01621622 + c1 * 0.05405405 + c2 * 0.12162162 + c3 * 0.19459459
            + c4 * 0.22702703
            + c5 * 0.19459459 + c6 * 0.12162162 + c7 * 0.05405405 + c8 * 0.01621622;

        return EncodeHDR(color);
    }

    half4 FragBlurV(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float texelSize = _BlitTexture_TexelSize.y;
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

        // Optimized bilinear 5-tap gaussian on the same-sized source (9-tap equivalent)
        half3 c0 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,
                                                uv - float2(0.0, texelSize * 3.23076923)));
        half3 c1 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,
                                                uv - float2(0.0, texelSize * 1.38461538)));
        half3 c2 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv));
        half3 c3 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,
                                                uv + float2(0.0, texelSize * 1.38461538)));
        half3 c4 = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,
                                                uv + float2(0.0, texelSize * 3.23076923)));

        half3 color = c0 * 0.07027027 + c1 * 0.31621622
            + c2 * 0.22702703
            + c3 * 0.31621622 + c4 * 0.07027027;

        return EncodeHDR(color);
    }

    half3 Upsample(float2 uv)
    {
        half3 highMip = DecodeHDR(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv));

        #if _BLOOM_HQ && !defined(SHADER_API_GLES)
        half3 lowMip = DecodeHDR(SampleTexture2DBicubic(TEXTURE2D_X_ARGS(_SourceTexLowMip, sampler_LinearClamp), uv, _SourceTexLowMip_TexelSize.zwxy, (1.0).xx, unity_StereoEyeIndex));
        #else
        half3 lowMip = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTexLowMip, sampler_LinearClamp, uv));
        #endif

        return lerp(highMip, lowMip, Scatter);
    }

    // half4 FragSandboxUberPost(Varyings input) : SV_Target
    // {
    //     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    //
    //     //float2 uv = SCREEN_COORD_APPLY_SCALEBIAS(UnityStereoTransformScreenSpaceTex(input.texcoord));
    //     //float2 uvDistorted = DistortUV(uv);
    //
    //     half3 color = (0.0).xxx;
    //
    //
    //     #if defined(BLOOM)
    //         {
    //             float2 uvBloom = uvDistorted;
    //     #if defined(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    //                 uvBloom = RemapFoveatedRenderingDistort(uvBloom);
    //     #endif
    //
    //     #if _BLOOM_HQ && !defined(SHADER_API_GLES)
    //             half4 bloom = SampleTexture2DBicubic(TEXTURE2D_X_ARGS(_Bloom_Texture, sampler_LinearClamp), SCREEN_COORD_REMOVE_SCALEBIAS(uvBloom), _Bloom_Texture_TexelSize.zwxy, (1.0).xx, unity_StereoEyeIndex);
    //     #else
    //             half4 bloom = SAMPLE_TEXTURE2D_X(_Bloom_Texture, sampler_LinearClamp, SCREEN_COORD_REMOVE_SCALEBIAS(uvBloom));
    //     #endif
    //
    //     #if UNITY_COLORSPACE_GAMMA
    //             bloom.xyz *= bloom.xyz; // γ to linear
    //     #endif
    //
    //             UNITY_BRANCH
    //             if (BloomRGBM > 0)
    //             {
    //                 bloom.xyz = DecodeRGBM(bloom);
    //             }
    //
    //             bloom.xyz *= BloomIntensity;
    //             color += bloom.xyz * BloomTint;
    //
    //     #if defined(BLOOM_DIRT)
    //             {
    //                 // UVs for the dirt texture should be DistortUV(uv * DirtScale + DirtOffset) but
    //                 // considering we use a cover-style scale on the dirt texture the difference
    //                 // isn't massive so we chose to save a few ALUs here instead in case lens
    //                 // distortion is active.
    //                 half3 dirt = SAMPLE_TEXTURE2D(_LensDirt_Texture, sampler_LinearClamp, uvDistorted * LensDirtScale + LensDirtOffset).xyz;
    //                 dirt *= LensDirtIntensity;
    //                 color += dirt * bloom.xyz;
    //             }
    //     #endif
    //         }
    //     #endif
    //
    //     // To save on variants we'll use an uniform branch for vignette. Lower end platforms
    //     // don't like these but if we're running Uber it means we're running more expensive
    //     // effects anyway. Lower-end devices would limit themselves to on-tile compatible effect
    //     // and thus this shouldn't too much of a problem (famous last words).
    //     UNITY_BRANCH
    //
    //
    //     // Color grading is always enabled when post-processing/uber is active
    //     {
    //         //color = ApplyColorGrading(color, PostExposure, TEXTURE2D_ARGS(_InternalLut, sampler_LinearClamp), LutParams, TEXTURE2D_ARGS(_UserLut, sampler_LinearClamp), UserLutParams, UserLutContribution);
    //     }
    //
    //     #if _FILM_GRAIN
    //         {
    //             color = ApplyGrain(color, uv, TEXTURE2D_ARGS(_Grain_Texture, sampler_LinearRepeat), GrainIntensity, GrainResponse, GrainScale, GrainOffset);
    //         }
    //     #endif
    //
    //     // When Unity is configured to use gamma color encoding, we ignore the request to convert to gamma 2.0 and instead fall back to sRGB encoding
    //     #if _GAMMA_20 && !UNITY_COLORSPACE_GAMMA
    //         {
    //             color = LinearToGamma20(color);
    //         }
    //         // Back to sRGB
    //     #elif UNITY_COLORSPACE_GAMMA || _LINEAR_TO_SRGB_CONVERSION
    //         {
    //             color = GetLinearToSRGB(color);
    //         }
    //     #endif
    //
    //     #if _DITHERING
    //         {
    //             color = ApplyDithering(color, uv, TEXTURE2D_ARGS(_BlueNoise_Texture, sampler_PointRepeat), DitheringScale, DitheringOffset);
    //             // Assume color > 0 and prevent 0 - ditherNoise.
    //             // Negative colors can cause problems if fed back to the postprocess via render to FP16 texture.
    //             color = max(color, 0);
    //         }
    //     #endif
    //
    //     #ifdef HDR_ENCODING
    //         {
    //             float4 uiSample = SAMPLE_TEXTURE2D_X(_OverlayUITexture, sampler_PointClamp, input.texcoord);
    //             color.rgb = SceneUIComposition(uiSample, color.rgb, PaperWhite, MaxNits);
    //             color.rgb = OETF(color.rgb);
    //         }
    //     #endif
    //
    //     #if defined(DEBUG_DISPLAY)
    //         half4 debugColor = 0;
    //
    //         if(CanDebugOverrideOutputColor(half4(color, 1), uv, debugColor))
    //         {
    //             return debugColor;
    //         }
    //     #endif
    //
    //     return half4(color, 1.0);
    // }

    half4 FragUpsample(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        half3 color = Upsample(UnityStereoTransformScreenSpaceTex(input.texcoord));
        return EncodeHDR(color);
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        ZTest Always ZWrite Off Cull Off


 
        Pass
        {
            Name "Bloom Prefilter"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragPrefilter
            #pragma multi_compile_local _ _BLOOM_HQ
            #pragma multi_compile_fragment _ _FOVEATED_RENDERING_NON_UNIFORM_RASTER
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Blur Horizontal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlurH
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Blur Vertical"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlurV
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Upsample"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUpsample
            #pragma multi_compile_local _ _BLOOM_HQ
            ENDHLSL
        }

          


//        Pass
//        {
//            Name "SandboxUberPost"
//
//            HLSLPROGRAM
//            #pragma vertex Vert
//            #pragma fragment FragSandboxUberPost
//            ENDHLSL
//        }



    }
}