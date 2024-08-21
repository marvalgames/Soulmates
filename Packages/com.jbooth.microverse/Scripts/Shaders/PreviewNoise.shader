Shader "Hidden/MicroVerse/NoisePreview"
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

            #include_with_pragmas "UnityCG.cginc"
            #include_with_pragmas "Noise.cginc"

            #pragma shader_feature_local_fragment _ _NOISE _FBM _WORLEY _WORM _WORMFBM

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

            float4 _Param;
            float2 _Remap;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 Colorize(float a, float b, float t)
            {
                float4 r = (t - a)/(b - a);
                if (r.x <= 0)
                {
                    r.b = abs(r.b);
                    r.rg = 0;
                }
                else if (r.x > 1)
                {
                    r.rgb = lerp(r.rgb, float3(1,0,0), saturate(r.x-1));
                }
                return r;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                #if _NOISE
                    return Colorize(_Remap.x, _Remap.y, Noise(i.uv, _Param));
                #elif _FBM
                    return Colorize(_Remap.x, _Remap.y, NoiseFBM(i.uv, _Param));
                #elif _WORM
                    return Colorize(_Remap.x, _Remap.y, NoiseWorm(i.uv, _Param));
                #elif _WORMFBM
                    return Colorize(_Remap.x, _Remap.y, NoiseWormFBM(i.uv, _Param));
                #else
                    return Colorize(_Remap.x, _Remap.y, NoiseWorley(i.uv, _Param));
                #endif
            }
            ENDCG
        }
    }
}
