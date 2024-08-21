Shader "Hidden/MicroVerse/TreeSplatMod"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/SplatMerge.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Noise.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Filtering.cginc"

            #pragma shader_feature_local_fragment _ _TEXTUREFILTER
            #pragma shader_feature_local_fragment _ _APPLYFILTER

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _IndexMap;
            sampler2D _WeightMap;
            sampler2D _PlacementMask;
            float4 _IndexMap_TexelSize;
            float _Index;
            sampler2D _TreeSDF;
            float _RealHeight;
            float _Amount;
            float _Width;

            float3 _TextureLayerWeights[32];

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }


            FragmentOutput frag (v2f i)
            {
                float sdf = tex2D(_TreeSDF, i.uv).r * 256;
                float2 stampUV = mul(_Transform, float4(i.uv, 0, 1)).xy;
                float w = 1.0 - saturate(sdf / _Width);

                w = smoothstep(0, 1, w);

                half4 indexes = tex2D(_IndexMap, i.uv) * 32;
                half4 weights = tex2D(_WeightMap, i.uv);

                w *= _Amount;
                float mask = 1.0 - tex2D(_PlacementMask, i.uv).g;
                w *= mask;
                #if _APPLYFILTER
                    w *= saturate(DoFilters(i.uv, stampUV, i.uv * _NoiseUV.z + _NoiseUV.xy));

                
                    #if _TEXTUREFILTER
                        float texMask = 1;
                        for (int itr = 0; itr < 4; ++itr)
                        {
                            int index = round(indexes[itr]);
                            float weight = weights[itr];
                            float3 tlw = _TextureLayerWeights[index];
                            texMask -= ((tlw.x * weight) + (tlw.z * weight) * tlw.y); 
                        }
                        texMask = saturate(texMask);
                        w *= texMask;
                    #endif
                #endif

                // reduce weights of other textures by the amount of this texture

                weights *= 1.0 - w;
                FragmentOutput o = FilterSplatWeights(w, weights, indexes, _Index);
                o.indexMap /= 32;

                return o;
            }
            ENDCG
        }
    }
}
