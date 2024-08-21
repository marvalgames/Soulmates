Shader "Hidden/MicroVerse/CombineDetailBuffers"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _Merge ("Texture", 2D) = "black" {}
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
            sampler2D _Merge;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half frag (v2f i) : SV_Target
            {
                // sample the texture
                half main = tex2D(_MainTex, i.uv).r;
                half merge = tex2D(_Merge, i.uv).r;
                return max(main, merge);
            }
            ENDCG
        }
    }
}
