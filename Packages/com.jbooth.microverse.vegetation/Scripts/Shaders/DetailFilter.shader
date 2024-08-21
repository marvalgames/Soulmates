Shader "Hidden/MicroVerse/DetailFilter"
{
    Properties
    {
        [HideInInspector] _Heightmap("Heightmap", 2D) = "black" {}
        [HideInInspector] _Normalmap("Normalmap", 2D) = "black" {}
        [HideInInspector] _PlacementMask("Placement Mask", 2D) = "black" {}
        [HideInInspector] _PlacementSDF("Placement SDF", 2D) = "white" {}
        [HideInInspector] _PlacementSDF2("Placement SDF2", 2D) = "white" {}
        [HideInInspector] _PlacementSDF3("Placement SDF3", 2D) = "white" {}
        [HideInInspector] _Curvemap ("Curvemap", 2D) = "black" {}
        [HideInInspector] _FalloffTexture("Falloff", 2D) = "white" {}
        [HideInInspector] _WeightNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector] _SlopeNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector] _AngleNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector] _CurvatureNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector] _ClearMask("ClearMask", 2D) = "black" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _TEXTUREFILTER
            #pragma shader_feature_local_fragment _ _DENSITYNOISENEEDED
            #include "UnityCG.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Noise.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Filtering.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/SDFFilter.cginc"

            sampler2D _MainTex;
            sampler2D _PlacementMask;
            sampler2D _WeightMap;
            sampler2D _IndexMap;
            sampler2D _ClearMask;
            float _ClearLayer;
            float2 _DensityNoise;
            float2 _DensityNoise2;
            float _Density;
            float4 _MainTex_TexelSize;

            float3 _TextureLayerWeights[32];
            float2 _WeightRange;
            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 stampUV : TEXCOORD1;
            };



            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.stampUV = mul(_Transform, float4(v.uv, 0, 1)).xy;
                return o;
            }

            half frag(v2f i) : SV_Target
            {
                #if _DENSITYNOISENEEDED
                    if (_DensityNoise.x > 0)
                    {
                        float h = abs(Hash12((frac(i.uv * 1500.3147313 + _DensityNoise.y)) * 64));
                        if (h < _DensityNoise.x)
                        {
                           return 0;
                        }
                    }
                #endif

                float2 noiseUV = (i.uv * _NoiseUV.z) + _NoiseUV.xy;

                float maskSample = tex2D(_PlacementMask, i.uv).w;
                float mask = 1.0 - maskSample;
                float sdf = SDFFilter(i.uv);
                
                float result = saturate(DoFilters(i.uv, i.stampUV, noiseUV));

                float texMask = 1;
                #if _TEXTUREFILTER
                    half4 indexes = tex2D(_IndexMap, i.uv) * 32;
                    half4 weights = tex2D(_WeightMap, i.uv);
                    for (int x = 0; x < 4; ++x)
                    {
                        int index = round(indexes[x]);
                        float weight = weights[x];
                        float3 tlw = _TextureLayerWeights[index];
                        texMask -= ((tlw.x * weight) + (tlw.z * weight) * tlw.y);
                    }
                    texMask = saturate(texMask);
                #endif

                float w = result * sdf * mask * texMask;

                if (w < _WeightRange.x || w > _WeightRange.y)
                    w = 0;

                float2 clearMask = tex2D(_ClearMask, i.uv);
                if (round(clearMask.r * 256) > _ClearLayer)
                    w *= 1.0 - clearMask.g;
                
                return saturate(w * _Density);
            }
            ENDCG
        }
    }
}
