Shader "Hidden/MicroVerse/ClearFilter"
{
    Properties
    {
        [HideInInspector] _MainTex("orig", 2D) = "black" {}
        [HideInInspector] _Heightmap("Heightmap", 2D) = "black" {}
        [HideInInspector] _Normalmap("Normalmap", 2D) = "black" {}
        [HideInInspector] _Curvemap ("Curvemap", 2D) = "black" {}
        [HideInInspector] _FalloffTexture("Falloff", 2D) = "white" {}
        [HideInInspector] _WeightNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector] _SlopeNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector] _AngleNoiseTexture("Noise", 2D) = "grey" {}
        [HideInInspector] _CurvatureNoiseTexture("Noise", 2D) = "grey" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _TEXTUREFILTER

            #include "UnityCG.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Noise.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Filtering.cginc"

            sampler2D _MainTex;
            sampler2D _WeightMap;
            sampler2D _IndexMap;
            float _LayerIndex;
            float4 _MainTex_TexelSize;

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

            float2 frag(v2f i) : SV_Target
            {
                float2 noiseUV = (i.uv * _NoiseUV.z) + _NoiseUV.xy;

                float result = saturate(DoFilters(i.uv, i.stampUV, noiseUV));
                float2 old = tex2D(_MainTex, i.uv).xy;

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

                float w = result * texMask;
                if (w > 0)
                {
                    old.x = _LayerIndex / 256;
                    old.y = old.y + w;
                }
                return old;
            }
            ENDCG
        }
    }
}
