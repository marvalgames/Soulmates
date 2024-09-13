#if !RUKHANKA_NO_DEBUG_DRAWER
using Rukhanka.DebugDrawer;
#endif

using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[DisableAutoCreation]
[UpdateAfter(typeof(AnimationCullingContextUpdateSystem))]
partial struct AnimationCullingSystem: ISystem
{
	EntityQuery cullAnimationTagQuery;

/////////////////////////////////////////////////////////////////////////////////
	
	[BurstCompile]
	public void OnCreate(ref SystemState ss)
	{
		cullAnimationTagQuery = SystemAPI.QueryBuilder()
			.WithAll<CullAnimationsTag>()
			.WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
			.Build();
		
		ss.RequireForUpdate(cullAnimationTagQuery);
	}

/////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	public void OnUpdate(ref SystemState ss)
	{
		if (!SystemAPI.TryGetSingleton<AnimationCullingContext>(out var actx))
		{
			if (!cullAnimationTagQuery.IsEmptyIgnoreFilter)
			{
				Debug.LogError($"Animation culling config is absent on scene, but some entities request animation culling functionality. <a href=\"https://docs.rukhanka.com/Optimizing%20Rukhanka/animation_frustum_culling#animation-culling-environment-setup\">Please properly configure the culling environment!</a>");
			}
			return;
		}
		
		var resetCullingStateJob = new ResetAnimationCullingJob()
		{
			cullAnimationsTypeHandle = SystemAPI.GetComponentTypeHandle<CullAnimationsTag>()
		};
		
		var resetCullingStateJH = resetCullingStateJob.ScheduleParallel(cullAnimationTagQuery, ss.Dependency);
		
		var wrbTypeHandle = SystemAPI.GetComponentTypeHandle<WorldRenderBounds>(true);
		var chunkWrbTypeHandle = SystemAPI.GetComponentTypeHandle<ChunkWorldRenderBounds>(true);
		var animatedRendererTypeHandle = SystemAPI.GetComponentTypeHandle<AnimatedRendererComponent>(true);
		var cullAnimationsTagLookup = SystemAPI.GetComponentLookup<CullAnimationsTag>();
			
		var cullAnimationsJob = new CullAnimationsJob()
		{
			actx = actx,
			wrbTypeHandle = wrbTypeHandle,
			chunkWrbTypeHandle = chunkWrbTypeHandle,
			animatedRendererTypeHandle = animatedRendererTypeHandle,
			cullAnimationsTagLookup = cullAnimationsTagLookup
		};
		
		var cullQuery = SystemAPI.QueryBuilder()
			.WithAllChunkComponent<ChunkWorldRenderBounds>()
			.WithAll<WorldRenderBounds, AnimatedRendererComponent>()
			.Build();
		
		ss.Dependency = cullAnimationsJob.ScheduleParallel(cullQuery, resetCullingStateJH);
		
	#if (RUKHANKA_DEBUG_INFO && !RUKHANKA_NO_DEBUG_DRAWER)
		SystemAPI.TryGetSingletonRW<Drawer>(out var dd);
		
		var debugDrawJob = new DebugDrawRendererBoundingBoxes()
		{
			dd = dd.ValueRW,
			actx = actx,
			wrbTypeHandle = wrbTypeHandle,
			chunkWrbTypeHandle = chunkWrbTypeHandle,
			animatedRendererTypeHandle = animatedRendererTypeHandle,
			cullAnimationsTagLookup = cullAnimationsTagLookup
		};
		
		ss.Dependency = debugDrawJob.ScheduleParallel(cullQuery, ss.Dependency);
	#endif
	}
}
}
