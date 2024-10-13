
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[DisableAutoCreation]
[UpdateAfter(typeof(AnimationCullingSystem))]
public partial struct AnimationProcessSystem: ISystem
{
	EntityQuery animatedObjectQuery;

	NativeList<int3> bonePosesOffsetsArr;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	public void OnCreate(ref SystemState ss)
	{
		InitializeRuntimeData(ref ss);

		bonePosesOffsetsArr = new (Allocator.Persistent);

		var eqb0 = new EntityQueryBuilder(Allocator.Temp)
		.WithAll<RigDefinitionComponent, AnimationToProcessComponent>();
		animatedObjectQuery = ss.GetEntityQuery(eqb0);
		
		ss.RequireForUpdate(animatedObjectQuery);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	public void OnDestroy(ref SystemState ss)
	{
		bonePosesOffsetsArr.Dispose();

		if (SystemAPI.TryGetSingleton<RuntimeAnimationData>(out var rad))
		{
			rad.Dispose();
			ss.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<RuntimeAnimationData>());
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void InitializeRuntimeData(ref SystemState ss)
	{
		var rad = RuntimeAnimationData.MakeDefault();
		ss.EntityManager.CreateSingleton(rad, "Rukhanka.RuntimeAnimationData");
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle PrepareComputationData(ref SystemState ss, NativeArray<int> chunkBaseEntityIndices, ref RuntimeAnimationData runtimeData, NativeList<Entity> entitiesArr, JobHandle dependsOn)
	{
		var rigDefinitionTypeHandle = SystemAPI.GetComponentTypeHandle<RigDefinitionComponent>(true);
		var cullAnimationsTagComponentLookup = SystemAPI.GetComponentLookup<CullAnimationsTag>(true);
		var atpsBufHandle = SystemAPI.GetBufferTypeHandle<AnimationToProcessComponent>(true);
		
		//	Calculate bone offsets per entity
		var calcBoneOffsetsJob = new CalculateBoneOffsetsJob()
		{
			chunkBaseEntityIndices = chunkBaseEntityIndices,
			bonePosesOffsets = bonePosesOffsetsArr,
			animationToProcessBufferLookup = atpsBufHandle,
			rigDefinitionTypeHandle = rigDefinitionTypeHandle,
			cullAnimationsTagLookup = cullAnimationsTagComponentLookup,
			entities = entitiesArr,
		};
		
		var calcBoneOffsetsJobJH = calcBoneOffsetsJob.ScheduleParallel(animatedObjectQuery, dependsOn);
		
		//	Do prefix sum to calculate absolute offsets
		var prefixSumJob = new DoPrefixSumJob()
		{
			boneOffsets = bonePosesOffsetsArr
		};

		var prefixSumJH = prefixSumJob.Schedule(calcBoneOffsetsJobJH);
		
		//	Resize data buffers depending on current workload
		var resizeDataBuffersJob = new ResizeDataBuffersJob()
		{
			boneOffsets = bonePosesOffsetsArr,
			runtimeData = runtimeData
		};

		var resizeDataBuffersJH = resizeDataBuffersJob.Schedule(prefixSumJH);
		
		//	Fill boneToEntityArr with proper values
		var boneToEntityArrFillJob = new CalculatePerBoneInfoJob()
		{
			bonePosesOffsets = bonePosesOffsetsArr,
			boneToEntityIndices = runtimeData.boneToEntityArr,
			chunkBaseEntityIndices = chunkBaseEntityIndices,
			entities = entitiesArr,
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap.AsParallelWriter()
		};

		var boneToEntityJH = boneToEntityArrFillJob.ScheduleParallel(animatedObjectQuery, resizeDataBuffersJH);
		return boneToEntityJH;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle EmitAnimationEvents(ref SystemState ss, JobHandle dependsOn)
	{
		var debugLog = false;
		var dt = SystemAPI.Time.DeltaTime;
		
	#if RUKHANKA_DEBUG_INFO
		if (SystemAPI.TryGetSingleton<DebugConfigurationComponent>(out var dc))
			debugLog = dc.logAnimationEvents;
	#endif
		
		var emitAnimationEventsJob = new EmitAnimationEventsJob()
		{
			doDebugLogging = debugLog,
			deltaTime = dt
		};
		var jh = emitAnimationEventsJob.ScheduleParallel(dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle MakeProcessedAnimationsSnapshot(ref SystemState ss, JobHandle dependsOn)
	{
		var makeProcessedAnimationsSnapshotJob = new MakeProcessedAnimationsSnapshotJob() { };
		var jh = makeProcessedAnimationsSnapshotJob.ScheduleParallel(dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle AnimationCalculation(ref SystemState ss, NativeList<Entity> entitiesArr, in RuntimeAnimationData runtimeData, JobHandle dependsOn)
	{
		var animationToProcessBufferLookup = SystemAPI.GetBufferLookup<AnimationToProcessComponent>(true);
		var rootMotionAnimationStateBufferLookupRW = SystemAPI.GetBufferLookup<RootMotionAnimationStateComponent>();

		var rigDefsArr = animatedObjectQuery.ToComponentDataListAsync<RigDefinitionComponent>(ss.WorldUpdateAllocator, out var rigDefsLookupJH);
		var dataGatherJH = JobHandle.CombineDependencies(rigDefsLookupJH, dependsOn);

		var computeAnimationsJob = new ComputeBoneAnimationJob()
		{
			animationsToProcessLookup = animationToProcessBufferLookup,
			entityArr = entitiesArr,
			rigDefs = rigDefsArr,
			boneTransformFlagsArr = runtimeData.boneTransformFlagsHolderArr,
			animatedBonesBuffer = runtimeData.animatedBonesBuffer,
			boneToEntityArr = runtimeData.boneToEntityArr,
			rootMotionAnimStateBufferLookup = rootMotionAnimationStateBufferLookupRW,
		};

		var jh = computeAnimationsJob.Schedule(runtimeData.animatedBonesBuffer, 16, dataGatherJH);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle ProcessGenericCurves(ref SystemState ss, in RuntimeAnimationData runtimeData, JobHandle dependsOn)
	{
		var genericCurveProcessJob = new ProcessGenericCurvesJob()
		{
			genericAnimatedValues = runtimeData.genericCurveAnimatedValuesBuffer,
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap,
		};
		var jh = genericCurveProcessJob.ScheduleParallel(dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle CopyEntityBonesToAnimationTransforms(ref SystemState ss, ref RuntimeAnimationData runtimeData, JobHandle dependsOn)
	{
		var rigDefinitionLookup = SystemAPI.GetComponentLookup<RigDefinitionComponent>(true);
		var parentComponentLookup = SystemAPI.GetComponentLookup<Parent>();
			
		//	Now take available entity transforms as ref poses overrides
		var copyEntityBoneTransforms = new CopyEntityBoneTransformsToAnimationBuffer()
		{
			rigDefComponentLookup = rigDefinitionLookup,
			boneTransformFlags = runtimeData.boneTransformFlagsHolderArr,
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap,
			animatedBoneTransforms = runtimeData.animatedBonesBuffer,
			parentComponentLookup = parentComponentLookup,
		};

		var jh = copyEntityBoneTransforms.ScheduleParallel(dependsOn);
		return jh;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle MakeAbsoluteBoneTransforms(ref SystemState ss, in RuntimeAnimationData runtimeData, JobHandle dependsOn)
	{
		var rigDefTypeHandle = SystemAPI.GetComponentTypeHandle<RigDefinitionComponent>(true);
		var entityTypeHandle = SystemAPI.GetEntityTypeHandle();
		
		var makeAbsTransformsJob = new MakeAbsoluteTransformsJob()
		{
			localBoneTransforms = runtimeData.animatedBonesBuffer,
			worldBoneTransforms = runtimeData.worldSpaceBonesBuffer,
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap,
			boneTransformFlags = runtimeData.boneTransformFlagsHolderArr,
			entityTypeHandle = entityTypeHandle,
			rigDefTypeHandle = rigDefTypeHandle
		};

		var query = SystemAPI.QueryBuilder().WithAll<RigDefinitionComponent>().Build();
		var jh = makeAbsTransformsJob.ScheduleParallel(query, dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle ComputeRootMotion(ref SystemState ss, in RuntimeAnimationData runtimeData, JobHandle dependsOn)
	{
		var computeRootMotionJob = new ComputeRootMotionJob()
		{
			animatedBonePoses = runtimeData.animatedBonesBuffer,
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap
		};

		var jh = computeRootMotionJob.ScheduleParallel(dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	public void OnUpdate(ref SystemState ss)
	{
		var entityCount = animatedObjectQuery.CalculateEntityCount();
		ref var runtimeData = ref SystemAPI.GetSingletonRW<RuntimeAnimationData>().ValueRW;
		
		bonePosesOffsetsArr.Resize(entityCount + 1, NativeArrayOptions.UninitializedMemory);
		var chunkBaseEntityIndices = animatedObjectQuery.CalculateBaseEntityIndexArrayAsync(ss.WorldUpdateAllocator, ss.Dependency, out var baseIndexCalcJH);
		var entitiesArr = animatedObjectQuery.ToEntityListAsync(ss.WorldUpdateAllocator, ss.Dependency, out var entityArrJH);

		//	Emit animation events based on current and previously processed animations
		var emitAnimationEventsJH = EmitAnimationEvents(ref ss, ss.Dependency);
		
		//	Make a snapshot of current frame animations, to use it in next frame as previously processed jobs
		var makeProcessedAnimationsSnapshotJH = MakeProcessedAnimationsSnapshot(ref ss, emitAnimationEventsJH);

		var combinedJH = JobHandle.CombineDependencies(baseIndexCalcJH, entityArrJH, makeProcessedAnimationsSnapshotJH);
		
		//	Define array with bone pose offsets for calculated bone poses
		var calcBoneOffsetsJH = PrepareComputationData(ref ss, chunkBaseEntityIndices, ref runtimeData, entitiesArr, combinedJH);

		//	User curve calculus
		var userCurveProcessJH = ProcessGenericCurves(ref ss, runtimeData, calcBoneOffsetsJH);

		//	Spawn jobs for animation calculation
		var computeAnimationJH = AnimationCalculation(ref ss, entitiesArr, runtimeData, userCurveProcessJH);
		
		//	Copy entities poses into animation buffer for non-animated parts
		var copyEntityTransformsIntoAnimationBufferJH = CopyEntityBonesToAnimationTransforms(ref ss, ref runtimeData, computeAnimationJH);
		
		//	Compute root motion
		var rootMotionJH = ComputeRootMotion(ref ss, runtimeData, copyEntityTransformsIntoAnimationBufferJH);
		
		//	Make world space transforms out of local bone poses
		var makeAbsTransformsJH = MakeAbsoluteBoneTransforms(ref ss, runtimeData, rootMotionJH);

		ss.Dependency = makeAbsTransformsJH;
	}
}
}
