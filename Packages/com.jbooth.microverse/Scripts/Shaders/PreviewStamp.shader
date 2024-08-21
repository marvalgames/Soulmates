Shader "Hidden/MicroVerse/PreviewStamp"
{
    Properties
    {
        _MainTex("Mask Texture", 2D) = "white" {}
        _ColorTex("Color Tex", 2D) = "white" {}
    }
    SubShader
    {
        Cull Back ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE

        #include_with_pragmas "UnityCG.cginc"
        #include_with_pragmas "TerrainPreview.cginc"

        
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ _USEFALLOFFTEXTURE _NOFALLOFF

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 pcPixels : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            float4 _Heightmap_TexelSize;
            float4x4 _Transform;
            float2 _Falloff;
            float _FalloffChannel;
            sampler2D _MainTex;
            sampler2D _ColorTex;
            float3 _Color;

            Varyings vert(uint vid : SV_VertexID)
            {
                Varyings o;
                
                // build a quad mesh, with one vertex per paint context pixel (pcPixel)

                float2 pcPixels = BuildProceduralQuadMeshVertex(vid);

                // compute heightmap UV and sample heightmap
                float2 heightmapUV = PaintContextPixelsToHeightmapUV(pcPixels);
                float heightmapSample = UnpackHeightmap(tex2Dlod(_Heightmap, float4(heightmapUV, 0, 0)));

                // compute brush UV
                float2 brushUV = PaintContextPixelsToBrushUV(pcPixels);

                // compute object position (in terrain space) and world position

                float3 positionObject = PaintContextPixelsToObjectPosition(pcPixels, heightmapSample);
                float3 positionWorld = TerrainObjectToWorldPosition(positionObject);

                o.pcPixels = pcPixels;
                o.positionCS = UnityWorldToClipPos(positionWorld);
                o.uv = brushUV;
                return o;
            }


            float4 frag(Varyings i) : SV_Target
            {
                float2 stampUV = mul(_Transform, float4(i.uv, 0, 1)).xy;
                float4 colorTex = tex2D(_ColorTex, stampUV);
                #if _USEFALLOFFTEXTURE
                    bool cp = stampUV.x <= 0 || stampUV.x >= 1 || stampUV.y <= 0 || stampUV.y >= 1;
                    if (cp)
                        return 0;
                    float mask = tex2D(_MainTex, stampUV)[(int)_FalloffChannel];
                    return float4(lerp(_Color, colorTex.rgb, colorTex.a), mask);
                #elif _NOFALLOFF
                    return colorTex;
                #else
                    float2 falloff = _Falloff * 0.5;
                    float radius = length( stampUV-0.5 );
 	                float mask = 1.0 - saturate(( radius-falloff.x ) / ( falloff.y-falloff.x ));
                    float area = 1.0 - saturate((radius*2-1) / 0.01);
                    colorTex.a *= area;
                    float3 color = lerp(_Color, colorTex.rgb, colorTex.a);
                    return float4(color, saturate(max(mask * 0.15, colorTex.a)));
                #endif
            }
            ENDHLSL
        }
    }
    Fallback Off
}