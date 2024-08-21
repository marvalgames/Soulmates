Shader "Hidden/MicroVerse/PreviewNoiseWorld"
{
    Properties
    {

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


            #pragma shader_feature_local_fragment _ _NOISE _FBM _WORLEY _WORM _WORMFBM _NOISETEXTURE
            #include_with_pragmas "Noise.cginc"

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 pcPixels : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float3 worldPos : TEXCOORD4;
            };

 
            float4 _Param;
            float4 _Param2;
            float2 _NoiseUV;
            sampler2D _NoiseTexture;
            float4 _NoiseTexture_ST;
            float _NoiseChannel;
            float2 _TerrainSize;
            float4 _Color;

            float2 _Remap;

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
                o.worldPos = positionWorld;
                o.uv = brushUV;
                return o;
            }

            float4 Colorize(float a, float b, float t)
            {
                float4 r = (t - a)/(b - a);
                if (r.x <= 0)
                {
                    r.b = abs(r.b);
                    r.rg = 0;
                }
                else if (r.x > 1)
                {
                    r.rgb = lerp(r.rgb, float3(1,0,0), saturate(r.x-1));
                }
                r.a = 0.5;
                return r;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 noiseUV = (i.worldPos.xz/_TerrainSize);
                if (_Param2.x > 0)
                {
                    // TODO: Add all the data to do stuff in stamp space to make this work..
                    return 0; // noiseUV = i.uv;
                }
              
                #if _NOISE
                    return Colorize(_Remap.x, _Remap.y, Noise(noiseUV, _Param));
                #elif _FBM
                    return Colorize(_Remap.x, _Remap.y, NoiseFBM(noiseUV, _Param));
                #elif _WORM
                    return Colorize(_Remap.x, _Remap.y, NoiseWorm(noiseUV, _Param));
                #elif _WORMFBM
                    return Colorize(_Remap.x, _Remap.y, NoiseWormFBM(noiseUV, _Param));
                #elif _WORLEY
                    return Colorize(_Remap.x, _Remap.y, NoiseWorley(noiseUV, _Param));
                #else 
                    return ((tex2D(_NoiseTexture, noiseUV * _NoiseTexture_ST.xy + _NoiseTexture_ST.zw)[_NoiseChannel]) * _Param.y + _Param.w) * _Color;
                #endif
            }
            ENDHLSL
        }
    }
    Fallback Off
}