#ifndef DEFORMATION_COMMON_HLSL_
#define DEFORMATION_COMMON_HLSL_

/////////////////////////////////////////////////////////////////////////////////

struct SourceSkinnedMeshVertex
{
    float3 position;
    float3 normal;
    float3 tangent;
    uint boneWeightsOffsetAndCount;

    void WriteIntoRawBuffer(RWByteAddressBuffer outBuffer, uint index)
    {
        uint byteOffset = index * 40;
        uint4 u0 = asuint(float4(position, normal.x));
        uint4 u1 = asuint(float4(normal.yz, tangent.xy));
        uint2 u2 = uint2(asuint(tangent.z), boneWeightsOffsetAndCount);
        outBuffer.Store4(byteOffset + 0,  u0);
        outBuffer.Store4(byteOffset + 16, u1);
        outBuffer.Store2(byteOffset + 32, u2);
    }

    static SourceSkinnedMeshVertex ReadFromRawBuffer(ByteAddressBuffer inBuffer, uint index)
    {
        uint byteOffset = index * 40;
        uint4 u0 = inBuffer.Load4(byteOffset + 0);
        uint4 u1 = inBuffer.Load4(byteOffset + 16);
        uint2 u2 = inBuffer.Load2(byteOffset + 32);

        SourceSkinnedMeshVertex rv;
        rv.position = asfloat(u0.xyz);
        rv.normal = asfloat(uint3(u0.w, u1.xy));
        rv.tangent = asfloat(uint3(u1.zw, u2.x));
        rv.boneWeightsOffsetAndCount = u2.y;
        return rv;
    }
};

/////////////////////////////////////////////////////////////////////////////////

#define DEFORMED_VERTEX_SIZE_IN_BYTES 36
struct DeformedVertex
{
    float3 position;
    float3 normal;
    float3 tangent;

    static DeformedVertex ReadFromRawBuffer(ByteAddressBuffer inBuffer, uint index)
    {
        uint byteOffset = index * DEFORMED_VERTEX_SIZE_IN_BYTES;
        uint4 v0 = inBuffer.Load4(byteOffset + 0);
        uint4 v1 = inBuffer.Load4(byteOffset + 16);
        uint  v2 = inBuffer.Load(byteOffset + 32);

        DeformedVertex rv;
        rv.position = asfloat(v0.xyz);
        rv.normal = asfloat(uint3(v0.w, v1.xy));
        rv.tangent = asfloat(uint3(v1.zw, v2.x));
        return rv;
    }

    void Scale(float v)
    {
        position *= v;
        normal *= v;
        tangent *= v;
    }
};

/////////////////////////////////////////////////////////////////////////////////

struct BoneInfluence
{
    float weight;
    int boneIndex;

    static BoneInfluence ReadFromRawBuffer(ByteAddressBuffer inBuffer, uint index)
    {
        uint byteOffset = index * 8;
        uint2 u0 = inBuffer.Load2(byteOffset + 0);

        BoneInfluence rv;
        rv.weight = asfloat(u0.x);
        rv.boneIndex = u0.y;
        return rv;
    }
};

/////////////////////////////////////////////////////////////////////////////////

struct MeshFrameDeformationDescription
{
    int baseSkinMatrixIndex;
    int baseBlendShapeWeightIndex;
	int baseOutVertexIndex;
	int baseInputMeshVertexIndex;
	int baseInputMeshBlendShapeIndex;
	int meshVerticesCount;
	int meshBlendShapesCount;
};

/////////////////////////////////////////////////////////////////////////////////

struct InputBlendShapeVertex
{
    uint meshVertexIndex;
    float3 positionDelta;
    float3 normalDelta;
    float3 tangentDelta;
};
#define INPUT_BLEND_SHAPE_VERTEX_SIZE_IN_BYTES 40

/////////////////////////////////////////////////////////////////////////////////

uint GetBoneWeightsOffsetFromPackedUINT(uint boneWeightsOffsetAndCount)
{
    return boneWeightsOffsetAndCount >> 8;
}

/////////////////////////////////////////////////////////////////////////////////

uint GetBoneWeightsCountFromPackedUINT(uint boneWeightsOffsetAndCount)
{
    return boneWeightsOffsetAndCount & 0xff;
}

/////////////////////////////////////////////////////////////////////////////////

uint PackBoneOffsetAndCount(uint count, uint offset)
{
    return count | (offset << 8);
}

/////////////////////////////////////////////////////////////////////////////////

StructuredBuffer<MeshFrameDeformationDescription> frameDeformedMeshes;

#endif // DEFORMATION_COMMON_HLSL_
