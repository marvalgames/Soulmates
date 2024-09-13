Shader "RukhankaBoneOutlineRenderer URP"
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
		ZTest off

		HLSLPROGRAM
		#pragma target 3.0

		#pragma vertex VS
		#pragma fragment PS
        #define BONE_OUTLINE

		#include "RukhankaBoneRenderer.hlsl"

		ENDHLSL
	}
}
}
