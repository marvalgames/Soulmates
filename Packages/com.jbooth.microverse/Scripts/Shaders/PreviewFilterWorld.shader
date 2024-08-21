Shader "Hidden/MicroVerse/PreviewFilterWorld"
{
    Properties
    {
        _Control0("", 2D) = "black" {}
        _Control1("", 2D) = "black" {}
        _Control2("", 2D) = "black" {}
        _Control3("", 2D) = "black" {}
        _Control4("", 2D) = "black" {}
        _Control5("", 2D) = "black" {}
        _Control6("", 2D) = "black" {}
        _Control7("", 2D) = "black" {}
    }
    SubShader
    {
        Cull Back ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE

        #include_with_pragmas "UnityCG.cginc"
        #include_with_pragmas "TerrainPreview.cginc"

        
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ _HEIGHTFILTER _SLOPEFILTER _ANGLEFILTER _CURVATUREFILTER _FLOWFILTER _TEXTUREFILTER

            #pragma shader_feature_local_fragment _ _USECURVE

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 pcPixels : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            sampler2D _Normalmap;
            Texture2D _Curve;
            Texture2D _Curvemap;
            Texture2D _Flowmap;
            SamplerState shared_linear_clamp;
            SamplerState shared_trilinear_clamp;
            float4 _Normalmap_TexelSize;
            float4 _Heightmap_TexelSize;
            float2 _HeightRange;
            float2 _HeightSmoothness;
            float2 _SlopeRange;
            float2 _SlopeSmoothness;
            float2 _CurvatureRange;
            float2 _CurvatureSmoothness;
            float2 _FlowRange;
            float2 _FlowSmoothness;
            float2 _AngleRange;
            float2 _AngleSmoothness;
            float4 _Color;
            float _MipBias;

            float3 _TextureLayerWeights[32];

            sampler _Control0;
            sampler _Control1;
            sampler _Control2;
            sampler _Control3;
            sampler _Control4;
            sampler _Control5;
            sampler _Control6;
            sampler _Control7;

            float FilterRangeSmoothstep(float2 range, float2 smoothness, float v)
            {
                // widen the range by the smoothness value. This
                // makes it so a height range of 0,5 is 100% at 0 and 5

                range.x -= smoothness.x;
                range.y += smoothness.y;
                float s1 = smoothstep(range.x, range.x + smoothness.x, v);
                float s2 = 1 - smoothstep(range.y - smoothness.y, range.y, v);
                return s1 * s2;
            }

            Varyings vert(uint vid : SV_VertexID)
            {
                Varyings o;
                
                // build a quad mesh, with one vertex per paint context pixel (pcPixel)

                float2 pcPixels = BuildProceduralQuadMeshVertex(vid);

                // compute heightmap UV and sample heightmap
                float2 heightmapUV = PaintContextPixelsToHeightmapUV(pcPixels);
                float heightmapSample = UnpackHeightmap(tex2Dlod(_Heightmap, float4(heightmapUV, 0, 0)));

                // compute brush UV
                float2 brushUV = PaintContextPixelsToBrushUV(pcPixels);

                // compute object position (in terrain space) and world position

                float3 positionObject = PaintContextPixelsToObjectPosition(pcPixels, heightmapSample);
                float3 positionWorld = TerrainObjectToWorldPosition(positionObject);

                o.pcPixels = pcPixels;
                o.positionCS = UnityWorldToClipPos(positionWorld);
                o.uv = brushUV;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float result = 0;
                #if _SLOPEFILTER || _ANGLEFILTER
                    float height0 = UnpackHeightmap(tex2D(_Heightmap, uv));
                    float height1 = UnpackHeightmap(tex2D(_Heightmap, uv + float2(_Heightmap_TexelSize.x, 0)));
                    float height2 = UnpackHeightmap(tex2D(_Heightmap, uv + float2(0, _Heightmap_TexelSize.y)));
                    float2 dxy = height0 - float2(height1, height2);

                    dxy = dxy * _Heightmap_TexelSize.zw;
                    float3 normal = normalize(float4( dxy.x, dxy.y, 1.0, height0)).xzy * 0.5 + 0.5;
                #endif

                #if _SLOPEFILTER
                    float slope = (3.14159265359/2) * acos(saturate(dot(normal, float3(0,1,0))));
                    #if _USECURVE
                        float slopeResult = _Curve.Sample(shared_linear_clamp, float2(slope, 0.5)).r;
                    #else
                        float slopeResult = FilterRangeSmoothstep(_SlopeRange, _SlopeSmoothness, slope);
                    #endif
                    result = slopeResult;
                #endif

                #if _ANGLEFILTER
                    float angle = atan2(normal.z, normal.x);
                    #if _USECURVE
                        float angleResult = _Curve.Sample(shared_linear_clamp, float2(angle, 0.5)).r;
                    #else
                        float angleResult = FilterRangeSmoothstep(_AngleRange, _AngleSmoothness, angle);
                    #endif
                    if (normal.y > 0.9)
                        angleResult = lerp(angleResult, 1, (normal.y-0.9) * 10);

                    result = angleResult;
                #endif

                #if _CURVATUREFILTER
                    float cav1 = _Curvemap.SampleLevel(shared_trilinear_clamp, i.uv, _MipBias).r;
                    #if _USECURVE
                        float curveResult = _Curve.Sample(shared_linear_clamp, float2(cav1, 0.5)).r;
                    #else
                        float curveResult = 1.0 - FilterRangeSmoothstep(_CurvatureRange, _CurvatureSmoothness, cav1);
                    #endif
                    result = curveResult;
                #endif

                #if _FLOWFILTER
                    float cav1 = _Flowmap.Sample(shared_linear_clamp, i.uv).r;
                    #if _USECURVE
                        float curveResult = _Curve.Sample(shared_linear_clamp, float2(cav1, 0.5)).r;
                    #else
                        float curveResult = 1.0 - FilterRangeSmoothstep(_FlowRange, _FlowSmoothness, cav1);
                    #endif
                    result = curveResult;
                #endif

                #if _HEIGHTFILTER
                    float height = UnpackHeightmap(tex2D(_Heightmap, uv));
                    #if _USECURVE
                        float heightResult = _Curve.Sample(shared_linear_clamp, float2(height, 0.5)).r;
                    #else
                        float heightResult = FilterRangeSmoothstep(_HeightRange, _HeightSmoothness, height);
                    #endif
                    result = heightResult;
                #endif

                #if _TEXTUREFILTER
                    // ugh, unity's crap format since we're already baked
                    float texMask = 1;
                    float weights[32];
                    float4 sample0 = tex2D(_Control0, i.uv);
                    float4 sample1 = tex2D(_Control1, i.uv);
                    float4 sample2 = tex2D(_Control2, i.uv);
                    float4 sample3 = tex2D(_Control3, i.uv);
                    float4 sample4 = tex2D(_Control4, i.uv);
                    float4 sample5 = tex2D(_Control5, i.uv);
                    float4 sample6 = tex2D(_Control6, i.uv);
                    float4 sample7 = tex2D(_Control7, i.uv);
                    weights[0] = sample0.x;
                    weights[1] = sample0.y;
                    weights[2] = sample0.z;
                    weights[3] = sample0.w;
                    weights[4] = sample1.x;
                    weights[5] = sample1.y;
                    weights[6] = sample1.z;
                    weights[7] = sample1.w;
                    weights[8] = sample2.x;
                    weights[9] = sample2.y;
                    weights[10] = sample2.z;
                    weights[11] = sample2.w;
                    weights[12] = sample3.x;
                    weights[13] = sample3.y;
                    weights[14] = sample3.z;
                    weights[15] = sample3.w;
                    weights[16] = sample4.x;
                    weights[17] = sample4.y;
                    weights[18] = sample4.z;
                    weights[19] = sample4.w;
                    weights[20] = sample5.x;
                    weights[21] = sample5.y;
                    weights[22] = sample5.z;
                    weights[23] = sample5.w;
                    weights[24] = sample6.x;
                    weights[25] = sample6.y;
                    weights[26] = sample6.z;
                    weights[27] = sample6.w;
                    weights[28] = sample7.x;
                    weights[29] = sample7.y;
                    weights[30] = sample7.z;
                    weights[31] = sample7.w;

                    for (int x = 0; x < 32; ++x)
                    {
                        float3 tlw = _TextureLayerWeights[x];
                        texMask -= ((tlw.x * weights[x]) + (tlw.z * weights[x]) * tlw.y); 
                    }
                    result = saturate(texMask);
                #endif

                result = saturate(result);
                return _Color * result;
                
            }
            ENDHLSL
        }
    }
    Fallback Off
}