using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Deformations;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
partial struct AnimationApplicationSystem
{

//=================================================================================================================//

[BurstCompile]
partial struct ApplyAnimationToSkinnedMeshJob: IJobEntity
{
	[ReadOnly]
	public ComponentLookup<RigDefinitionComponent> rigDefinitionLookup;
	[ReadOnly]
	public ComponentLookup<CullAnimationsTag> cullAnimationsTagLookup;
	[ReadOnly]
	public NativeList<BoneTransform> boneTransforms;
	[ReadOnly]
	public NativeParallelHashMap<Entity, RuntimeAnimationData.AnimatedEntityBoneDataProps> entityToDataOffsetMap;
	[ReadOnly]
	public NativeParallelHashMap<Hash128, BlobAssetReference<BoneRemapTableBlob>> rigToSkinnedMeshRemapTables;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static Hash128 CalculateBoneRemapTableHash(in BlobAssetReference<SkinnedMeshInfoBlob> skinnedMesh, in BlobAssetReference<RigDefinitionBlob> rigDef)
	{
		var rv = new Hash128(skinnedMesh.Value.hash.Value.x, skinnedMesh.Value.hash.Value.y, rigDef.Value.hash.Value.x, rigDef.Value.hash.Value.y);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	ref BoneRemapTableBlob GetBoneRemapTable(in BlobAssetReference<SkinnedMeshInfoBlob> skinnedMesh, in BlobAssetReference<RigDefinitionBlob> rigDef)
	{
		var h = CalculateBoneRemapTableHash(skinnedMesh, rigDef);
		return ref rigToSkinnedMeshRemapTables[h].Value;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	SkinMatrix MakeSkinMatrixForBone(ref SkinnedMeshBoneInfo boneInfo, in float4x4 boneXForm, in float4x4 entityToRootBoneTransform)
	{
		var boneTransformMatrix = math.mul(entityToRootBoneTransform, boneXForm);
		boneTransformMatrix = math.mul(boneTransformMatrix, boneInfo.bindPose);

		var skinMatrix = new SkinMatrix() { Value = new float3x4(boneTransformMatrix.c0.xyz, boneTransformMatrix.c1.xyz, boneTransformMatrix.c2.xyz, boneTransformMatrix.c3.xyz) };
		return skinMatrix;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(in AnimatedSkinnedMeshComponent animatedSkinnedMesh, ref DynamicBuffer<SkinMatrix> outSkinMatricesBuf)
	{
		var rigEntity = animatedSkinnedMesh.animatedRigEntity;
		
		if (cullAnimationsTagLookup.HasComponent(rigEntity) && cullAnimationsTagLookup.IsComponentEnabled(rigEntity))
			return;

		if (!rigDefinitionLookup.TryGetComponent(rigEntity, out var rigDef))
			return;
		
		if (!entityToDataOffsetMap.TryGetValue(rigEntity, out var boneDataOffset))
			return;

		ref var boneRemapTable = ref GetBoneRemapTable(animatedSkinnedMesh.smrInfoBlob, rigDef.rigBlob);

		var absoluteBoneTransforms = RuntimeAnimationData.GetAnimationDataForRigRO(boneTransforms, boneDataOffset.bonePoseOffset, boneDataOffset.rigBoneCount);
		var skinMeshBonesInfo = animatedSkinnedMesh.smrInfoBlob;

		var rootBoneIndex = math.max(0, animatedSkinnedMesh.rootBoneIndexInRig);
		var boneObjLocalPose = absoluteBoneTransforms[rootBoneIndex];
		var entityToRootBoneTransform = math.inverse(boneObjLocalPose.ToFloat4x4());

		// Iterate over all animated bones and set pose for corresponding skin matrices
		for (int animationBoneIndex = 0; animationBoneIndex < absoluteBoneTransforms.Length; ++animationBoneIndex)
		{
			var skinnedMeshBoneIndex = boneRemapTable.rigBoneToSkinnedMeshBoneRemapIndices[animationBoneIndex];

			//	Skip bone if it is not present in skinned mesh
			if (skinnedMeshBoneIndex < 0)
				continue;

			var absBonePose = absoluteBoneTransforms[animationBoneIndex];
			var boneXForm = absBonePose.ToFloat4x4();

			ref var boneInfo = ref skinMeshBonesInfo.Value.bones[skinnedMeshBoneIndex];
			var skinMatrix = MakeSkinMatrixForBone(ref boneInfo, boneXForm, entityToRootBoneTransform);
			outSkinMatricesBuf[skinnedMeshBoneIndex] = skinMatrix;
		}
	}
}

//=================================================================================================================//

[BurstCompile]
partial struct PropagateBoneTransformToEntityTRSJob: IJobEntity
{
	[ReadOnly]
	public NativeList<BoneTransform> boneTransforms;
	[ReadOnly]
	public NativeParallelHashMap<Entity, RuntimeAnimationData.AnimatedEntityBoneDataProps> entityToDataOffsetMap;

	[NativeDisableParallelForRestriction]
	public ComponentLookup<PostTransformMatrix> postTransformMatrixLookup;
		
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute(Entity e, in AnimatorEntityRefComponent animatorRef, ref LocalTransform lt)
	{
		var boneData = RuntimeAnimationData.GetAnimationDataForRigRO(boneTransforms, entityToDataOffsetMap, animatorRef.animatorEntity);
		if (boneData.IsEmpty)
			return;
		
		if (animatorRef.boneIndexInAnimationRig >= boneData.Length)
			return;

		var boneTransform = boneData[animatorRef.boneIndexInAnimationRig];
		lt = boneTransform.ToLocalTransformComponent();
		if (postTransformMatrixLookup.HasComponent(e))
		{
			lt.Scale = 1;
			var ptm = float4x4.Scale(boneTransform.scale);
			postTransformMatrixLookup[e] = new PostTransformMatrix() { Value = ptm };
		}
	}
}

//=================================================================================================================//

[BurstCompile]
partial struct CountNumberOfNewRemapTablesJob: IJobEntity
{
	[ReadOnly]
	public ComponentLookup<RigDefinitionComponent> rigDefinitionArr;
	[ReadOnly]
	public NativeParallelHashMap<Hash128, BlobAssetReference<BoneRemapTableBlob>> rigToSkinnedMeshRemapTables;
	
	[NativeDisableUnsafePtrRestriction]
	public UnsafeAtomicCounter32 numberOfNewRemapTables;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(in AnimatedSkinnedMeshComponent asmc)
	{
		if (!rigDefinitionArr.TryGetComponent(asmc.animatedRigEntity, out var rigDef))
			return;
		
		var h = ApplyAnimationToSkinnedMeshJob.CalculateBoneRemapTableHash(asmc.smrInfoBlob, rigDef.rigBlob);
		if (!rigToSkinnedMeshRemapTables.ContainsKey(h))
			numberOfNewRemapTables.Add(1);
	}
}

//=================================================================================================================//

[BurstCompile]
unsafe struct IncreaseRigRemapTableCapacityJob: IJob
{
	public NativeParallelHashMap<Hash128, BlobAssetReference<BoneRemapTableBlob>> rigToSkinnedMeshRemapTables;
	[ReadOnly, NativeDisableUnsafePtrRestriction]
	public int *numberOfNewRemapTables;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute()
	{
		rigToSkinnedMeshRemapTables.Capacity += *numberOfNewRemapTables;
	}
}

//=================================================================================================================//

[BurstCompile]
unsafe partial struct FillRigToSkinBonesRemapTableCacheJob: IJobEntity
{
	[ReadOnly]
	public ComponentLookup<RigDefinitionComponent> rigDefinitionArr;
	public NativeParallelHashMap<Hash128, BlobAssetReference<BoneRemapTableBlob>>.ParallelWriter rigToSkinnedMeshRemapTables;
	[ReadOnly, NativeDisableUnsafePtrRestriction]
	public int *newRemapTablesCounter;

#if RUKHANKA_DEBUG_INFO
	public bool doLogging;
#endif

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(in AnimatedSkinnedMeshComponent asmc)
	{
		if (*newRemapTablesCounter == 0 || !rigDefinitionArr.TryGetComponent(asmc.animatedRigEntity, out var rigDef))
			return;
		
		MakeRigToSkinnedMeshRemapTable(asmc, rigDef);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void MakeRigToSkinnedMeshRemapTable(in AnimatedSkinnedMeshComponent sm, in RigDefinitionComponent rigDef)
	{
		//	Try cache first
		var h = ApplyAnimationToSkinnedMeshJob.CalculateBoneRemapTableHash(sm.smrInfoBlob, rigDef.rigBlob);
		if (UnsafeParallelHashMapBase<Hash128, BlobAssetReference<BoneRemapTableBlob>>
		    .TryGetFirstValueAtomic(rigToSkinnedMeshRemapTables.m_Writer.m_Buffer, h, out _, out _))
			return;

		//	Compute new remap table
		var bb = new BlobBuilder(Allocator.Temp);
		ref var brt = ref bb.ConstructRoot<BoneRemapTableBlob>();

	#if RUKHANKA_DEBUG_INFO
		ref var rnd = ref rigDef.rigBlob.Value.name;
		ref var snd = ref sm.smrInfoBlob.Value.skeletonName;
		if (doLogging)
			Debug.Log($"[FillRigToSkinBonesRemapTableCacheJob] Creating rig '{rnd.ToFixedString()}' to skinned mesh '{snd.ToFixedString()}' remap table");
	#endif
		
		var bba = bb.Allocate(ref brt.rigBoneToSkinnedMeshBoneRemapIndices, rigDef.rigBlob.Value.bones.Length);
		for (int i = 0; i < bba.Length; ++i)
		{
			bba[i] = -1;
			ref var rb = ref rigDef.rigBlob.Value.bones[i];
			var rbHash =  rb.hash;
			
			for (int j = 0; j < sm.smrInfoBlob.Value.bones.Length; ++j)
			{
				ref var bn = ref sm.smrInfoBlob.Value.bones[j];
				var bnHash = bn.hash;

				if (bnHash == rbHash)
				{ 
					bba[i] = j;
				#if RUKHANKA_DEBUG_INFO
					if (doLogging)
						Debug.Log($"[FillRigToSkinBonesRemapTableCacheJob] Remap {rb.name.ToFixedString()}->{bn.name.ToFixedString()} : {i} -> {j}");
				#endif
				}
			}
		}
		var rv = bb.CreateBlobAssetReference<BoneRemapTableBlob>(Allocator.Persistent);
		rigToSkinnedMeshRemapTables.TryAdd(h, rv);
	}
}

//=================================================================================================================//

[BurstCompile]
partial struct CopySkinnedMeshBoundsToChildRenderers: IJobEntity
{
	[ReadOnly]
	public ComponentLookup<SkinnedMeshBounds> skinnedMeshBoundsLookup;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(ref RenderBounds rb, in DeformedEntity de)
	{
		if (!skinnedMeshBoundsLookup.TryGetComponent(de.Value, out var smb))
			return;
		
		rb.Value = smb.value;
	}
}

//=================================================================================================================//

[BurstCompile]
partial struct UpdateSkinnedMeshBoundsJob: IJobEntity
{
	[ReadOnly]
	public NativeList<BoneTransform> worldBonePoses;
	[ReadOnly]
	public NativeParallelHashMap<Entity, RuntimeAnimationData.AnimatedEntityBoneDataProps> entityToDataOffsetMap;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(ref SkinnedMeshBounds rb, in AnimatedSkinnedMeshComponent asm)
	{
		var animationData = RuntimeAnimationData.GetAnimationDataForRigRO(worldBonePoses, entityToDataOffsetMap, asm.animatedRigEntity);
		
		if (animationData.Length <= asm.rootBoneIndexInRig || asm.rootBoneIndexInRig < 0)
			return;
		
		//	Skinned mesh root bone world pose
		var rootBonePose = animationData[asm.rootBoneIndexInRig];
		var invRootBonePose = BoneTransform.Inverse(rootBonePose);
		
		//	Loop over all bones and calculate extents in root bone space
		float3 minPt = float.MaxValue;
		float3 maxPt = float.MinValue;
		for (var i = asm.rootBoneIndexInRig + 1; i < animationData.Length; ++i)
		{
			var boneWorldPose = animationData[i];
			var rootBoneSpaceBonePose = BoneTransform.Multiply(invRootBonePose, boneWorldPose);
			minPt = math.min(minPt, rootBoneSpaceBonePose.pos);
			maxPt = math.max(maxPt, rootBoneSpaceBonePose.pos);
		}
		
		var aabb = new AABB()
		{
			Center = (minPt + maxPt) * 0.5f,
			Extents = (maxPt - minPt) * 0.5f,
		};
		
		//	Slightly extend result aabb to 'emulate' skin. Without proper per-vertex skin hull calculation this is reasonable approximation
		aabb.Extents *= 1.1f;
		
		rb.value = aabb;
	}
}

//=================================================================================================================//

[BurstCompile]
partial struct CopyAnimatedValuesToControllerParametersJob: IJobEntity
{
	[ReadOnly]
	public NativeParallelHashMap<Entity, RuntimeAnimationData.AnimatedEntityBoneDataProps> entityToDataOffsetMap;
	[ReadOnly]
	public NativeList<RuntimeAnimationData.GenericFloatAnimatedValue> genericAnimatedValues;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(Entity e, DynamicBuffer<AnimatorControllerParameterComponent> runtimeParams)
	{
		for (var i = 0; i < runtimeParams.Length; ++i)
		{
			var p = runtimeParams[i];
			var hash128 = new Hash128(0, p.hash, (int)BindingType.Unknown, 0xffffffff);
			var idx = RuntimeAnimationData.FindGenericAnimatedDataIndexByHash(e, entityToDataOffsetMap, genericAnimatedValues, hash128);
			if (idx >= 0)
			{
				p.value = genericAnimatedValues[idx].value;
				runtimeParams[i] = p;
			}
		}
	}
}

//=================================================================================================================//

[BurstCompile]
partial struct ApplyBlendShapeWeightsJob: IJobEntity
{
	[ReadOnly]
	public NativeParallelHashMap<Entity, RuntimeAnimationData.AnimatedEntityBoneDataProps> entityToDataOffsetMap;
	[ReadOnly]
	public NativeList<RuntimeAnimationData.GenericFloatAnimatedValue> genericAnimatedValues;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(DynamicBuffer<BlendShapeWeight> blendShapeWeights, in AnimatedSkinnedMeshComponent asm)
	{
		var e = asm.animatedRigEntity;
		for (var i = 0; i < asm.smrInfoBlob.Value.blendShapes.Length; ++i)
		{
			ref var bs = ref asm.smrInfoBlob.Value.blendShapes[i];
			var hash128 = new Hash128(asm.nameHash, bs.hash, (int)BindingType.BlendShape, 0xffffffff);
			var idx = RuntimeAnimationData.FindGenericAnimatedDataIndexByHash(e, entityToDataOffsetMap, genericAnimatedValues, hash128);
			if (idx >= 0)
			{
				blendShapeWeights[i] = new BlendShapeWeight()
				{
					Value = genericAnimatedValues[idx].value
				};
			}
		}
	}
}
}
}
