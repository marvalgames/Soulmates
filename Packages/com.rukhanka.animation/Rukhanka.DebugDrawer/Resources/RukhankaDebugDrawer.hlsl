#ifndef RUKHANKA_DEBUG_DRAWER_HLSL_
#define RUKHANKA_DEBUG_DRAWER_HLSL_

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#ifdef IS_HDRP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.cs.hlsl"
#endif

#include "RukhankaDebugDrawerCommon.hlsl"

/////////////////////////////////////////////////////////////////////////////////

struct VertexInput
{
	uint vertexID: SV_VertexID;
};

struct VertexToPixel
{
	float4 pos: SV_Position;
    float3 normal: NORMAL;
    float2 uv: TEXCOORD0;
	float4 color: COLOR0;
};

struct LineData
{
	float3 pos[2];
	uint color;
};

struct ThickLineData
{
	float3 pos[2];
    float thickness;
	uint color;
};

struct TriData
{
	float3 pos[3];
	uint color;
};

StructuredBuffer<LineData> lineDataBuf;
StructuredBuffer<ThickLineData> thickLineDataBuf;
StructuredBuffer<TriData> triDataBuf;
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;

#ifdef IS_HDRP
StructuredBuffer<DirectionalLightData> _DirectionalLightDatas;
#else
float4 _MainLightPosition;
float4 _MainLightColor;
#endif

/////////////////////////////////////////////////////////////////////////////////

VertexToPixel VSLines(VertexInput i)
{
	VertexToPixel o = (VertexToPixel)0;
    uint lineID = i.vertexID >> 1;
    uint vertexID = i.vertexID & 1;

	LineData ln = lineDataBuf[lineID];
	float3 worldPos = ln.pos[vertexID];

	worldPos = GetCameraRelativePositionWS(worldPos);
	o.pos = mul(unity_MatrixVP, float4(worldPos, 1));
	o.color = UnpackColor(ln.color);
	return o;
}

/////////////////////////////////////////////////////////////////////////////////

VertexToPixel VSThickLines(VertexInput i)
{
	VertexToPixel o = (VertexToPixel)0;
    uint instanceID = i.vertexID / 6;
    uint triangleID = (i.vertexID - instanceID * 6) / 3;
    uint vertexID = i.vertexID - instanceID * 6 - triangleID * 3;
    uint posID = (vertexID + triangleID) >> 1;
    uint leftRightID = (vertexID + triangleID) & 1;

	ThickLineData tld = thickLineDataBuf[instanceID];
	float3 worldPos = tld.pos[posID];

    float3 dp = tld.pos[0] - tld.pos[1];
    float3 viewVec = unity_MatrixV[2].xyz;
    float3 c = cross(viewVec, dp);
    c = normalize(c) * (leftRightID * 2.0f - 1) * tld.thickness;

    worldPos += c;
    o.uv = float2(leftRightID, posID);

	worldPos = GetCameraRelativePositionWS(worldPos);
	o.pos = mul(unity_MatrixVP, float4(worldPos, 1));
	o.color = UnpackColor(tld.color);
    o.color.a = 1;
	return o;
}

/////////////////////////////////////////////////////////////////////////////////

VertexToPixel VSTriangle(VertexInput i)
{
	VertexToPixel o = (VertexToPixel)0;
    uint triangleID = i.vertexID / 3;
    uint vertexID = i.vertexID - triangleID * 3;

	TriData td = triDataBuf[triangleID];

    const uint2 neighbourIndices[] =
    {
        uint2(2, 1),
        uint2(0, 2),
        uint2(1, 0),
    };

    float3 p0p2 = td.pos[neighbourIndices[vertexID].x] - td.pos[vertexID];
    float3 p0p1 = td.pos[neighbourIndices[vertexID].y] - td.pos[vertexID];

    float3 normal = 0;
    float eps = 0.00001f;
    if (length(p0p1) > eps && length(p0p2) > eps)
    {
        normal = cross(p0p2, p0p1);
        normal = normalize(normal);
    }

	float3 worldPos = td.pos[vertexID];

	worldPos = GetCameraRelativePositionWS(worldPos);
	o.pos = mul(unity_MatrixVP, float4(worldPos, 1));
    o.normal = normal;
	o.color = UnpackColor(td.color);
	return o;
}

/////////////////////////////////////////////////////////////////////////////////

void GetMainLight(out float3 lightDir, out float3 color)
{
#ifdef IS_HDRP
	if (_DirectionalLightCount > 0)
	{
		DirectionalLightData light = _DirectionalLightDatas[0];
		lightDir = -light.forward.xyz;
		color = light.color;
	}
	else
	{
		lightDir = float3(1, 0, 0);
		color = 0;
	}
#else
	lightDir = _MainLightPosition.rgb;
    color = _MainLightColor.rgb;
#endif
}

/////////////////////////////////////////////////////////////////////////////////

float4 PS(VertexToPixel i): SV_Target0
{
    float4 rv = i.color;//float4(0, 0, 0, 1);
    if (length(i.normal) > 0.1f)
    {
        float3 mainLightDir, mainLightColor;
    	GetMainLight(mainLightDir, mainLightColor);
        float df = dot(mainLightDir, i.normal) * 0.5f + 0.5f;

        rv.rgb *= df;
    }
	return rv;
}

#endif
