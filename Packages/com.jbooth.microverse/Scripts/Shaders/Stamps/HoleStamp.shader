Shader "Hidden/MicroVerse/HoleStamp"
{
    Properties
    {
        [HideInInspector]_MainTex("MainTex", 2D) = "black" {}
        [HideInInspector]_PasteTex("PasteTex", 2D) = "black" {}
        [HideInInspector]_IndexMap ("Texture", 2D) = "black" {}
        [HideInInspector]_WeightMap ("Texture", 2D) = "red" {}
        [HideInInspector]_Heightmap ("Heightmap", 2D) = "black" {}
        [HideInInspector]_Normalmap ("Normalmap", 2D) = "black" {}
        [HideInInspector]_Curvemap ("Curvemap", 2D) = "black" {}
        [HideInInspector]_FalloffTexture("Falloff", 2D) = "white" {}
        [HideInInspector]_WeightNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector]_SlopeNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector]_AngleNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector]_CurvatureNoiseTexture("Noise", 2D) = "grey" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _TEXTUREFILTER
            #pragma shader_feature_local_fragment _ _COPYPASTE
        
            #include_with_pragmas "UnityCG.cginc"
            #include_with_pragmas "/../Noise.cginc"
            #include_with_pragmas "/../Filtering.cginc"


            float _Channel;
            sampler2D _WeightMap;
            sampler2D _IndexMap;
            Texture2D _MainTex;
            Texture2D _PasteTex;
            SamplerState my_linear_clamp_sampler;

            float3 _TextureLayerWeights[32];


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


            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 noiseUV = (i.uv * _NoiseUV.z) + _NoiseUV.xy;
                float result = (DoFilters(uv, i.stampUV, noiseUV));
                float texMask = 1;
                #if _TEXTUREFILTER
                    half4 indexes = tex2D(_IndexMap, uv) * 32;
                    half4 weights = tex2D(_WeightMap, uv);
                    for (int itr = 0; itr < 4; ++itr)
                    {
                        int index = round(indexes[itr]);
                        float weight = weights[itr];
                        float3 tlw = _TextureLayerWeights[index];
                        texMask -= ((tlw.x * weight) + (tlw.z * weight) * tlw.y); 
                    }
                    texMask = saturate(texMask);
                #endif
                result *= texMask;
                #if _COPYPASTE
                    float4 existing = _MainTex.Sample(my_linear_clamp_sampler, i.uv);
                    float4 stamp = _PasteTex.Sample(my_linear_clamp_sampler, i.stampUV);
                    return min(existing, stamp * (1-result));
                #else
                    float4 existing = _MainTex.Sample(my_linear_clamp_sampler, uv);
                #endif

                
   
                return lerp(existing, 0, result);
                
            }
            ENDCG
        }
    }
}
