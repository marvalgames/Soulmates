Shader "Hidden/VolumetricLights/CopyDepthIntoCubemap"
{
    Properties
    {
    }
    SubShader
    {
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {

            HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_ShadowTexture);
            SAMPLER(sampler_ShadowTexture);
            float3 _LightPos;
            float4x4 _I_VP_Matrix;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                o.uv.y = 1.0 - o.uv.y;
                return o;
            }

            #define dot2(x) dot(x,x)

            float4 frag (v2f i) : SV_Target
            {
               float rawDepth = SAMPLE_DEPTH_TEXTURE(_ShadowTexture, sampler_ShadowTexture, i.uv);
               float4 positionCS  = ComputeClipSpacePosition(i.uv, rawDepth);
               float4 hpositionWS = mul(_I_VP_Matrix, positionCS);
               float3 wpos = hpositionWS.xyz / hpositionWS.w;

               return dot2(wpos - _LightPos);
            }
            ENDHLSL
        }
    }
}
