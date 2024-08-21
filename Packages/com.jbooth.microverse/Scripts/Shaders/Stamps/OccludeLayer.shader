Shader "Hidden/MicroVerse/OccludeLayer"
{
    Properties
    {
        _MainTex ("Previous", 2D) = "black" {}
        _Mask("Mask", Vector) = (0,0,0,0) 
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _TEXTUREFILTER
            #pragma shader_feature_local_fragment _ _ISSPLAT
            #include_with_pragmas "UnityCG.cginc"
            #include_with_pragmas "\..\Noise.cginc"
            #include_with_pragmas "\..\Filtering.cginc"

            

            sampler2D _MainTex;
            sampler2D _WeightMap;
            sampler2D _IndexMap;
            float3 _TextureLayerWeights[32];
            float4 _Mask;

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

            float4 frag(v2f i) : SV_Target
            {
                float2 noiseUV = (i.uv * _NoiseUV.z) + _NoiseUV.xy;
                float result = DoFilters(i.uv, i.stampUV, noiseUV);

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

                float4 previous = tex2D(_MainTex, i.uv);

                #if _ISSPLAT
                    return saturate(previous - saturate(saturate(result) * texMask * _Mask.g));
                #endif
                
                
                return saturate(previous + saturate(result) * texMask * _Mask);
                
            }
            ENDCG
        }
    }
}
