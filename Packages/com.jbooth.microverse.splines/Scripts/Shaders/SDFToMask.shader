Shader "Hidden/MicroVerse/SDFToMask"
{
    Properties
    {
        _MainTex ("Heightmap Texture", 2D) = "white" {}
        _SplineSDF("Spline SDF", 2D) = "black" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #pragma shader_feature_local _ _TREATASAREA

            #pragma shader_feature_local_fragment _ _SPLINECURVETREEWEIGHT
            #pragma shader_feature_local_fragment _ _SPLINECURVEDETAILWEIGHT

            struct vertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _SplineSDF;
            sampler2D _SplineTreeWeight;
            sampler2D _SplineDetailWeight;

            float _TreeWidth;
            float _TreeSmoothness;
            float _DetailWidth;
            float _DetailSmoothness;
            float _HeightWidth;
            float _HeightSmoothness;
            float _SplatWidth;
            float _SplatSmoothness;
            

            v2f vert(vertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {

                float4 curMask = tex2D(_MainTex, i.uv);
                float2 data = tex2D(_SplineSDF, i.uv).xy;
                

                float height = 1.0 - smoothstep(_HeightWidth, _HeightWidth + _HeightSmoothness, data.g);
                float splat = 1.0 - smoothstep(_SplatWidth, _SplatWidth + _SplatSmoothness, data.g);
                float tree = 1.0 - smoothstep(_TreeWidth, _TreeWidth + _TreeSmoothness, data.g);
                float detail = 1.0 - smoothstep(_DetailWidth, _DetailWidth + _DetailSmoothness, data.g);

                #if _SPLINECURVETREEWEIGHT
                    float tcurve = saturate(data.g / (_TreeWidth + _TreeSmoothness));
                    tree *= 1.0 - tex2D(_SplineTreeWeight, float2(tcurve, 0.5)).r;
                #endif

                #if _SPLINECURVEDETAILWEIGHT
                    float dcurve = saturate(data.g / (_DetailWidth + _DetailSmoothness));
                    detail *= 1.0 - tex2D(_SplineDetailWeight, float2(dcurve, 0.5)).r;
                #endif


                #if _TREATASAREA
                    if (data.r < 0)
                    {
                        height = 1; splat = 1; tree = 1; detail = 1;
                    }
                #endif


                

                height = max(curMask.x, height);
                splat = max(curMask.y, splat);
                tree = max(curMask.z, tree);
                detail = max(curMask.w, detail);
                if (_HeightWidth >= 0)
                    curMask.x = height;
                if (_SplatWidth >= 0)
                    curMask.y = splat;
                if (_TreeWidth >= 0)
                    curMask.z = tree;
                if (_DetailWidth >= 0)
                    curMask.w = detail;
                return curMask; 

            }
            ENDCG
        }
    }
}