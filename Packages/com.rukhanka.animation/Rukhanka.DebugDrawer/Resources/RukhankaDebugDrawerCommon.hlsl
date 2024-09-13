#ifndef RUKHANKA_DEBUG_DRAWER_COMMON_HLSL_
#define RUKHANKA_DEBUG_DRAWER_COMMON_HLSL_

float3 GetCameraRelativePositionWS(float3 positionWS)
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
	positionWS -= _WorldSpaceCameraPos.xyz;
#endif
	return positionWS;
}

/////////////////////////////////////////////////////////////////////////////////

float4 UnpackColor(uint color)
{
    float4 rv = float4
    (
        color >> 24,
        color >> 16 & 0xff,
        color >> 8 & 0xff,
        color & 0xff
    );
    rv = rv / 255.0f;
    return rv;
}

#endif
