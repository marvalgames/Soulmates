#if !RUKHANKA_NO_DEFORMATION_SYSTEM

using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
[UpdateInGroup(typeof(RukhankaDeformationSystemGroup))]
[CreateAfter(typeof(RegisterMaterialsAndMeshesSystem))]
public partial struct SkinnedMeshPreparationSystem: ISystem
{
	SharedComponentTypeHandle<RenderMeshArray> renderMeshArrayTypeHandle;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void OnCreate(ref SystemState ss)
	{
		var deformationRuntimeData = DeformationRuntimeData.Construct(ref ss);
		ss.EntityManager.CreateSingleton(deformationRuntimeData, "Rukhanka Deformation Runtime Data");
		
		renderMeshArrayTypeHandle = ss.GetSharedComponentTypeHandle<RenderMeshArray>();
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void OnDestroy(ref SystemState ss)
	{
		ref var deformationRuntimeData = ref SystemAPI.GetSingletonRW<DeformationRuntimeData>().ValueRW;
		deformationRuntimeData.Dispose();
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	public void OnUpdate(ref SystemState ss)
	{
		renderMeshArrayTypeHandle.Update(ref ss);
		ref var deformationRuntimeData = ref SystemAPI.GetSingletonRW<DeformationRuntimeData>().ValueRW;
		
		//	Reset data used in current frame
		var resetFrameDataJH = ResetNewFrameData(ref ss, ref deformationRuntimeData, ss.Dependency);
		
		//	Gather meshes that new in this frame and absent in previous frames
		var getNewSkinnedMeshesJH = GatherNewMeshes(ref ss, ref deformationRuntimeData, resetFrameDataJH);
		
		//	Register new meshes in internal skinned mesh database
		var registerNewSkinnedMeshesJH = RegisterNewMeshes(ref ss, ref deformationRuntimeData, getNewSkinnedMeshesJH);
		
		//	Compute all frame deformed meshes skin matrices offsets in global GPU buffer
		var computeFrameSkinMatrixDataJH = ComputeFrameSkinnedMeshData(ref ss, ref deformationRuntimeData, resetFrameDataJH);
		
		var combinedJobHandle = JobHandle.CombineDependencies(computeFrameSkinMatrixDataJH, registerNewSkinnedMeshesJH);
		ss.Dependency = combinedJobHandle;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle ResetNewFrameData(ref SystemState ss, ref DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var frameSkinnedMeshesQuery = SystemAPI.QueryBuilder()
			.WithAll<Rukhanka.SkinMatrix>()
			.Build();
		
		var resetFrameDataJob = new ResetFrameDataJob()
		{
			frameSkinnedMeshesCount = frameSkinnedMeshesQuery.CalculateEntityCount(),
			frameSkinMatrixCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameSkinMatrixCount)),
			frameBlendShapeWeightCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameBlendShapeWeightsCount)),
			newSkinnedMeshesToRegister = drd.newSkinnedMeshesToRegister,
			entityToSMRFrameDataMap = drd.entityToSMRFrameDataMap,
			frameDeformedVertexCount = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameDeformedVerticesCount)),
		};
		
		var resetFrameDataJH = resetFrameDataJob.Schedule(dependsOn);
		return resetFrameDataJH;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle GatherNewMeshes(ref SystemState ss, ref DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var deformedSubMeshQuery = SystemAPI.QueryBuilder()
			.WithAll<MaterialMeshInfo, DeformedMeshIndex, AnimatedRendererComponent>()
			.Build();
		
		var getNewSkinnedMeshesJob = new GetFrameNewSkinnedMeshesJob()
		{
			existingSkinnedMeshes = drd.registeredSkinnedMeshesMap,
			newSkinnedMeshes = drd.newSkinnedMeshesToRegister.AsParallelWriter(),
			renderMeshArrays = drd.renderMeshArrays,
			materialMeshInfoTypeHandle = SystemAPI.GetComponentTypeHandle<MaterialMeshInfo>(true),
			renderMeshArrayTypeHandle = renderMeshArrayTypeHandle,
			animatedSkinnedMeshLookup = SystemAPI.GetComponentLookup<AnimatedSkinnedMeshComponent>(true),
			animatedRendererTypeHandle = SystemAPI.GetComponentTypeHandle<AnimatedRendererComponent>(true),
		};
		
		var getNewSkinnedMeshesJH = getNewSkinnedMeshesJob.ScheduleParallel(deformedSubMeshQuery, dependsOn);
		return getNewSkinnedMeshesJH;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle RegisterNewMeshes(ref SystemState ss, ref DeformationRuntimeData drd, JobHandle dependsOn)
	{
		var registerNewSkinnedMeshesJob = new RegisterNewSkinnedMeshesJob()
		{
			existingSkinnedMeshes = drd.registeredSkinnedMeshesMap,
			newSkinnedMeshes = drd.newSkinnedMeshesToRegister,
			totalSkinnedVerticesCount = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.totalSkinnedVerticesCount)),
			totalBoneWeightsCount = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.totalBoneWeightsCount)),
			totalBlendShapeVerticesCount = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.totalBlendShapeVerticesCount)),
			maximumVerticesAcrossAllRegisteredMeshes = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.maximumVerticesAcrossAllRegisteredMeshes)),
		};
		
		var registerNewSkinnedMeshesJH = registerNewSkinnedMeshesJob.Schedule(dependsOn);
		return registerNewSkinnedMeshesJH;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe JobHandle ComputeFrameSkinnedMeshData(ref SystemState ss, ref DeformationRuntimeData drd, JobHandle dependsOn)
	{
		//	Skinned meshes with LODs. If culling context is present then skin only meshes in visible LOD level
		if (SystemAPI.TryGetSingleton<AnimationCullingContext>(out var animationCullingContext))
		{
			var computeFrameSkinnedMeshesWithLODsJob = new ComputeFrameSkinnedMeshesWithLODsJob()
			{
				skinMatrixOffsetCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameSkinMatrixCount)),
				blendShapeWeightOffsetCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameBlendShapeWeightsCount)),
				frameDeformedVerticesCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameDeformedVerticesCount)),
				cullAnimationsTagLookup = SystemAPI.GetComponentLookup<CullAnimationsTag>(true),
				entityToSMRFrameData = drd.entityToSMRFrameDataMap.AsParallelWriter(),
				animatedSkinnedMeshLookup = SystemAPI.GetComponentLookup<AnimatedSkinnedMeshComponent>(true),
				skinMatrixBufferLookup = SystemAPI.GetBufferLookup<Rukhanka.SkinMatrix>(true),
				blendShapeWeightBufferLookup = SystemAPI.GetBufferLookup<Rukhanka.BlendShapeWeight>(true),
				lodRangeLookup = SystemAPI.GetComponentLookup<LODRange>(true),
				lodWorldRefPointLookup = SystemAPI.GetComponentLookup<LODWorldReferencePoint>(true),
				lodAffectors = animationCullingContext.lodAffectors,
				
				//	Disable culling in editor world
			#if UNITY_EDITOR
				isEditorWorld = ss.WorldUnmanaged.Flags == WorldFlags.Editor
			#endif	
			};
		
			dependsOn = computeFrameSkinnedMeshesWithLODsJob.ScheduleParallel(dependsOn);
		}
		//	Otherwise skin all meshes
		else
		{
			var deformedMeshQuery = SystemAPI.QueryBuilder()
				.WithAll<AnimatedSkinnedMeshComponent>()
				.WithAny<Rukhanka.SkinMatrix, Rukhanka.BlendShapeWeight>()
				.Build();
			
			var computeFrameSkinnedMeshesJob = new ComputeFrameSkinnedMeshesJob()
			{
				skinMatrixOffsetCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameSkinMatrixCount)),
				blendShapeWeightOffsetCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameBlendShapeWeightsCount)),
				frameDeformedVerticesCounter = new UnsafeAtomicCounter32(UnsafeUtility.AddressOf(ref drd.frameDeformedVerticesCount)),
				cullAnimationsTagLookup = SystemAPI.GetComponentLookup<CullAnimationsTag>(true),
				entityToSMRFrameData = drd.entityToSMRFrameDataMap.AsParallelWriter(),
				skinMatrixBufferLookup = SystemAPI.GetBufferLookup<Rukhanka.SkinMatrix>(true),
				blendShapeWeightBufferLookup = SystemAPI.GetBufferLookup<Rukhanka.BlendShapeWeight>(true),
				
				//	Disable culling in editor world
			#if UNITY_EDITOR
				isEditorWorld = ss.WorldUnmanaged.Flags == WorldFlags.Editor
			#endif	
			};
			dependsOn = computeFrameSkinnedMeshesJob.ScheduleParallel(deformedMeshQuery, dependsOn);
		}
		
	
		return dependsOn;
	}
}
}

#endif