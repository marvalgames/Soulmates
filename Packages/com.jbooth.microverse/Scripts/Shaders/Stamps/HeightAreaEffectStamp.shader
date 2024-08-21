Shader "Hidden/MicroVerse/HeightAreaEffectStamp"
{
    Properties
    {
        [HideInInspector] _MainTex ("Heightmap Texture", 2D) = "white" {}
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

            #pragma shader_feature_local_fragment _ _TERRACE _REMAP _BEACH
            #pragma shader_feature_local_fragment _ _NOISENOISE _NOISEFBM _NOISEWORLEY _NOISENOISETEXTURE _NOISEWORM _NOISEWORMFBM
            

            // because unity's height format is stupid and only uses half the possible
            // precision.
            #define kMaxHeight          (32766.0f/65535.0f)

            #include_with_pragmas "UnityCG.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Noise.cginc"
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _RemapCurve;

            sampler2D _NoiseNoiseTexture;
            float4 _NoiseNoiseTexture_ST;
            float4 _NoiseNoise;
            float4 _NoiseNoise2;
            int _NoiseNoiseChannel;

            
            float3 _NoiseUV;
            float _TerraceSize;
            float _TerraceStrength;
            float _SmoothSize;
            float _BeachDistance;
            float _BeachPower;
            float _WorldPosY;
 
            
            float4x4 _Transform;

            
            
            
            float3 _RealSize;

            


            float GetNoise(float2 noiseUV, float2 stampUV)
            {
                float2 uv0 = noiseUV;

                if (_NoiseNoise2.x > 0)
                    uv0 = stampUV;

                float result = 0;
                #if _NOISENOISE
                    result = Noise(uv0, _NoiseNoise);
                #elif _NOISEFBM
                    result = NoiseFBM(uv0, _NoiseNoise);
                #elif _NOISEWORLEY
                    result = NoiseWorley(uv0, _NoiseNoise);
                #elif _NOISEWORM
                    result = NoiseWorm(uv0, _NoiseNoise);
                #elif _NOISEWORMFBM
                    result = NoiseWormFBM(uv0, _NoiseNoise);
                #elif _NOISENOISETEXTURE
                    result = ((tex2D(_NoiseNoiseTexture, uv0 * _NoiseNoiseTexture_ST.xy + _NoiseNoiseTexture_ST.zw)[_NoiseNoiseChannel]) * _NoiseNoise.y + _NoiseNoise.w);
                #endif
                return result;
            }


            v2f vert(vertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                o.stampUV = mul(_Transform, float4(v.uv, 0, 1)).xy;
                return o;
            }

            

            float4 frag(v2f i) : SV_Target
            {
                float4 heightSample = tex2D(_MainTex, i.uv);
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

                float newHeight = height;
                #if _TERRACE
                    float scaledHeight = height * _RealSize.y / _TerraceSize;
				    newHeight = round(scaledHeight) * _TerraceSize / _RealSize.y;
                    newHeight = lerp(height, newHeight, _TerraceStrength);
                #elif _BEACH
                    float scaledHeight = height * _RealSize.y;
                    float dist = abs(scaledHeight - _WorldPosY);
                    dist /= _BeachDistance;
                    dist = saturate(dist);
                    dist = pow(dist, _BeachPower);
                    newHeight = lerp(_WorldPosY, scaledHeight, dist);
                    newHeight /= _RealSize.y;
                #elif _REMAP
                    newHeight = tex2D(_RemapCurve, float2(height, 0));
                #endif


                #if _NOISENOISE || _NOISEFBM || _NOISEWORLEY || _NOISENOISETEXTURE || _NOISEWORM || _NOISEWORMFBM
                    newHeight = CombineHeight(newHeight, GetNoise(i.uv, stampUV) / _RealSize.y, _CombineMode);
                #endif

                return PackHeightmap(clamp(lerp(height, newHeight, falloff), 0, kMaxHeight));
            }
            ENDCG
        }
    }
}