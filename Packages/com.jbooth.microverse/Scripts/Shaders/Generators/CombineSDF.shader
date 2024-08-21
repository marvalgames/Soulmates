Shader "Hidden/MicroVerse/CombineSDF"
{
    Properties
    {
        _SourceA ("Texture", 2D) = "white" {}
        _SourceB ("Texture", 2D) = "white" {}
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

            sampler2D _SourceA;
            sampler2D _SourceB;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float frag (v2f i) : SV_Target
            {
                float sourceA = tex2D(_SourceA, i.uv).r;
                float sourceB = tex2D(_SourceB, i.uv).r;
                return min(sourceA, sourceB);
            }
            ENDCG
        }
    }
}
