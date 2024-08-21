Shader "Hidden/VolumetricLights/TransparentMultiply"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1)
    }
    SubShader
    {
        Blend Zero SrcColor, One Zero
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            half3 _TranslucencyIntensity;

            #define TRANSLUCENCY_INTENSITY _TranslucencyIntensity.x
            #define TRANSLUCENCY_BLEND _TranslucencyIntensity.y

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half3 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = float4(TRANSFORM_TEX(v.uv, _MainTex), o.vertex.zw);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 color = tex2D(_MainTex, i.uv.xy);
                color.rgb *= _Color;
                color.rgb = lerp(1.0.xxx, color.rgb * TRANSLUCENCY_INTENSITY, TRANSLUCENCY_BLEND);
                color.a = i.uv.z/i.uv.w;
                return color;
            }
            ENDCG
        }
    }
}
