Shader "Hidden/MicroVerse/NineCombine"
{
    Properties
    {
        _Tex0 ("Texture", 2D) = "black" {}
        _Tex1 ("Texture", 2D) = "black" {}
        _Tex2 ("Texture", 2D) = "black" {}
        _Tex3 ("Texture", 2D) = "black" {}
        _Tex4 ("Texture", 2D) = "black" {}
        _Tex5 ("Texture", 2D) = "black" {}
        _Tex6 ("Texture", 2D) = "black" {}
        _Tex7 ("Texture", 2D) = "black" {}
        _Tex8 ("Texture", 2D) = "black" {}
        _Zoom ("Zoom", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            Texture2D _Tex0;
            Texture2D _Tex1;
            Texture2D _Tex2;
            Texture2D _Tex3;
            Texture2D _Tex4;
            Texture2D _Tex5;
            Texture2D _Tex6;
            Texture2D _Tex7;
            Texture2D _Tex8;
            float _Zoom;
            SamplerState my_linear_clamp_sampler;

            float4 _Tex4_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {

                float2 uv = i.uv;
                float fac = (1 + (_Zoom-1)*2); // 1.25 zoom = 1.5
                uv = uv * fac - ((_Zoom-1));    // 0-1.5, -0.25 -> 1.25

                UNITY_BRANCH
                if (uv.y < 0)
                {
                    uv.y++;
                    if (uv.x < 0)
                    {
                        uv.x++;
                        return _Tex6.Sample(my_linear_clamp_sampler, uv).r;
                    }
                    else if (uv.x > 1)
                    {
                        uv.x--;
                        return _Tex8.Sample(my_linear_clamp_sampler, uv).r;
                    }
                    else
                    {
                        return _Tex7.Sample(my_linear_clamp_sampler, uv).r;
                    }
                    
                }
                else if (uv.y > 1)
                {
                    uv.y--;
                    if (uv.x < 0)
                    {
                        uv.x++;
                        return _Tex0.Sample(my_linear_clamp_sampler, uv).r;
                    }
                    else if (uv.x > 1)
                    {
                        uv.x--;
                        return _Tex2.Sample(my_linear_clamp_sampler, uv).r;
                    }
                    else
                    {
                        return _Tex1.Sample(my_linear_clamp_sampler, uv).r;
                    }
                }
                else if (uv.x < 0)
                {
                    uv.x++;
                    return _Tex3.Sample(my_linear_clamp_sampler, uv).r;
                }
                else if (uv.x > 1)
                {
                    uv.x--;
                    return _Tex5.Sample(my_linear_clamp_sampler, uv).r;
                }
                else
                {
                    return _Tex4.Sample(my_linear_clamp_sampler, uv).r;
                }

                
            }
            ENDCG
        }
    }
}
