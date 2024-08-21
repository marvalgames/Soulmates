Shader "Hidden/MicroVerse/NormalMapGen"
{
    Properties
    {
        _MainTex("tex", 2D) = "black" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _PX
            #pragma shader_feature_local_fragment _ _PY
            #pragma shader_feature_local_fragment _ _NX
            #pragma shader_feature_local_fragment _ _NY

            #include_with_pragmas "UnityCG.cginc"

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

            sampler2D _Heightmap;
            sampler2D _Heightmap_PX;
            sampler2D _Heightmap_PY;
            sampler2D _Heightmap_NX;
            sampler2D _Heightmap_NY;
            float4 _Heightmap_TexelSize;
            

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }


            float3 GenerateNormal(float2 uv, float scale, float2 offset)
            {
                float height = UnpackHeightmap(tex2D(_Heightmap, uv));
                float2 uvx = uv + float2(offset.x, 0.0);
                float2 uvy = uv + float2(0.0, offset.y);

                float x,y;


                #if _PX
                    UNITY_BRANCH
                    if (uvx.x > 1)
                    {
                       uvx.x -= 1;
                       uvx.x += offset.x;
                       x = UnpackHeightmap(tex2D(_Heightmap_PX, uvx)); 
                    }
                    else
                    {
                       x = UnpackHeightmap(tex2D(_Heightmap, uvx));
                    }
                #else
                    x = UnpackHeightmap(tex2D(_Heightmap, uvx));
                #endif

                #if _PY
                    UNITY_BRANCH
                    if (uvy.y > 1)
                    {
                       uvy.y -= 1;
                       uvy.y += offset.y;
                       y = UnpackHeightmap(tex2D(_Heightmap_PY, uvy)); 
                    }
                    else
                    {
                       y = UnpackHeightmap(tex2D(_Heightmap, uvy));
                    }
                #else
                    y = UnpackHeightmap(tex2D(_Heightmap, uvy));
                #endif

                
                float2 dxy = height - float2(x, y);

                dxy = dxy * scale / offset.xy;
                return normalize(float4( dxy.x, dxy.y, 1.0, height)).xzy * 0.5 + 0.5;
            }

            float SampleHeight(float2 uv)
            {
                #if _PX
                    UNITY_BRANCH
                    if (uv.x > 1)
                    {
                        uv.x -= 1;
                        uv.x += _Heightmap_TexelSize.x;
                        return UnpackHeightmap(tex2D(_Heightmap_PX, uv));   
                    }
                #endif
                #if _PY
                    UNITY_BRANCH
                    if (uv.y > 1)
                    {
                       uv.y -= 1;
                       uv.y += _Heightmap_TexelSize.y;
                       return UnpackHeightmap(tex2D(_Heightmap_PY, uv)); 
                    }
                #endif
                #if _NX
                    UNITY_BRANCH
                    if (uv.x < 0)
                    {
                        uv.x += 1;
                        uv.x -= _Heightmap_TexelSize.x;
                        return UnpackHeightmap(tex2D(_Heightmap_NX, uv));   
                    }
                #endif
                #if _NY
                    UNITY_BRANCH
                    if (uv.y < 0)
                    {
                       uv.y += 1;
                       uv.y -= _Heightmap_TexelSize.y;
                       return UnpackHeightmap(tex2D(_Heightmap_NY, uv)); 
                    }
                #endif

                return UnpackHeightmap(tex2D(_Heightmap, uv)); 
            }

            float3 GenerateNormal5Tap(float2 uv, float2 offset)
            {
                float height = UnpackHeightmap(tex2D(_Heightmap, uv));

                float x = SampleHeight(uv + float2(offset.x, 0.0));
                float y = SampleHeight(uv + float2(0.0, offset.y));
                float nx = SampleHeight(uv + float2(-offset.x, 0.0));
                float ny = SampleHeight(uv + float2(0.0, -offset.y));

                float2 dxy = height - float2(x, y);
                dxy += float2(nx, ny) - height;

                dxy = dxy * 1.0f / offset.xy;
                dxy *= 0.5;
                return normalize(float4(dxy.x, dxy.y, 1.0, height)).xzy * 0.5 + 0.5;
            }


            float4 frag(v2f i) : SV_Target
            {
                //float3 normal = GenerateNormal(i.uv, 1, _Heightmap_TexelSize.xy);
                float3 normal = GenerateNormal5Tap(i.uv, _Heightmap_TexelSize.xy);
                return float4(normal, 1);
            }
            ENDCG
        }
    }
}