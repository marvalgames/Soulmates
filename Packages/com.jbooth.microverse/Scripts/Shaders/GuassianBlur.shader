Shader "Hidden/MicroVerse/GaussianBlurDepth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", float) = 1.0
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

            sampler2D _MainTex;
            float _BlurSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float frag (v2f i) : SV_Target
            {

                float col = 0;
                float totalWeight = 0.0;
                int blurSize = int(_BlurSize);

                // Sum all pixels around the current pixel
                for (int x = -blurSize; x <= blurSize; x++)
                {
                    for (int y = -blurSize; y <= blurSize; y++)
                    {
                        col += SAMPLE_DEPTH_TEXTURE(_MainTex, i.uv + float2(x, y).r);
                    }
                }

                // Average the colors
                int totalPixels = (2 * blurSize + 1) * (2 * blurSize + 1);
                col /= totalPixels;

                return 0.0;

            }
            ENDCG
        }
    }
}
