Shader "Hidden/MicroVerse/HeightmapStamp"
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
            #pragma shader_feature_local_fragment _ _TWIST
            #pragma shader_feature_local_fragment _ _EROSION
            #pragma shader_feature_local_fragment _ _HEIGHTBIAS
            #pragma shader_feature_local_fragment _ _USEORIGINALHEIGHTMAP
            #pragma shader_feature_local_fragment _ _ABSOLUTEHEIGHT
            #pragma shader_feature_local_fragment _ _PASTESTAMP
            #pragma shader_feature_local_fragment _ _USEPOWORTILT
            #pragma shader_feature_local_fragment _ _USEHEIGHTREMAPCUVE

            #pragma shader_feature_local_fragment _ _USEFALLOFFPAINTAREA

            #pragma shader_feature_local _ _FALLOFFSMOOTHSTEP _FALLOFFEASEIN _FALLOFFEASEOUT _FALLOFFEASEINOUT
            #pragma shader_feature_local _ _FALLOFFNOISE _FALLOFFFBM _FALLOFFWORLEY _FALLOFFWORM _FALLOFFWORMFBM _FALLOFFNOISETEXTURE

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
            Texture2D _HeightRemapCurve;
            SamplerState shared_point_clamp;
            
            float2 _HeightRemap;
            float2 _HeightRenorm;
            float _Twist;
            float _Erosion;
            float _ErosionSize;
            float _HeightBias;
            float4 _ScaleOffset;
            float _MipBias;
            float2 _RemapRange;
            float3 _NoiseUV;
            sampler2D _StampTex;
            float4 _StampTex_TexelSize;
            sampler2D _PlacementMask;
            float4x4 _Transform;

            float _AlphaMapSize;
            float3 _RealSize;


            float _Blend;
            float _Invert;
            float _Power;
            float3 _Tilt;
            float2 _TiltScale;
            
            
            
            
            

            sampler2D _OriginalHeights;

            v2f vert(vertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                o.stampUV = mul(_Transform, float4(v.uv, 0, 1)).xy;
                o.stampUV -= 0.5;
                float2 tilt = saturate(abs(_Tilt.zx));
                tilt *= tilt;
                o.stampUV *= _TiltScale > 0.5 ? lerp(1, 3.14, tilt) : 1;
                o.stampUV += 0.5;
                return o;
            }



            float3 GenerateStampNormal(float2 uv, float height, float spread)
            {
                float2 offset = _StampTex_TexelSize.xy * spread;
                float2 uvx = uv + float2(offset.x, 0.0);
                float2 uvy = uv + float2(0.0, offset.y);

                float x = tex2D(_StampTex, uvx).r;
                float y = tex2D(_StampTex, uvy).r;

                float2 dxy = height - float2(x, y);

                dxy = dxy * 1 / offset.xy;
                return normalize(float4( dxy.x, dxy.y, 1.0, height)).xzy * 0.5 + 0.5;
            }


            // radial distort UV coordinates
            float2 RadialUV(float2 uv, float2 center, float str, float2 offset)
            {
                float2 delta = uv - center;
                float delta2 = dot(delta.xy, delta.xy);
                float2 delta_offset = delta2 * str;
                return uv + float2(delta.y, -delta.x) * delta_offset + offset;
            }



            float4 frag(v2f i) : SV_Target
            {
                float4 heightSample = _MainTex.SampleLevel(shared_point_clamp, i.uv, 0);
                bool cp = (i.stampUV.x < 0 || i.stampUV.x > 1 || i.stampUV.y < 0|| i.stampUV.y > 1);
                if (cp)
                    return heightSample;
                float height = UnpackHeightmap(heightSample);

                float2 noiseUV = (i.uv * _NoiseUV.z) + _NoiseUV.xy;
                float2 stampUV = i.stampUV * _ScaleOffset.xy + _ScaleOffset.zw;

                #if _TWIST
                    stampUV = RadialUV(i.stampUV, 0.5, _Twist, 0);
                #endif

                float stamp = tex2Dlod(_StampTex, float4(stampUV, 0, _MipBias)).r;

                stamp = abs(_Invert - stamp);

                #if _USEHEIGHTREMAPCUVE
                   stamp = _HeightRemapCurve.SampleLevel(shared_linear_clamp, float2(stamp, 0), 0);
                #endif
               
                #if _USEPOWORTILT
                    stamp = pow(stamp, _Power);
                    float2 tilt = lerp(float2(-1, -1), float2(1,1), stampUV) * (_Tilt.zx);
                    stamp += tilt.x + tilt.y;
                #endif

                #if _EROSION
                    float3 normal = GenerateStampNormal(stampUV, stamp, _ErosionSize) * 0.3333;
                    normal += GenerateStampNormal(stampUV, stamp, _ErosionSize*3) * 0.3333;
                    normal += GenerateStampNormal(stampUV, stamp, _ErosionSize*7) * 0.3334;
                    
                    float erosNoise = ErosionNoise(stampUV, normal);
                    float erosStr = (1 - normal.y);
                    erosStr *= erosStr;
                    stamp -= erosStr * erosNoise * _Erosion / _RealSize.y;
                #endif

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

                falloff *= 1.0 - tex2D(_PlacementMask, i.uv).x;

                #if _USEORIGINALHEIGHTMAP
                    float originalHeight = UnpackHeightmap(tex2D(_OriginalHeights, i.uv));   
                    return PackHeightmap(originalHeight);
                #endif

                #if _ABSOLUTEHEIGHT
                    stamp *= _HeightRenorm.y;
                #endif

                float newHeight = saturate(_HeightRemap.x + stamp * (_HeightRemap.y - _HeightRemap.x));
          
                float blend = CombineHeight(height, newHeight, _CombineMode);
                return PackHeightmap(clamp(lerp(height, blend, falloff), 0, kMaxHeight));
            }
            ENDCG
        }
    }
}