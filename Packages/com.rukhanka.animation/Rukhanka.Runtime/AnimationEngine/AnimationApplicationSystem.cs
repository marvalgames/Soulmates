using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Deformations;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[DisableAutoCreation]
[UpdateAfter(typeof(RukhankaAnimationInjectionSystemGroup))]
partial struct AnimationApplicationSystem: ISystem
{
	private EntityQuery
		boneObjectEntitiesWithParentQuery,
		boneObjectEntitiesNoParentQuery;

	NativeParallelHashMap<Hash128, BlobAssetReference<BoneRemapTableBlob>> rigToSkinnedMeshRemapTables;
	int newRemapTablesCounter;

/////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	public void OnCreate(ref SystemState ss)
	{
		var eqb0 = new EntityQueryBuilder(Allocator.Temp)
			.WithAll<AnimatorEntityRefComponent, Parent>()
			.WithAllRW<LocalTransform>();
		boneObjectEntitiesWithParentQuery = ss.GetEntityQuery(eqb0);

		var eqb1 = new EntityQueryBuilder(Allocator.Temp)
			.WithAll<AnimatorEntityRefComponent>()
			.WithNone<Parent>()
			.WithAllRW<LocalTransform>();
		boneObjectEntitiesNoParentQuery = ss.GetEntityQuery(eqb1);
		
		var rq = new EntityQueryBuilder(Allocator.Temp)
			.WithAll<AnimatorEntityRefComponent>()
			.Build(ref ss);
		ss.RequireForUpdate(rq);

		rigToSkinnedMeshRemapTables = new NativeParallelHashMap<Hash128, BlobAssetReference<BoneRemapTableBlob>>(128, Allocator.Persistent);
	}
	
/////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	public void OnDestroy(ref SystemState ss)
	{
		foreach (var kv in rigToSkinnedMeshRemapTables)
			kv.Value.Dispose();	
		rigToSkinnedMeshRemapTables.Dispose();
	}

/////////////////////////////////////////////////////////////////////////////////

    [BurstCompile]
    public void OnUpdate(ref SystemState ss)
    {
		ref var runtimeData = ref SystemAPI.GetSingletonRW<RuntimeAnimationData>().ValueRW;

		var fillRigToSkinnedMeshRemapTablesJH = FillRigToSkinBonesRemapTableCache(ref ss, ss.Dependency);
		
		//	Copy animated values to corresponding controller parameters
		var copyAnimatedValuesToParametersJH = CopyAnimatedValuesToControllerParameters(ref ss, runtimeData, ss.Dependency);
		//	Set blend weights
		var applyBlendWeightsJH = ApplyBlendShapeWeights(ref ss, runtimeData, ss.Dependency);
		var combinedJH = JobHandle.CombineDependencies(copyAnimatedValuesToParametersJH, applyBlendWeightsJH);

		//	Propagate local animated transforms to the entities with and without parents
		var propagateTRSToEntitiesWithParentsJH = PropagateAnimatedBonesToEntitiesTRS(ref ss, runtimeData, boneObjectEntitiesWithParentQuery, true, ss.Dependency);
		var propagateTRSToEntitiesNoParentsJH = PropagateAnimatedBonesToEntitiesTRS(ref ss, runtimeData, boneObjectEntitiesNoParentQuery, false, propagateTRSToEntitiesWithParentsJH);
		
		//	Make corresponding skin matrices for all skinned meshes
		var jh = JobHandle.CombineDependencies(fillRigToSkinnedMeshRemapTablesJH, propagateTRSToEntitiesNoParentsJH);
		var applySkinJH = ApplySkinning(ref ss, runtimeData, jh);

		//	Update render bounds for meshes that request this
		var updateRenderBoundsJH = UpdateRenderBounds(ref ss, runtimeData, ss.Dependency);

		ss.Dependency = JobHandle.CombineDependencies(applySkinJH, updateRenderBoundsJH, combinedJH);
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle FillRigToSkinBonesRemapTableCache(ref SystemState ss, JobHandle dependsOn)
	{
		var rigDefinitionComponentLookup = SystemAPI.GetComponentLookup<RigDefinitionComponent>(true);

	#if RUKHANKA_DEBUG_INFO
		SystemAPI.TryGetSingleton<DebugConfigurationComponent>(out var dc);
	#endif
		newRemapTablesCounter = 0;
		
		//	Count new remap tables count
		var countNumberOfNewRemapTablesJob = new CountNumberOfNewRemapTablesJob()
		{
			rigDefinitionArr = rigDefinitionComponentLookup,
			numberOfNewRemapTables = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref newRemapTablesCounter)),
			rigToSkinnedMeshRemapTables = rigToSkinnedMeshRemapTables
		};
		var countNumberOfNewRemapTablesJH = countNumberOfNewRemapTablesJob.ScheduleParallel(dependsOn);
		
		//	Reserve necessary space in remap table cache
		var increaseRigRemapTableCapacityJob = new IncreaseRigRemapTableCapacityJob()
		{
			numberOfNewRemapTables = (int*)UnsafeUtility.AddressOf(ref newRemapTablesCounter),
			rigToSkinnedMeshRemapTables = rigToSkinnedMeshRemapTables
		};
		var	increaseRigRemapTableCapacityJH = increaseRigRemapTableCapacityJob.Schedule(countNumberOfNewRemapTablesJH);
		
		//	Fill table cache with new tables
		var fillRigToSkinBonesRemapTableCacheJob = new FillRigToSkinBonesRemapTableCacheJob()
		{
			rigDefinitionArr = rigDefinitionComponentLookup,
			rigToSkinnedMeshRemapTables = rigToSkinnedMeshRemapTables.AsParallelWriter(),
			newRemapTablesCounter = (int*)UnsafeUtility.AddressOf(ref newRemapTablesCounter),
		#if RUKHANKA_DEBUG_INFO
			doLogging = dc.logAnimationCalculationProcesses
		#endif
		};

		var rv = fillRigToSkinBonesRemapTableCacheJob.ScheduleParallelByRef(increaseRigRemapTableCapacityJH);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle UpdateRenderBounds(ref SystemState ss, in RuntimeAnimationData runtimeData, JobHandle dependsOn)
	{
		var updateSkinnedMeshBoundsJob = new UpdateSkinnedMeshBoundsJob()
		{
			worldBonePoses = runtimeData.worldSpaceBonesBuffer,
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap,
		};
		
		var updateSkinnedMeshBoundsJH = updateSkinnedMeshBoundsJob.ScheduleParallel(dependsOn);
		
		var skinnedMeshBoundsComponentLookup = SystemAPI.GetComponentLookup<SkinnedMeshBounds>(true);
		var copyBoundsToChildRenderersJob = new CopySkinnedMeshBoundsToChildRenderers()
		{
			skinnedMeshBoundsLookup = skinnedMeshBoundsComponentLookup
		};
		
		var q = SystemAPI.QueryBuilder()
			.WithAll<RenderBounds, DeformedEntity, ShouldUpdateBoundingBoxTag>()
			.Build();
		
		var copyBoundsToChildRenderersJH = copyBoundsToChildRenderersJob.ScheduleParallel(q, updateSkinnedMeshBoundsJH);
		return copyBoundsToChildRenderersJH;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle PropagateAnimatedBonesToEntitiesTRS(ref SystemState ss, in RuntimeAnimationData runtimeData, EntityQuery eq, bool withParents, JobHandle dependsOn)
	{
		var propagateAnimationJob = new PropagateBoneTransformToEntityTRSJob()
		{
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap,
			boneTransforms = withParents ? runtimeData.animatedBonesBuffer : runtimeData.worldSpaceBonesBuffer,
			postTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(),
		};

		var jh = propagateAnimationJob.ScheduleParallel(eq, dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	JobHandle CopyAnimatedValuesToControllerParameters(ref SystemState ss, in RuntimeAnimationData runtimeData, JobHandle dependsOn)
	{
		var copyAnimValuesToControllerParamsJob = new CopyAnimatedValuesToControllerParametersJob()
		{
			genericAnimatedValues = runtimeData.genericCurveAnimatedValuesBuffer,
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap,
		};
		
		var jh = copyAnimValuesToControllerParamsJob.ScheduleParallel(dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle ApplyBlendShapeWeights(ref SystemState ss, in RuntimeAnimationData runtimeData, JobHandle dependsOn)
	{
		var applyBlendWeightsJob = new ApplyBlendShapeWeightsJob()
		{
			genericAnimatedValues = runtimeData.genericCurveAnimatedValuesBuffer,
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap,
		};
		
		var jh = applyBlendWeightsJob.ScheduleParallel(dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle ApplySkinning(ref SystemState ss, in RuntimeAnimationData runtimeData, JobHandle dependsOn)
	{
		var rigDefinitionComponentLookup = SystemAPI.GetComponentLookup<RigDefinitionComponent>(true);
		var cullAnimationsTagComponentLookup = SystemAPI.GetComponentLookup<CullAnimationsTag>(true);

		var animationApplyJob = new ApplyAnimationToSkinnedMeshJob()
		{
			boneTransforms = runtimeData.worldSpaceBonesBuffer,
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap,
			rigDefinitionLookup = rigDefinitionComponentLookup,
			rigToSkinnedMeshRemapTables = rigToSkinnedMeshRemapTables,
			cullAnimationsTagLookup = cullAnimationsTagComponentLookup
		};
		
		var jh = animationApplyJob.ScheduleParallel(dependsOn);
		return jh;
	}
}
}
