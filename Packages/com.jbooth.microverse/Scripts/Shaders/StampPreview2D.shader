Shader "Hidden/MicroVerse/StampPreview2D"
{
    Properties
    {
        [NoScaleOffset]_Gradient ("Gradient", 2D) = "white" {}
        [NoScaleOffset]_Stamp ("Stamp Map", 2D) = "black" {}
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

            sampler2D _Stamp;
            sampler2D _Gradient;
            float4 _Stamp_TexelSize;



            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                half height = tex2D(_Stamp, i.uv).r;
                half4 c = tex2D(_Gradient, float2(height,0));


                float2 uvx = i.uv + float2(_Stamp_TexelSize.x, 0.0);
                float2 uvy = i.uv + float2(0.0, _Stamp_TexelSize.y);

                float x = tex2D(_Stamp, uvx).r;
                float y = tex2D(_Stamp, uvy).r;
                float2 dxy = height - float2(x, y);

                float scale = 1;
                dxy = dxy * scale / _Stamp_TexelSize.xy;
                float3 normal = normalize(float3( dxy.x, dxy.y, 1.0)).xzy * 0.5 + 0.5;
                normal.xyz = normal.xzy;

                float lit = dot(normal, normalize(float3(1,1,1)));
                

                return c * lit;

            }
            ENDCG
        }
    }
}
