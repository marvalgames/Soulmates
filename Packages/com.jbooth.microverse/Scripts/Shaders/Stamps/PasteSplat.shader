Shader "Hidden/MicroVerse/PasteSplat"
{
    Properties
    {


    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _USEFALLOFF _USEFALLOFFRANGE _USEFALLOFFTEXTURE _USEFALLOFFSPLINEAREA

            #include_with_pragmas "UnityCG.cginc"
            #include_with_pragmas "/../SplatMerge.cginc"

            sampler2D _IndexMap;
            sampler2D _WeightMap;
            sampler2D _OrigIndexMap;
            sampler2D _OrigWeightMap;
            float4x4 _Transform;
            float _Channels[32];
            sampler2D _FalloffTexture;
            float2 _FalloffTextureParams;
            int _FalloffTextureChannel;
            float2 _Falloff;
            float _FalloffAreaRange;

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

            float RectFalloff(float2 uv, float falloff) 
            {
                if (falloff == 1)
                {
                    if (uv.x <= 0 || uv.y <= 0 || uv.x >= 1 || uv.y >= 1)
                        return 0;
                    return 1;
                    
                }
                uv = saturate(uv);
                uv -= 0.5;
                uv = abs(uv);
                uv = 0.5 - uv;
                falloff = 1 - falloff;
                uv = smoothstep(uv, 0, 0.03 * falloff);
                return min(uv.x, uv.y);
            }



            FragmentOutput frag(v2f i)
            {
                half4 weightMap = tex2D(_WeightMap, i.stampUV);
                half4 indexMap = round(tex2D(_IndexMap, i.stampUV) * 32);

                half4 origWeightMap = tex2D(_OrigWeightMap, i.uv);
                half4 origIndexMap = round(tex2D(_OrigIndexMap, i.uv) * 32);

                FragmentOutput o;
                bool cp = (i.stampUV.x <= 0 || i.stampUV.x >= 1 || i.stampUV.y <= 0 || i.stampUV.y >= 1);
                if (cp)
                {
                    o.indexMap = origIndexMap / 32;
                    o.weightMap = origWeightMap;
                    return o;
                }

                float mask = 1;
                #if _USEFALLOFF
                    mask *= RectFalloff(i.stampUV, _Falloff.y);
                #elif _USEFALLOFFRANGE
                    float2 falloff = _Falloff * 0.5;
                    float radius = length( i.stampUV-0.5 );
 	                mask = 1.0 - saturate(( radius-falloff.x ) / max(0.01, ( falloff.y-falloff.x )));
                #elif _USEFALLOFFTEXTURE
                    mask = 1.0 - saturate(tex2D(_FalloffTexture, i.stampUV)[_FalloffTextureChannel] * _FalloffTextureParams.x + _FalloffTextureParams.y);
                #elif _USEFALLOFFSPLINEAREA
                    float d = tex2D(_FalloffTexture, i.uv).r;
                    d *= -1;
                    d /= max(0.0001, _FalloffAreaRange);
                    mask *= saturate(d);
                #endif

                indexMap[0] = _Channels[indexMap[0]];
                indexMap[1] = _Channels[indexMap[1]];
                indexMap[2] = _Channels[indexMap[2]];
                indexMap[3] = _Channels[indexMap[3]];
                weightMap *= mask;

                o = FilterSplatWeights(weightMap.x, origWeightMap, origIndexMap, indexMap.x);
                o = FilterSplatWeights(weightMap.y, o.weightMap, o.indexMap, indexMap.y);
                o = FilterSplatWeights(weightMap.z, o.weightMap, o.indexMap, indexMap.z);
                o = FilterSplatWeights(weightMap.w, o.weightMap, o.indexMap, indexMap.w);

                o.indexMap /= 32;
                return o;
            }
            ENDCG
        }
    }
}