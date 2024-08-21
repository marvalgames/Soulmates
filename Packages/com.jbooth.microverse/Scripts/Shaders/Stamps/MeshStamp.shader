Shader "Hidden/MicroVerse/MeshStamp"
{
    Properties
    {
        [HideInInspector] _MainTex ("Heightmap Texture", 2D) = "white" {}
        [HideInInspector] _StampTex("Stamp", 2D) = "black" {}
        [HideInInspector] _FalloffTexture("Falloff", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _USEFALLOFF _USEFALLOFFRANGE _USEFALLOFFTEXTURE _USEFALLOFFSPLINEAREA

            #pragma shader_feature_local_fragment _ _USEFALLOFFPAINTAREA

            #pragma shader_feature_local_fragment _ _FALLOFFSMOOTHSTEP _FALLOFFEASEIN _FALLOFFEASEOUT _FALLOFFEASEINOUT
            #pragma shader_feature_local_fragment _ _FALLOFFNOISE _FALLOFFFBM _FALLOFFWORLEY _FALLOFFWORM _FALLOFFWORMFBM _FALLOFFNOISETEXTURE
            #pragma shader_feature_local_fragment _ _SUBTRACT
            #pragma shader_feature_local_fragment _ _CONNECT
            #pragma shader_feature_local_fragment _ _FILLAROUND
            // because unity's height format is stupid and only uses half the possible
            // precision.
            #define kMaxHeight          (32766.0f/65535.0f)

            #include_with_pragmas "UnityCG.cginc"
            #include_with_pragmas "/../Noise.cginc"
            #include_with_pragmas "/../HeightStampFiltering.cginc"

            struct vertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 stampUV: TEXCOORD1;
            };

            Texture2D _MainTex;
            float4 _MainTex_TexelSize;
            SamplerState shared_point_clamp;
            
            float3 _NoiseUV;
            sampler2D _StampTex;
            float4 _StampTex_TexelSize;
            float4x4 _Transform;
            float4 _YBounds;
            float _AlphaMapSize;
            float3 _RealSize;
            sampler2D _OriginalHeights;
            float _BlurSize;
            float3 _HeightScaleClamp;
            float _ConnectHeight;

            v2f vert(vertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                o.stampUV = mul(_Transform, float4(v.uv, 0, 1)).xy;
                return o;
            }

           

            float BlurSample(float2 stampUV, float height)
            {
                float col = 0;
                float totalWeight = 0.0;
                int blurSize = int(_BlurSize);
                float blurSkip = 1 + _BlurSize / 4;
                // Sum all pixels around the current pixel
                float2 ybounds = _YBounds.xy;
                ybounds.y -= ybounds.x;
                ybounds.y *= _HeightScaleClamp.x;
                ybounds.y += ybounds.x;

                float2 uv = stampUV;
                #if _SUBTRACT || _CONNECT || _FILLAROUND             
                uv.y = 1.0 - uv.y;
                #endif

                [loop]
                for (int x = -blurSize; x <= blurSize; x += 1)
                {
                    [loop]
                    for (int y = -blurSize; y <= blurSize; y += 1)
                    {
                        float hs = SAMPLE_DEPTH_TEXTURE(_StampTex, uv + float2(x, y) * _MainTex_TexelSize.xy * blurSkip).r;
                        
                        // clip out taller points of the mesh
                        #if _CONNECT
                        if(hs < _ConnectHeight)
                            hs = 0.0;
                        #endif
            
                        float orig = hs;
                        #if _SUBTRACT || _FILLAROUND || _CONNECT
                            hs = 1 - hs;   
                        #endif

                        hs = clamp(hs, _HeightScaleClamp.y, _HeightScaleClamp.z);
                        hs = (lerp(ybounds.x, ybounds.y, hs) + _YBounds.w) / _RealSize.y;

                        // clip out non-rendered pixels in the depth buffer. This is ok
                        // because we always render with an extra 0.5 meters of space on each end of
                        // the clipping area..
                        if (orig < 0.0001)
                             hs = height;
                        #if _SUBTRACT
                            hs = min(hs, height);
                        #else
                            hs = max(hs, height);
                        #endif

                        col += hs;
                    }
                }
                // Average the colors
                int totalPixels = (2 * blurSize + 1) * (2 * blurSize + 1);
                col /= totalPixels;
    
                return col;

            }


            float4 frag(v2f i) : SV_Target
            {
                float4 heightSample = _MainTex.SampleLevel(shared_point_clamp, i.uv, 0);
                bool cp = (i.stampUV.x < 0 || i.stampUV.x > 1 || i.stampUV.y < 0|| i.stampUV.y > 1);
                if (cp)
                    return heightSample;
                float height = UnpackHeightmap(heightSample);

                float2 noiseUV = (i.uv * _NoiseUV.z) + _NoiseUV.xy;
                float2 stampUV = i.stampUV;

                float2 falloffuv = noiseUV;
                if (_FalloffNoise2.x > 0)
                    falloffuv = stampUV;

                float noise = 0;
                float falloff = ComputeFalloff(i.uv, i.stampUV, noiseUV, 0);

                #if _FALLOFFNOISE
                    noise = (Noise(falloffuv, _FalloffNoise)) / _RealSize.y;
                #elif _FALLOFFFBM
                    noise = (NoiseFBM(falloffuv, _FalloffNoise)) / _RealSize.y;
                #elif _FALLOFFWORLEY
                    noise = (NoiseWorley(falloffuv, _FalloffNoise)) / _RealSize.y;
                #elif _FALLOFFWORM
                    noise = (NoiseWorm(falloffuv, _FalloffNoise)) / _RealSize.y;
                #elif _FALLOFFWORMFBM
                    noise = (NoiseWormFBM(falloffuv, _FalloffNoise)) / _RealSize.y;
                #elif _FALLOFFNOISETEXTURE
                    noise = (tex2D(_FalloffNoiseTexture, falloffuv * _FalloffNoiseTexture_ST.xy + _FalloffNoiseTexture_ST.zw)[_FalloffNoiseChannel] * 2.0 - 1.0) / _RealSize.y * _FalloffNoise.y + _FalloffNoise.w;
                #endif

                

                #if _FALLOFFNOISE || _FALLOFFFBM || _FALLOFFWORLEY || _FALLOFFWORM || _FALLOFFWORMFBM || _FALLOFFNOISETEXTURE
                    noise *= 1-falloff;
                    falloff = ComputeFalloff(i.uv, stampUV, noiseUV, noise);
                #endif

                float depthSample = BlurSample(stampUV, height);
                #if _SUBTRACT
                    float blend = min(height, depthSample);
                #else
                    float blend = max(height, depthSample);
                #endif
                
                    
                #if _FILLAROUND
                    float newHeight = saturate(_YBounds.x) * depthSample;
                    blend = max(height, newHeight);
                    return PackHeightmap(clamp(lerp(height, blend, falloff), 0, kMaxHeight));
                #elif _CONNECT
                    float newHeight = saturate(_YBounds.x) * depthSample;
                    float boundBottom = saturate((_YBounds.x + 0.5f) / _RealSize.y);
                    newHeight = min(newHeight, boundBottom);
                    blend = max(height, newHeight);
                    return PackHeightmap(clamp(lerp(height, blend, falloff), 0, kMaxHeight));
                #endif
    
                return PackHeightmap(clamp(lerp(height, blend, falloff), 0, kMaxHeight));
            }
            ENDCG
        }
    }
}