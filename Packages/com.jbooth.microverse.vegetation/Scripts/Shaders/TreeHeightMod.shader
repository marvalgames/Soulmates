Shader "Hidden/MicroVerse/TreeHeightMod"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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
            float4 _MainTex_TexelSize;
            sampler2D _TreeSDF;
            sampler2D _PlacementMask;
            float _RealHeight;
            float _Amount;
            float _Width;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 raw = tex2D(_MainTex, i.uv);
                float sdf = tex2D(_TreeSDF, i.uv).r * 256;
                if (sdf < 0)
                    return raw;

                float origHeight = UnpackHeightmap(raw);
                float w = 1.0 - saturate(sdf / _Width);

                float mask = 1.0 - tex2D(_PlacementMask, i.uv).r;
                w *= mask;
                w = smoothstep(0,1, w);

                origHeight += (_Amount / _RealHeight) * w;
                return PackHeightmap(origHeight);
            }
            ENDCG
        }
    }
}
