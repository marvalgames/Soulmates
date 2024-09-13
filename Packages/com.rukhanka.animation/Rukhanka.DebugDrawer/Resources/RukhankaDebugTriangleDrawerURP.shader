Shader "RukhankaDebugTriangleDrawer URP"
{
SubShader
{
    PackageRequirements
    {
        "com.unity.render-pipelines.universal": "1.0.0"
    }
	Tags
	{
        "RenderPipeline"="UniversalPipeline"
		"Queue" = "Transparent+0"
	}

	Pass
	{
		Tags
		{
            "LightMode" = "UniversalForward"
		}

        Blend SrcAlpha OneMinusSrcAlpha
        Cull front

		HLSLPROGRAM
		#pragma target 3.0

		#pragma vertex VSTriangle
		#pragma fragment PS

		#include "RukhankaDebugDrawer.hlsl"

		ENDHLSL
	}
}
}
