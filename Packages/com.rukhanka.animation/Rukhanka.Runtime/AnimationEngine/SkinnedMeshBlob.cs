using Hash128 = Unity.Entities.Hash128;
using Unity.Mathematics;
using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public struct SkinnedMeshBoneInfo
{
#if RUKHANKA_DEBUG_INFO
	public BlobString name;
#endif
	public Hash128 hash;
	public float4x4 bindPose;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct BlendShapeInfo
{
#if RUKHANKA_DEBUG_INFO
	public BlobString name;
#endif
	public uint hash;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct SkinnedMeshInfoBlob
{
#if RUKHANKA_DEBUG_INFO
	public BlobString skeletonName;
	public float bakingTime;
#endif
	public Hash128 hash;
	public BlobArray<SkinnedMeshBoneInfo> bones;
	public BlobArray<BlendShapeInfo> blendShapes;
	//	Each weight index is bone count for a vertex (lower 8 bits) and start bone weight data index (upper 24 bits)
	//	Bone weights index need to be used to index into Mesh.GetAllBoneWeights().
	public BlobArray<uint> boneWeightsIndices;
	public int meshBoneWeightsCount;
	public int meshBlendShapesCount;
	public int meshVerticesCount;
	
	public static uint GetBoneWeightCount(uint packedBoneWeightsValue) => packedBoneWeightsValue & 0xff;
	public static uint GetBoneWeightIndexOffset(uint packedBoneWeightsValue) => packedBoneWeightsValue >> 8;
	public static uint PackBoneCountAndOffset(byte boneCount, uint offset)
	{
		var rv = boneCount | (offset << 8);
		return rv;
	}
}
}
