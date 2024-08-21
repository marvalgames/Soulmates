Shader "Hidden/MicroVerse/DetailPasteStamp"
{
    Properties
    {
        _MainTex("mt", 2D) = "black" {}
        [HideInInspector] _ClearMask("ClearMask", 2D) = "black" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _RINT

            #include "UnityCG.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Noise.cginc"
            #include_with_pragmas "Packages/com.jbooth.microverse/Scripts/Shaders/Filtering.cginc"

            sampler2D _MainTex;
            sampler2D _PlacementMask;
            float4 _MainTex_TexelSize;
            sampler2D _WeightMap;
            sampler2D _IndexMap;
            sampler2D _ClearMask;
            float _ClearLayer;


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

            

            #if _RINT
            int
            #else
            float
            #endif
            frag(v2f i) : SV_Target
            {
                float stamp = tex2D(_MainTex, i.stampUV).r;
                float2 noiseUV = (i.uv * _NoiseUV.z) + _NoiseUV.xy;

                float maskSample = tex2D(_PlacementMask, i.uv).w;
                float mask = 1.0 - maskSample;
                
                float result = saturate(DoFilters(i.uv, i.stampUV, noiseUV));

                float2 clearMask = tex2D(_ClearMask, i.uv);
                if (round(clearMask.r * 256) > _ClearLayer)
                    result *= 1.0 - clearMask.g;

                return stamp * result * mask;
                
            }
            ENDCG
        }
    }
}
