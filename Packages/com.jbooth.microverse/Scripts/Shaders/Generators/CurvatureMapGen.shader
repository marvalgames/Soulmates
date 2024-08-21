Shader "Hidden/MicroVerse/CurvatureMapGen"
{
    Properties
    {
        [HideInInspector]_Normalmap ("Normalmap", 2D) = "bump" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _NX
            #pragma shader_feature_local_fragment _ _NY
            #pragma shader_feature_local_fragment _ _PX
            #pragma shader_feature_local_fragment _ _PY

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

            sampler2D _Normalmap;
            sampler2D _Normalmap_PX;
            sampler2D _Normalmap_PY;
            sampler2D _Normalmap_NX;
            sampler2D _Normalmap_NY;
            float4 _Normalmap_TexelSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }


            float CurvatureFromNormal(float2 uv, float2 scale, float lod)
            {
	            float width = scale.x;
                float height = scale.y;
                float4 uvpx = float4(uv.x + width, uv.y, 0, lod);
                float4 uvnx = float4(uv.x - width, uv.y, 0, lod);
                float4 uvpy = float4(uv.x, uv.y + height, 0, lod);
                float4 uvny = float4(uv.x, uv.y - height, 0, lod);

                float posX, negX, posY, negY;
                #if _PX
                    UNITY_BRANCH
                    if (uvpx.x > 1)
                    {
                        uvpx.x -= 1;
                        uvpx.x += width;
                        posX = (tex2Dlod(_Normalmap_PX, uvpx) * 2.0 + 1.0).x;
                    }
                    else
                    {
                        posX = (tex2Dlod(_Normalmap, uvpx) * 2.0 + 1.0).x;
                    }
                #else
                    posX = (tex2Dlod(_Normalmap, uvpx) * 2.0 + 1.0).x;
                #endif

                #if _NX
                    UNITY_BRANCH
                    if (uvnx.x < 0)
                    {
                        uvnx.x += 1;
                        uvnx.x -= width;
                        negX = (tex2Dlod(_Normalmap_NX, uvnx) * 2.0 + 1.0).x;
                    }
                    else
                    {
                        negX = (tex2Dlod(_Normalmap, uvnx) * 2.0 + 1.0).x;
                    }
                #else
                    negX = (tex2Dlod(_Normalmap, uvnx) * 2.0 + 1.0).x;
                #endif

                #if _PY
                    UNITY_BRANCH
                    if (uvpy.y > 1)
                    {
                        uvpy.y -= 1;
                        uvpy.y += height;
                        posY = (tex2Dlod(_Normalmap_PY, uvpy) * 2.0 + 1.0).z;
                    }
                    else
                    {
                        posY = (tex2Dlod(_Normalmap, uvpy) * 2.0 + 1.0).z;
                    }
                #else
                    posY = (tex2Dlod(_Normalmap, uvpy) * 2.0 + 1.0).z;
                #endif


                #if _NY
                    UNITY_BRANCH
                    if (uvny.y < 0)
                    {
                        uvny.y += 1;
                        uvny.y -= height;
                        negY = (tex2Dlod(_Normalmap_NY, uvny) * 2.0 + 1.0).z;
                    }
                    else
                    {
                        negY = (tex2Dlod(_Normalmap, uvny) * 2.0 + 1.0).z;
                    }
                #else
                    negY = (tex2Dlod(_Normalmap, uvny) * 2.0 + 1.0).z;
                #endif
    
                float x = (posX - negX) + 0.5;
	            float y = (posY - negY) + 0.5;

	            float convexity = (y < 0.5) ? 2.0 * x * y : 1.0 - 2.0 * (1.0 - x) * (1.0 - y);
                
                //float cavity = (convexity - (1.0 - convexity));

                return convexity;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                float cav1 = CurvatureFromNormal(i.uv, _Normalmap_TexelSize, 0);
                return cav1;
            }
            ENDCG
        }
    }
}