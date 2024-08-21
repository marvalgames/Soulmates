Shader "Hidden/MicroVerse/SplatFilter"
{
    Properties
    {
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
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define _SPLATSTAMP 1

            #include_with_pragmas "UnityCG.cginc"
            #include_with_pragmas "/../Noise.cginc"
            #include_with_pragmas "/../Filtering.cginc"
            #include_with_pragmas "/../SplatMerge.cginc"

            float _Channel;
            sampler2D _IndexMap;
            sampler2D _WeightMap;
            sampler2D _PlacementMask;
            float2 _AlphaMapSize;

            


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

            FragmentOutput frag(v2f i)
            {
                half4 weightMap = tex2D(_WeightMap, i.uv);
                half4 indexMap = round(tex2D(_IndexMap, i.uv) * 32);

                // acount for pixel centering
                float2 noiseUV = i.uv;
                noiseUV *= _AlphaMapSize;
                noiseUV += lerp(-0.5, 0.5, i.uv);
                noiseUV /= _AlphaMapSize;
                noiseUV *= _NoiseUV.z;
                noiseUV += _NoiseUV.xy;
                // adjust filter UVs to center
                float2 uvOffset = 1.0 / _AlphaMapSize * 0.5;
                float result = saturate(DoFilters(i.uv - uvOffset, i.stampUV, noiseUV));
                FragmentOutput o = FilterSplatWeights(result, weightMap, indexMap, _Channel);
                o.indexMap /= 32;
                return o;
            }
            ENDCG
        }
    }
}