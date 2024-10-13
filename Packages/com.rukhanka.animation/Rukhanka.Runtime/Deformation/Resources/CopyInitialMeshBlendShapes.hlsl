#ifndef COPY_INITIAL_MESH_BLEND_SHAPES_HLSL_
#define COPY_INITIAL_MESH_BLEND_SHAPES_HLSL_

/////////////////////////////////////////////////////////////////////////////////

ByteAddressBuffer meshBlendShapesBuffer;
RWByteAddressBuffer outInitialMeshBlendShapesData;

uint inputBlendShapeVerticesCount;
uint inputBlendShapeVertexOffset;
uint outBlendShapeVertexOffset;

/////////////////////////////////////////////////////////////////////////////////

InputBlendShapeVertex ReadBlendShapeVertexDelta(uint vertexID)
{
    uint vertexByteOffset = vertexID * InputBlendShapeVertex::size;
    uint4 v0 = meshBlendShapesBuffer.Load4(vertexByteOffset + 0);
    uint4 v1 = meshBlendShapesBuffer.Load4(vertexByteOffset + 16);
    uint2 v2 = meshBlendShapesBuffer.Load2(vertexByteOffset + 32);

    InputBlendShapeVertex rv;
    rv.meshVertexIndex = v0.x;
    rv.positionDelta = asfloat(v0.yzw);
    rv.normalDelta = asfloat(v1.xyz);
    rv.tangentDelta = asfloat(uint3(v1.w, v2.xy));

    return rv;
}

/////////////////////////////////////////////////////////////////////////////////

[numthreads(128, 1, 1)]
void CopyInitialMeshBlendShapes(uint tid: SV_DispatchThreadID)
{
    if (tid >= inputBlendShapeVerticesCount)
        return;

    InputBlendShapeVertex v = ReadBlendShapeVertexDelta(tid + inputBlendShapeVertexOffset);

    uint4 o0 = asuint(float4(v.positionDelta, v.normalDelta.x));
    uint4 o1 = asuint(float4(v.normalDelta.yz, v.tangentDelta.xy));
    uint o2 = asuint(v.tangentDelta.z);

    uint outVertexOffset = v.meshVertexIndex + outBlendShapeVertexOffset;
    uint outVertexByteOffset = outVertexOffset * DeformedVertex::size;
    outInitialMeshBlendShapesData.Store4(outVertexByteOffset + 0, o0);
    outInitialMeshBlendShapesData.Store4(outVertexByteOffset + 16, o1);
    outInitialMeshBlendShapesData.Store(outVertexByteOffset + 32, o2);
}

#endif // COPY_INITIAL_MESH_BLEND_SHAPES_HLSL_
