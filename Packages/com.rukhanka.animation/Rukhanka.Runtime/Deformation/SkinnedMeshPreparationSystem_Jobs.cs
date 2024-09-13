#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public partial struct SkinnedMeshPreparationSystem
{
    
//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
struct GetFrameNewSkinnedMeshesJob: IJobChunk
{
	[ReadOnly]
	public NativeParallelHashMap<int, BRGRenderMeshArray> renderMeshArrays;
	[ReadOnly]
	public SharedComponentTypeHandle<RenderMeshArray> renderMeshArrayTypeHandle;
	[ReadOnly]
	public ComponentTypeHandle<MaterialMeshInfo> materialMeshInfoTypeHandle;
	[ReadOnly]
	public ComponentTypeHandle<AnimatedRendererComponent> animatedRendererTypeHandle;
	[ReadOnly]
	public ComponentLookup<AnimatedSkinnedMeshComponent> animatedSkinnedMeshLookup;
	
	[ReadOnly]
    public NativeParallelHashMap<Hash128, SkinnedMeshDescription> existingSkinnedMeshes;
    public NativeParallelHashMap<BatchMeshID, BlobAssetReference<SkinnedMeshInfoBlob>>.ParallelWriter newSkinnedMeshes;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
	{
		if (!renderMeshArrays.IsCreated)
			return;
		
		int renderMeshArrayIndex = chunk.GetSharedComponentIndex(renderMeshArrayTypeHandle);
		BRGRenderMeshArray chunkRenderMeshArray = default;
		if (renderMeshArrayIndex >= 0)
			renderMeshArrays.TryGetValue(renderMeshArrayIndex, out chunkRenderMeshArray);
		
		var materialMeshInfos = chunk.GetNativeArray(ref materialMeshInfoTypeHandle);
		var animatedRendererComponents = chunk.GetNativeArray(ref animatedRendererTypeHandle);
		
		var cee = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
		while (cee.NextEntityIndex(out var i))
		{
			var mmi = materialMeshInfos[i];
			var arc = animatedRendererComponents[i];
			var meshID = chunkRenderMeshArray.GetMeshID(mmi);
			
			if (!animatedSkinnedMeshLookup.TryGetComponent(arc.skinnedMeshEntity, out var asm))
				continue;
			
			if (!existingSkinnedMeshes.ContainsKey(asm.smrInfoBlob.Value.hash)) 
			{
				newSkinnedMeshes.TryAdd(meshID, asm.smrInfoBlob);
			}
		}
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
struct RegisterNewSkinnedMeshesJob: IJob
{
    public NativeParallelHashMap<BatchMeshID, BlobAssetReference<SkinnedMeshInfoBlob>> newSkinnedMeshes;
    public NativeParallelHashMap<Hash128, SkinnedMeshDescription> existingSkinnedMeshes;
	[NativeDisableUnsafePtrRestriction]
    public UnsafeAtomicCounter32 totalSkinnedVerticesCount;
	[NativeDisableUnsafePtrRestriction]
    public UnsafeAtomicCounter32 totalBoneWeightsCount;
	[NativeDisableUnsafePtrRestriction]
    public UnsafeAtomicCounter32 totalBlendShapeVerticesCount;
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 maximumVerticesAcrossAllRegisteredMeshes;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public unsafe void Execute()
	{
		var baseVertex = *totalSkinnedVerticesCount.Counter;
		var baseBoneWeightIndex = *totalBoneWeightsCount.Counter;
		var baseBlendShapeIndex = *totalBlendShapeVerticesCount.Counter;
		foreach (var sm in newSkinnedMeshes)
		{
			var smd = new SkinnedMeshDescription();
			ref var skinnedMeshBlob = ref sm.Value.Value;
			
			var numMeshVertices = skinnedMeshBlob.meshVerticesCount;
			smd.baseVertex = baseVertex;
			baseVertex += numMeshVertices;
			smd.vertexCount = numMeshVertices;
			
			var numBoneWeightIndicesCount = skinnedMeshBlob.meshBoneWeightsCount;
			smd.baseBoneWeightIndex = baseBoneWeightIndex;
			baseBoneWeightIndex += numBoneWeightIndicesCount;
			
			var blendShapesDataSize = skinnedMeshBlob.meshBlendShapesCount * numMeshVertices;
			smd.baseBlendShapeIndex = baseBlendShapeIndex;
			baseBlendShapeIndex += blendShapesDataSize;
			
			maximumVerticesAcrossAllRegisteredMeshes.Reset(math.max(skinnedMeshBlob.boneWeightsIndices.Length, *maximumVerticesAcrossAllRegisteredMeshes.Counter));
			
			existingSkinnedMeshes.Add(skinnedMeshBlob.hash, smd);
		}
		totalSkinnedVerticesCount.Reset(baseVertex);
		totalBoneWeightsCount.Reset(baseBoneWeightIndex);
		totalBlendShapeVerticesCount.Reset(baseBlendShapeIndex);
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
struct ResetFrameDataJob: IJob
{
	public NativeParallelHashMap<BatchMeshID, BlobAssetReference<SkinnedMeshInfoBlob>> newSkinnedMeshesToRegister;
	public NativeParallelHashMap<Entity, SkinnedMeshRendererFrameDeformationData> entityToSMRFrameDataMap;
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 frameSkinMatrixCounter;
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 frameBlendShapeWeightCounter;
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 frameDeformedVertexCount;
	public int frameSkinnedMeshesCount;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute()
	{
		newSkinnedMeshesToRegister.Clear();
		entityToSMRFrameDataMap.Clear();
		entityToSMRFrameDataMap.Capacity = math.max(frameSkinnedMeshesCount, entityToSMRFrameDataMap.Capacity);
		frameSkinMatrixCounter.Reset(0);
		frameBlendShapeWeightCounter.Reset(0);
		frameDeformedVertexCount.Reset(0);
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
partial struct ComputeFrameSkinnedMeshesWithLODsJob: IJobEntity
{
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 skinMatrixOffsetCounter;
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 blendShapeWeightOffsetCounter;
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 frameDeformedVerticesCounter;
	[ReadOnly]
	public ComponentLookup<CullAnimationsTag> cullAnimationsTagLookup;
	[ReadOnly]
	public ComponentLookup<LODRange> lodRangeLookup;
	[ReadOnly]
	public ComponentLookup<LODWorldReferencePoint> lodWorldRefPointLookup;
	[ReadOnly]
	public BufferLookup<Rukhanka.SkinMatrix> skinMatrixBufferLookup;
	[ReadOnly]
	public BufferLookup<Rukhanka.BlendShapeWeight> blendShapeWeightBufferLookup;
	[ReadOnly]
	public ComponentLookup<AnimatedSkinnedMeshComponent> animatedSkinnedMeshLookup;
	[ReadOnly]
	public NativeList<LODGroupExtensions.LODParams> lodAffectors;
	
	public NativeParallelHashMap<Entity, SkinnedMeshRendererFrameDeformationData>.ParallelWriter entityToSMRFrameData;
	
#if UNITY_EDITOR
	public bool isEditorWorld;
#endif
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe void Execute(Entity e, in AnimatedRendererComponent arc)
	{
		//	Early out if mesh already registered
		if (UnsafeParallelHashMapBase<Entity, SkinnedMeshRendererFrameDeformationData>.TryGetFirstValueAtomic(entityToSMRFrameData.m_Writer.m_Buffer, arc.skinnedMeshEntity, out _, out _))
			return;
			
	#if UNITY_EDITOR
		if (!isEditorWorld)
	#endif
		if (cullAnimationsTagLookup.HasComponent(arc.animatorEntity) && cullAnimationsTagLookup.IsComponentEnabled(arc.animatorEntity))
			return;
	
		if (!IsLODActive(e))
			return;
		
		var asm = animatedSkinnedMeshLookup[arc.skinnedMeshEntity];
		var currentDeformedVertexIndex = frameDeformedVerticesCounter.Add(asm.smrInfoBlob.Value.meshVerticesCount);
		var smrdd = SkinnedMeshRendererFrameDeformationData.MakeDefault();
		smrdd.deformedVertexIndex = currentDeformedVertexIndex;
        
		if (skinMatrixBufferLookup.TryGetBuffer(arc.skinnedMeshEntity, out var smb))
		{
			var currentSkinMatrixBufferOffset = skinMatrixOffsetCounter.Add(smb.Length);
			smrdd.skinMatrixIndex = currentSkinMatrixBufferOffset;
		}
		
		if (blendShapeWeightBufferLookup.TryGetBuffer(arc.skinnedMeshEntity, out var bsw))
		{
			var currentBlendShapeWeightOffset = blendShapeWeightOffsetCounter.Add(bsw.Length);
			smrdd.blendShapeWeightIndex = currentBlendShapeWeightOffset;
		}
			
		entityToSMRFrameData.TryAdd(arc.skinnedMeshEntity, smrdd);
	}
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	float CalculateLODDistance(in LODRange lodRange, in LODWorldReferencePoint lodRefPoint, in LODGroupExtensions.LODParams lodParams)
	{
		float rv = lodParams.distanceScale;
		if (!lodParams.isOrtho)
			rv *= math.length(lodParams.cameraPos - lodRefPoint.Value);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	bool IsLODActive(Entity e)
	{
		if (!lodAffectors.IsCreated || !lodRangeLookup.TryGetComponent(e, out var lodRange) || !lodWorldRefPointLookup.TryGetComponent(e, out var lodRefPoint))
			return true;
		
		for (var i = 0; i < lodAffectors.Length; ++i)
		{
			var la = lodAffectors[i];
			var d = CalculateLODDistance(lodRange, lodRefPoint, la);
			var isLodActive = d < lodRange.MaxDist && d >= lodRange.MinDist;
			if (isLodActive)
				return true;
		}
		return false;
	}
}

//-----------------------------------------------------------------------------------------------------------------//

[BurstCompile]
partial struct ComputeFrameSkinnedMeshesJob: IJobEntity
{
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 skinMatrixOffsetCounter;
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 blendShapeWeightOffsetCounter;
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 frameDeformedVerticesCounter;
	[ReadOnly]
	public ComponentLookup<CullAnimationsTag> cullAnimationsTagLookup;
	[ReadOnly]
	public BufferLookup<Rukhanka.SkinMatrix> skinMatrixBufferLookup;
	[ReadOnly]
	public BufferLookup<Rukhanka.BlendShapeWeight> blendShapeWeightBufferLookup;
	
	public NativeParallelHashMap<Entity, SkinnedMeshRendererFrameDeformationData>.ParallelWriter entityToSMRFrameData;
	
#if UNITY_EDITOR
	public bool isEditorWorld;
#endif
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(Entity e, in AnimatedSkinnedMeshComponent asm)
	{
	#if UNITY_EDITOR
		if (!isEditorWorld)
	#endif
		if (cullAnimationsTagLookup.HasComponent(asm.animatedRigEntity) && cullAnimationsTagLookup.IsComponentEnabled(asm.animatedRigEntity))
			return;
		
		var currentDeformedVertexIndex = frameDeformedVerticesCounter.Add(asm.smrInfoBlob.Value.meshVerticesCount);
		var smrdd = SkinnedMeshRendererFrameDeformationData.MakeDefault();
		smrdd.deformedVertexIndex = currentDeformedVertexIndex;
		
		if (skinMatrixBufferLookup.TryGetBuffer(e, out var skinMatrices))
		{
			var currentSkinMatrixBufferOffset = skinMatrixOffsetCounter.Add(skinMatrices.Length);
			smrdd.skinMatrixIndex = currentSkinMatrixBufferOffset;
		}
		
		if (blendShapeWeightBufferLookup.TryGetBuffer(e, out var blendShapeWeights))
		{
			var currentBlendShapeWeightsBufferOffset = blendShapeWeightOffsetCounter.Add(blendShapeWeights.Length);
			smrdd.blendShapeWeightIndex = currentBlendShapeWeightsBufferOffset;
		}
		
		entityToSMRFrameData.TryAdd(e, smrdd);
	}
}

}
}

#endif