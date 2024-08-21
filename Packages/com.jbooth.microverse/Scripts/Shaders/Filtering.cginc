            #if _COMPUTESHADER
                #define SAMPLE(tex, samp, uv) tex.SampleLevel(samp, uv, 0)
            #else
                #if _REQUIRELODSAMPLER
                    #define SAMPLE(tex, samp, uv) tex.SampleLevel(samp, uv, 0)
                #else
                    #define SAMPLE(tex, samp, uv) tex.Sample(samp, uv)
                #endif
            #endif

            #pragma shader_feature_local_fragment _ _HEIGHTFILTER
            #pragma shader_feature_local_fragment _ _SLOPEFILTER
            #pragma shader_feature_local_fragment _ _ANGLEFILTER
            #pragma shader_feature_local_fragment _ _CURVATUREFILTER
            #pragma shader_feature_local_fragment _ _FLOWFILTER

            #pragma shader_feature_local_fragment _ _HEIGHTCURVE
            #pragma shader_feature_local_fragment _ _SLOPECURVE
            #pragma shader_feature_local_fragment _ _ANGLECURVE
            #pragma shader_feature_local_fragment _ _CURVATURECURVE
            #pragma shader_feature_local_fragment _ _FLOWCURVE

            #pragma shader_feature_local_fragment _ _HEIGHTNOISE _HEIGHTFBM _HEIGHTWORLEY _HEIGHTNOISETEXTURE _HEIGHTWORM _HEIGHTWORMFBM
            #pragma shader_feature_local_fragment _ _SLOPENOISE _SLOPEFBM _SLOPEWORLEY _SLOPENOISETEXTURE _SLOPEWORM _SLOPEWORMFBM
            #pragma shader_feature_local_fragment _ _WEIGHTNOISE _WEIGHTFBM _WEIGHTWORLEY _WEIGHTNOISETEXTURE _WEIGHTWORM _WEIGHTWORMFBM
            #pragma shader_feature_local_fragment _ _ANGLENOISE _ANGLEFBM _ANGLEWORLEY _ANGLENOISETEXTURE _ANGLEWORM _ANGLEWORMFBM
            #pragma shader_feature_local_fragment _ _CURVATURENOISE _CURVATUREFBM _CURVATUREWORLEY _CURVATURENOISETEXTURE _CURVATUREWORM _CURVATUREWORMFBM
            #pragma shader_feature_local_fragment _ _FLOWNOISE _FLOWFBM _FLOWWORLEY _FLOWNOISETEXTURE _FLOWWORM _FLOWWORMFBM


            #pragma shader_feature_local_fragment _ _WEIGHT2NOISE _WEIGHT2FBM _WEIGHT2WORLEY _WEIGHT2NOISETEXTURE _WEIGHT2WORM _WEIGHT2WORMFBM
            #pragma shader_feature_local_fragment _ _WEIGHT3NOISE _WEIGHT3FBM _WEIGHT3WORLEY _WEIGHT3NOISETEXTURE _WEIGHT3WORM _WEIGHT3WORMFBM

            #pragma shader_feature_local_fragment _ _USEFALLOFF _USEFALLOFFRANGE _USEFALLOFFTEXTURE _USEFALLOFFSPLINEAREA
            #pragma shader_feature_local_fragment _ _FALLOFFSMOOTHSTEP _FALLOFFEASEIN _FALLOFFEASEOUT _FALLOFFEASEINOUT
            #pragma shader_feature_local_fragment _ _FALLOFFNOISE _FALLOFFFBM _FALLOFFWORLEY _FALLOFFWORM _FALLOFFWORMFBM _FALLOFFNOISETEXTURE

            #pragma shader_feature_local_fragment _ _USEFALLOFFPAINTAREA

// for some reason the tree stamp doesn't work with the cached normal map
// I have spent a lot of time trying to figure out why, but to no avail. If I
// recompute the normal based on the height map, however, it works fine. Works
// on every other filter shader, but would prefer to not pay the cost, so
// the tree stamp sets this to make it work. Fugly.

            #pragma shader_feature_local_fragment _ _RECONSTRUCTNORMAL
            #pragma shader_feature_local_fragment _ _CLAMPFALLOFFTEXTURE

            SamplerState shared_linear_clamp;
            SamplerState shader_trilinear_clamp;
            SamplerState shared_linear_repeat;
            Texture2D _Heightmap;
            float4 _Heightmap_TexelSize;
            Texture2D _Normalmap;
            float4 _Normalmap_TexelSize;
            Texture2D _Curvemap;
            Texture2D _Flowmap;
            Texture2D _SlopeNoiseTexture;
            Texture2D _AngleNoiseTexture;
            Texture2D _HeightNoiseTexture;
            Texture2D _WeightNoiseTexture;
            Texture2D _Weight2NoiseTexture;
            Texture2D _Weight3NoiseTexture;
            Texture2D _CurvatureNoiseTexture;
            Texture2D _FlowNoiseTexture;
            Texture2D _FalloffNoiseTexture;

            Texture2D _HeightCurve;
            Texture2D _SlopeCurve;
            Texture2D _AngleCurve;
            Texture2D _CurvatureCurve;
            Texture2D _FlowCurve;

            float4 _HeightNoiseTexture_ST;
            float4 _SlopeNoiseTexture_ST;
            float4 _AngleNoiseTexture_ST;
            float4 _CurvatureNoiseTexture_ST;
            float4 _FlowNoiseTexture_ST;
            float4 _WeightNoiseTexture_ST;
            float4 _Weight2NoiseTexture_ST;
            float4 _Weight3NoiseTexture_ST;
            float4 _FalloffNoiseTexture_ST;

            float _Weight2NoiseOp;
            float _Weight3NoiseOp;

            Texture2D _FalloffTexture;
            float4 _FalloffTexture_TexelSize;
            float2 _FalloffTextureParams;
            float4 _FalloffTextureRotScale;
            int _FalloffTextureChannel;

            float3 _NoiseUV;
            float3 _TerrainSize;
 
            float3 _RealSize;

            float2 _SlopeRange;
            float2 _SlopeSmoothness;
            float4 _SlopeNoise;
            float4 _SlopeNoise2;
            int _SlopeNoiseChannel;
            float _SlopeWeight;

            float2 _AngleRange;
            float2 _AngleSmoothness;
            float4 _AngleNoise;
            float4 _AngleNoise2;
            int _AngleNoiseChannel;
            float _AngleWeight;

            float2 _HeightRange;
            float2 _HeightSmoothness;
            float4 _HeightNoise1;
            float4 _HeightNoise2;
            int _HeightNoiseChannel;
            float _HeightWeight;

            float _Weight;
            float4 _WeightNoise;
            float4 _WeightNoise2;
            int _WeightNoiseChannel;
            float4 _Weight2Noise;
            float4 _Weight2Noise2;
            int _Weight2NoiseChannel;
            float4 _Weight3Noise;
            float4 _Weight3Noise2;
            int _Weight3NoiseChannel;

            float2 _CurvatureRange;
            float2 _CurvatureSmoothness;
            float4 _CurvatureNoise;
            float4 _CurvatureNoise2;
            float _CurvatureMipBias;
            int _CurvatureNoiseChannel;
            float _CurvatureWeight;

            float2 _FlowRange;
            float2 _FlowSmoothness;
            float4 _FlowNoise;
            float4 _FlowNoise2;
            int _FlowNoiseChannel;
            float _FlowWeight;

            float4x4 _Transform;
            float2 _Falloff;
            float _FalloffAreaRange;

            float4 _FalloffNoise;
            float4 _FalloffNoise2;
            int _FalloffNoiseChannel;
            float _FalloffAreaBoost;

            float4x4 _PaintAreaMatrix;
            Texture2D _PaintAreaFalloffTexture;
            float _PaintAreaClamp;

            float2 RotateScaleUV(float2 uv, float2 amt)
            {
                uv -= 0.5;
                uv *= amt.y;
                if (amt.x != 0)
                {
                    float s = sin ( amt.x );
                    float c = cos ( amt.x );
                    float2x2 mtx = float2x2( c, -s, s, c);
                    mtx *= 0.5;
                    mtx += 0.5;
                    mtx = mtx * 2-1;
                    uv = mul ( uv, mtx );
                }
                uv += 0.5;
                return uv;
            }

            float FilterRangeSmoothstep(float2 range, float2 smoothness, float v)
            {
                // widen the range by the smoothness value. This
                // makes it so a height range of 0,5 is 100% at 0 and 5
                smoothness = max(0.00001, smoothness);
                range.x -= smoothness.x;
                range.y += smoothness.y;
                float s1 = smoothstep(range.x, range.x + smoothness.x, v);
                float s2 = 1 - smoothstep(range.y - smoothness.y, range.y, v);
                return s1 * s2;
            }

            
            float RectFalloff(float2 uv, float falloff) 
            {
                if (falloff == 1)
                {
                    if (uv.x <= 0 || uv.y <= 0 || uv.x >= 1 || uv.y >= 1)
                        return 0;
                    return 1;
                }
                uv = saturate(uv);
                uv -= 0.5;
                uv = abs(uv);
                uv = 0.5 - uv;
                falloff = 1 - falloff;
                uv = smoothstep(uv, 0, 0.03 * falloff);
                return min(uv.x, uv.y);
            }

            void ApplyWeightNoise(float2 noiseUV, float2 stampUV, inout float result)
            {
                float2 uv0 = noiseUV;
                float2 uv1 = noiseUV;
                float2 uv2 = noiseUV;

                if (_WeightNoise2.x > 0)
                    uv0 = stampUV;

                if (_Weight2Noise2.x > 0)
                    uv1 = stampUV;

                if (_Weight3Noise2.x > 0)
                    uv2 = stampUV;

                #if _WEIGHTNOISE
                    result = 1 + Noise(uv0, _WeightNoise);
                #elif _WEIGHTFBM
                    result = 1 + NoiseFBM(uv0, _WeightNoise);
                #elif _WEIGHTWORLEY
                    result = 1 + NoiseWorley(uv0, _WeightNoise);
                #elif _WEIGHTWORM
                    result = 1 + NoiseWorm(uv0, _WeightNoise);
                #elif _WEIGHTWORMFBM
                    result = 1 + NoiseWormFBM(uv0, _WeightNoise);
                #elif _WEIGHTNOISETEXTURE
                    result = ((SAMPLE(_WeightNoiseTexture, shared_linear_repeat, uv0 * _WeightNoiseTexture_ST.xy + _WeightNoiseTexture_ST.zw)[_WeightNoiseChannel]) * _WeightNoise.y + _WeightNoise.w);
                #endif

                float result2 = 0;
                #if _WEIGHT2NOISE
                    result2 = Noise(uv1, _Weight2Noise);
                #elif _WEIGHT2FBM
                    result2 = NoiseFBM(uv1, _Weight2Noise);
                #elif _WEIGHT2WORLEY
                    result2 = NoiseWorley(uv1, _Weight2Noise);
                #elif _WEIGHT2WORM
                    result2 = NoiseWorm(uv1, _Weight2Noise);
                #elif _WEIGHT2WORMFBM
                    result2 = NoiseWormFBM(uv1, _Weight2Noise);
                #elif _WEIGHT2NOISETEXTURE
                    result2 = ((SAMPLE(_Weight2NoiseTexture, shared_linear_repeat, uv1 * _Weight2NoiseTexture_ST.xy + _Weight2NoiseTexture_ST.zw)[_Weight2NoiseChannel]) * _Weight2Noise.y + _Weight2Noise.w);
                #endif

                float result3 = 0;
                #if _WEIGHT3NOISE
                    result3 = Noise(uv2, _Weight3Noise);
                #elif _WEIGHT3FBM
                    result3 = NoiseFBM(uv2, _Weight3Noise);
                #elif _WEIGHT3WORLEY
                    result3 = NoiseWorley(uv2, _Weight3Noise);
                #elif _WEIGHT3WORM
                    result3 = NoiseWorm(uv2, _Weight3Noise);
                #elif _WEIGHT3WORMFBM
                    result3 = NoiseWormFBM(uv2, _Weight3Noise);
                #elif _WEIGHT3NOISETEXTURE
                    result3 = ((SAMPLE(_Weight3NoiseTexture, shared_linear_repeat, uv2 * _Weight3NoiseTexture_ST.xy + _Weight3NoiseTexture_ST.zw)[_Weight3NoiseChannel]) * _Weight3Noise.y + _Weight3Noise.w);
                #endif


                #if _WEIGHT2NOISE || _WEIGHT2FBM || _WEIGHT2WORLEY || _WEIGHT2WORM || _WEIGHT2WORMFBM || _WEIGHT2NOISETEXTURE
                    if (_Weight2NoiseOp == 0)
                        result += result2;   
                    else if (_Weight2NoiseOp == 1) 
                        result -= result2;
                    else if (_Weight2NoiseOp == 2)
                        result *= result2;
                    else if (_Weight2NoiseOp == 3)
                        result *= 1 + result2;
                    else if (_Weight2NoiseOp == 4)
                        result = min(result, result2);
                    else
                        result = max(result, result2);
                #endif

                #if _WEIGHT3NOISE || _WEIGHT3FBM || _WEIGHT3WORLEY || _WEIGHT3WORM || _WEIGHT3WORMFBM || _WEIGHT3NOISETEXTURE
                    if (_Weight3NoiseOp == 0)
                        result += result3;   
                    else if (_Weight3NoiseOp == 1) 
                        result -= result3;
                    else if (_Weight3NoiseOp == 2)
                        result *= result3;
                    else if (_Weight3NoiseOp == 3)
                        result *= 1 + result3;
                    else if (_Weight3NoiseOp == 4)
                        result = min(result, result3);
                    else
                        result = max(result, result3);
                #endif
            }

            void ApplyHeightFilter(float2 noiseUV, float2 stampUV, float realHeight, inout float height, inout float result)
            {
                if (_HeightNoise2.x > 0)
                    noiseUV = stampUV;

                #if _HEIGHTFILTER
                    #if _HEIGHTNOISE
                        height += Noise(noiseUV, _HeightNoise1) / realHeight;
                    #elif _HEIGHTFBM
                        height += NoiseFBM(noiseUV, _HeightNoise1) / realHeight;
                    #elif _HEIGHTWORLEY
                        height += NoiseWorley(noiseUV, _HeightNoise1) / realHeight;
                    #elif _HEIGHTWORM
                        height += NoiseWorm(noiseUV, _HeightNoise1) / realHeight;
                    #elif _HEIGHTWORMFBM
                        height += NoiseWormFBM(noiseUV, _HeightNoise1) / realHeight;
                    #elif _HEIGHTNOISETEXTURE
                        height += (((SAMPLE(_HeightNoiseTexture, shared_linear_repeat, noiseUV * _HeightNoiseTexture_ST.xy + _HeightNoiseTexture_ST.zw)[_HeightNoiseChannel])[_HeightNoiseChannel]) * _HeightNoise1.y + _HeightNoise1.z) / realHeight;
                    #endif

                    #if _HEIGHTCURVE
                        float heightResult = lerp(1, SAMPLE(_HeightCurve, shared_linear_clamp, float2(height, 0.5)).r, _HeightWeight);
                    #else
                        float heightResult = lerp(1, FilterRangeSmoothstep(_HeightRange, _HeightSmoothness, height), _HeightWeight);
                    #endif
                    result *= heightResult;
                #endif
            }

            void ApplySlopeAngleFilter(float2 noiseUV, float2 stampUV, float3 normal, inout float result)
            {
                float2 uv0 = noiseUV;
                float2 uv1 = noiseUV;

                if (_SlopeNoise2.x > 0)
                    uv0 = stampUV;

                if (_AngleNoise2.x > 0)
                    uv1 = stampUV;

                #if _SLOPEFILTER
                    float slope = (PI/2) * acos(saturate(dot(normal, float3(0,1,0))));

                    #if _SLOPENOISE
                        slope += Noise(uv0, _SlopeNoise);
                    #elif _SLOPEFBM
                        slope += NoiseFBM(uv0, _SlopeNoise);
                    #elif _SLOPEWORLEY
                        slope += NoiseWorley(uv0, _SlopeNoise);
                    #elif _SLOPEWORM
                        slope += NoiseWorm(uv0, _SlopeNoise);
                    #elif _SLOPEWORMFBM
                        slope += NoiseWormFBM(uv0, _SlopeNoise);
                    #elif _SLOPENOISETEXTURE
                        slope += ((SAMPLE(_SlopeNoiseTexture, shared_linear_repeat, uv0 * _SlopeNoiseTexture_ST.xy + _SlopeNoiseTexture_ST.zw)[_SlopeNoiseChannel]) * _SlopeNoise.y + _SlopeNoise.w);
                    #endif


                    #if _SLOPECURVE
                        float slopeResult = lerp(1, SAMPLE(_SlopeCurve, shared_linear_clamp, float2(slope, 0.5)).r, _SlopeWeight);
                    #else
                        float slopeResult = lerp(1, FilterRangeSmoothstep(_SlopeRange, _SlopeSmoothness, slope), _SlopeWeight);
                    #endif

                    result *= slopeResult;
                #endif

                #if _ANGLEFILTER
                    float angle = atan2(normal.z, normal.x);
                    
                    #if _ANGLENOISE
                        angle += Noise(uv1, _AngleNoise);
                    #elif _ANGLEFBM
                        angle += NoiseFBM(uv1, _AngleNoise);
                    #elif _ANGLEWORLEY
                        angle += NoiseWorley(uv1, _AngleNoise);
                    #elif _ANGLEWORM
                        angle += NoiseWorm(uv1, _AngleNoise);
                    #elif _ANGLEWORMFBM
                        angle += NoiseWormFBM(uv1, _AngleNoise);
                    #elif _ANGLENOISETEXTURE
                        angle += (SAMPLE(_AngleNoiseTexture, shared_linear_repeat, uv1 * _AngleNoiseTexture_ST.xy + _AngleNoiseTexture_ST.zw)[_AngleNoiseChannel] * 2.0 - 1.0) * _AngleNoise.y + _AngleNoise.w;
                    #endif
 
                    #if _ANGLECURVE
                        float angleResult = lerp(1, SAMPLE(_AngleCurve, shared_linear_clamp, float2(angle, 0.5)).r, _AngleWeight);
                    #else
                        float angleResult = lerp(1, FilterRangeSmoothstep(_AngleRange, _AngleSmoothness, angle), _AngleWeight);
                    #endif
                    // prevent directly pointing up stuff, since it has no angle..

                    if (normal.y > 0.9)
                        angleResult = lerp(angleResult, 1, (normal.y-0.9) * 10);

                    result *= angleResult;
                #endif
            }

            
            float ComputeFalloff(float2 uv, float2 stampUV, float2 noiseUV, float noise)
            {
                float falloff = 1;
                #if _USEFALLOFF
                    falloff = RectFalloff(stampUV, saturate(_Falloff.y - noise));
                #elif _USEFALLOFFRANGE
                {
                    float2 off = saturate(_Falloff * 0.5 - saturate(noise) * 0.5);
                    float radius = length( stampUV-0.5 );
 	                falloff = 1.0 - saturate(( radius-off.x ) / max(0.001, ( off.y-off.x )));
                }
                #elif _USEFALLOFFTEXTURE
                {
                    #if _CLAMPFALLOFFTEXTURE
                        float falloffSample = SAMPLE(_FalloffTexture, shared_linear_clamp, stampUV)[_FalloffTextureChannel];
                    #else
                        float falloffSample = SAMPLE(_FalloffTexture, shared_linear_repeat, RotateScaleUV(stampUV, _FalloffTextureRotScale.xy) + _FalloffTextureRotScale.zw)[_FalloffTextureChannel];
                    #endif
                    falloff *= falloffSample;
                    falloff *= _FalloffTextureParams.x;
                    falloff += _FalloffTextureParams.y * falloffSample;
                    falloff *= RectFalloff(stampUV, saturate(_Falloff.y - noise));
                }
                #elif _USEFALLOFFSPLINEAREA
                {
                    float d = SAMPLE(_FalloffTexture, shared_linear_clamp, uv).r - _FalloffAreaBoost;
                    d *= -1;
                    d /= max(0.0001, _FalloffAreaRange - noise);
                    falloff *= saturate(d);
                }
                #endif

                // not else, goes on top..
                #if _USEFALLOFFPAINTAREA
                {
                    float2 worldPosition = noiseUV * _TerrainSize.xz;
                    #if !_SPLATSTAMP
                        worldPosition *= (1000 / _TerrainSize.xz);
                    #endif// * (1000 / _TerrainSize.xz);
                    float3 localPos = mul(_PaintAreaMatrix, float4(worldPosition.x, 0, worldPosition.y, 1)).xyz;
                    float2 uv = float2(localPos.x + 0.5, localPos.z + 0.5);
                    float falloffSample = SAMPLE(_PaintAreaFalloffTexture, shared_linear_clamp, uv).r;
                    falloff *= falloffSample;
                    if (_PaintAreaClamp > 0.5)
                        falloff *= RectFalloff(uv, 1);
                }
                #endif

                #if _FALLOFFSMOOTHSTEP
                    falloff = smoothstep(0,1,falloff);
                #elif _FALLOFFEASEIN
                    falloff *= falloff;
                #elif _FALLOFFEASEOUT
                    falloff = 1 - (1 - falloff) * (1 - falloff);
                #elif _FALLOFFEASEINOUT
                    falloff = falloff < 0.5 ? 2 * falloff * falloff : 1 - pow(-2 * falloff + 2, 2) / 2;
                #endif
                return falloff;
            }

            float DoFilters(float2 uv, float2 stampUV, float2 noiseUV, out float oHFResult)
            {
                oHFResult = 1;
                float result = 1;
                ApplyWeightNoise(noiseUV, stampUV, result);

                #if _HEIGHTFILTER
                    float height = UnpackHeightmap(SAMPLE(_Heightmap, shared_linear_clamp, uv));
                    ApplyHeightFilter(noiseUV, stampUV, _RealSize.y, height, result);
                    oHFResult = result;
                #endif
                

                #if _SLOPEFILTER || _ANGLEFILTER
                    #if _RECONSTRUCTNORMAL
                        float height0 = UnpackHeightmap(SAMPLE(_Heightmap, shared_linear_clamp, uv));
                        float height1 = UnpackHeightmap(SAMPLE(_Heightmap, shared_linear_clamp, uv + float2(_Heightmap_TexelSize.x, 0)));
                        float height2 = UnpackHeightmap(SAMPLE(_Heightmap, shared_linear_clamp, uv + float2(0, _Heightmap_TexelSize.y)));
                        float2 dxy = height0 - float2(height1, height2);

                        dxy = dxy * _Heightmap_TexelSize.zw;
                        float3 normal = normalize(float4( dxy.x, dxy.y, 1.0, height0)).xzy * 0.5 + 0.5;
                    #else
                        float3 normal = SAMPLE(_Normalmap, shared_linear_clamp, uv).xyz;
                        normal.xz *= 2;
                        normal.xz -= 1;
                    #endif
                 
                    ApplySlopeAngleFilter(noiseUV, stampUV, normal, result);
                #endif

                #if _CURVATUREFILTER
                    float curvature = _Curvemap.SampleLevel(shader_trilinear_clamp, uv, _CurvatureMipBias).r;
                    
                    #if _CURVATURENOISE
                        curvature += Noise(noiseUV, _CurvatureNoise);
                    #elif _CURVATUREFBM
                        curvature += NoiseFBM(noiseUV, _CurvatureNoise);
                    #elif _CURVATUREWORLEY
                        curvature += NoiseWorley(noiseUV, _CurvatureNoise);
                    #elif _CURVATUREWORM
                        curvature += NoiseWorm(noiseUV, _CurvatureNoise);
                    #elif _CURVATUREWORMFBM
                        curvature += NoiseWormFBM(noiseUV, _CurvatureNoise);
                    #elif _CURVATURENOISETEXTURE
                        curvature += (SAMPLE(_CurvatureNoiseTexture, shared_linear_repeat, noiseUV * _CurvatureNoiseTexture_ST.xy + _CurvatureNoiseTexture_ST.zw)[_CurvatureNoiseChannel]) * _CurvatureNoise.y + _CurvatureNoise.w;
                    #endif

                    #if _CURVATURECURVE
                        float curveResult = lerp(1, 1.0 - SAMPLE(_CurvatureCurve, shared_linear_clamp, float2(curvature, 0.5)).r, _CurvatureWeight);
                    #else
                        float curveResult = lerp(1, 1.0 - FilterRangeSmoothstep(_CurvatureRange, _CurvatureSmoothness, curvature), _CurvatureWeight);
                    #endif

                    result *= curveResult;
                #endif

                #if _FLOWFILTER
                    float flow = SAMPLE(_Flowmap, shared_linear_clamp, uv).r;
                    
                    #if _FLOWNOISE
                        flow += Noise(noiseUV, _FlowNoise);
                    #elif _FLOWFBM
                        flow += NoiseFBM(noiseUV, _FlowNoise);
                    #elif _FLOWWORLEY
                        flow += NoiseWorley(noiseUV, _FlowNoise);
                    #elif _FLOWWORM
                        flow += NoiseWorm(noiseUV, _FlowNoise);
                    #elif _FLOWWORMFBM
                        flow += NoiseWormFBM(noiseUV, _FlowNoise);
                    #elif _FLOWNOISETEXTURE
                        flow += (SAMPLE(_FlowNoiseTexture, shared_linear_repeat, noiseUV * _FlowNoiseTexture_ST.xy + _FlowNoiseTexture_ST.zw)[_FlowNoiseChannel]) * _FlowNoise.y + _FlowNoise.w;
                    #endif

                    #if _FLOWCURVE
                        float flowResult = lerp(1, 1.0 - SAMPLE(_FlowCurve, shared_linear_clamp, float2(flow, 0.5)).r, _FlowWeight);
                    #else
                        float flowResult = lerp(1, 1.0 - FilterRangeSmoothstep(_FlowRange, _FlowSmoothness, flow), _FlowWeight);
                    #endif

                    result *= flowResult;
                #endif

                float falloffnoise = 0;
                

                float2 falloffuv = noiseUV;
                if (_FalloffNoise2.x > 0)
                    falloffuv = stampUV;

                float falloff = ComputeFalloff(uv, stampUV, noiseUV, falloffnoise);

                #if _FALLOFFNOISE 
                    falloffnoise = Noise(falloffuv, _FalloffNoise);
                #elif _FALLOFFFBM
                    falloffnoise = NoiseFBM(falloffuv, _FalloffNoise);
                #elif _FALLOFFWORLEY
                    falloffnoise = NoiseWorley(falloffuv, _FalloffNoise);
                #elif _FALLOFFWORM
                    falloffnoise = NoiseWorm(falloffuv, _FalloffNoise);
                #elif _FALLOFFWORMFBM
                    falloffnoise = NoiseWormFBM(falloffuv, _FalloffNoise);
                #elif _FALLOFFNOISETEXTURE
                    falloffnoise = (SAMPLE(_FalloffNoiseTexture, shared_linear_repeat, falloffuv * _FalloffNoiseTexture_ST.xy + _FalloffNoiseTexture_ST.zw)[_FalloffNoiseChannel]) * _FalloffNoise.y + _FalloffNoise.w;
                #endif
 

                #if _FALLOFFNOISE || _FALLOFFFBM || _FALLOFFWORLEY || _FALLOFFWORM || _FALLOFFWORMFBM || _FALLOFFNOISETEXTURE
                    falloffnoise *= 1-falloff;
                    falloff = ComputeFalloff(uv, stampUV, falloffuv, falloffnoise);
                #endif
                falloffnoise *= 1.0 - falloff;



                // WTF - I shouldn't have to do this, falloff is already multiplied in
                // from this previously
                #if _USEFALLOFF || _USEFALLOFFTEXTURE
                    falloff *= RectFalloff(stampUV, 1);
                #endif
                

                return result * _Weight * falloff;
            }

            float DoFilters(float2 uv, float2 stampUV, float2 noiseUV)
            {
                float hr = 1;
                return DoFilters(uv, stampUV, noiseUV, hr);
            }



