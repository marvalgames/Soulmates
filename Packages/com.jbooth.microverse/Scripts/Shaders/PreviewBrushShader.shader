Shader "Hidden/MicroVerse/PreviewBrushShader"
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

            float _Falloff;
            float _Size;
    
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                i.uv -= 0.5;
                i.uv /= lerp(0.25, 1.0, _Size);
                i.uv += 0.5;
                float d = distance(i.uv, float2(0.5, 0.5));
                d *= 2;
                d = 1.0 - d;
                d = pow(d, abs(_Falloff));
                return d;
            }
            ENDCG
        }
    }
}
