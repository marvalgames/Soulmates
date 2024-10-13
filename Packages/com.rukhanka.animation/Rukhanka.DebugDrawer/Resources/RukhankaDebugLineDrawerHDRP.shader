Shader "RukhankaDebugLineDrawer HDRP"
{
SubShader
{
    PackageRequirements
    {
        "com.unity.render-pipelines.high-definition": "1.0.0"
    }
	Tags
	{
		"RenderPipeline" = "HDRenderPipeline"
		"RenderType" = "HDUnlitShader"
		"Queue" = "Transparent+0"
	}

	Pass
	{
		Name "ForwardOnly"
		Tags
		{
			"LightMode" = "ForwardOnly"
		}

        Blend SrcAlpha OneMinusSrcAlpha
		ZTest off

		HLSLPROGRAM
		#pragma target 3.0

		#pragma vertex VSLines
		#pragma fragment PS
        #define IS_HDRP

		#include "RukhankaDebugDrawer.hlsl"

		ENDHLSL
	}
}
}
