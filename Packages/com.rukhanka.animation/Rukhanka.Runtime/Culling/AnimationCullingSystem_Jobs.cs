#if !RUKHANKA_NO_DEBUG_DRAWER
using Rukhanka.DebugDrawer;
#endif

using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Collections;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
partial struct AnimationCullingSystem
{
	
[BurstCompile]
struct CullAnimationsJob: IJobChunk
{
	[ReadOnly]
	public AnimationCullingContext actx;
	[ReadOnly]
	public ComponentTypeHandle<ChunkWorldRenderBounds> chunkWrbTypeHandle;
	[ReadOnly]
	public ComponentTypeHandle<WorldRenderBounds> wrbTypeHandle;
	[ReadOnly]
	public ComponentTypeHandle<AnimatedRendererComponent> animatedRendererTypeHandle;
	
	[NativeDisableParallelForRestriction]
	public ComponentLookup<CullAnimationsTag> cullAnimationsTagLookup;

/////////////////////////////////////////////////////////////////////////////////

	public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
	{
		var cee = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

		var chunkWRB = chunk.GetChunkComponentData(ref chunkWrbTypeHandle);
		var isChunkVisible = IsAxisAlignedBoxVisible(chunkWRB.Value, actx);
		
		var wrbArr = chunk.GetNativeArray(ref wrbTypeHandle);
		var arArr = chunk.GetNativeArray(ref animatedRendererTypeHandle);
		
		while (cee.NextEntityIndex(out var i) && isChunkVisible)
		{
			var arEntity = arArr[i].animatorEntity;
			
			if (arEntity == Entity.Null || !cullAnimationsTagLookup.HasComponent(arEntity))
				continue;
			
			var cullAnimsAlreadyDisabled = !cullAnimationsTagLookup.IsComponentEnabled(arEntity);
			if (cullAnimsAlreadyDisabled)
			{
				continue;
			}
			
			var wrb = wrbArr[i].Value;
			var isVisible = IsAxisAlignedBoxVisible(wrb, actx);
			cullAnimationsTagLookup.SetComponentEnabled(arEntity, !isVisible);
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////

	internal static bool IsAxisAlignedBoxVisible(in AABB wrb, in AnimationCullingContext actx)
	{
		var isVisible = false;
		for (var j = 0; j < actx.cullingVolumePlaneRanges.Length && !isVisible; ++j)
		{
			var planeRange = actx.cullingVolumePlaneRanges[j];
			var isVisibleForPlaneRange = true;
			for (var i = 0; i < planeRange.y && isVisibleForPlaneRange; ++i)
			{
				var pln = actx.cullingPlanes[i + planeRange.x];
				var rg0 = pln.xyz * wrb.Extents;
				var rg = math.dot(math.abs(rg0), 1);
				var distance = math.dot(pln, new float4(wrb.Center, 1));
				isVisibleForPlaneRange = distance > -rg;
			}
			isVisible |= isVisibleForPlaneRange;
		}
		return isVisible;
	}
}

//=================================================================================================================//

#if !RUKHANKA_NO_DEBUG_DRAWER
[BurstCompile]
struct DebugDrawRendererBoundingBoxes: IJobChunk
{
	public Drawer dd;
	
	[ReadOnly]
	public AnimationCullingContext actx;
	[ReadOnly]
	public ComponentTypeHandle<ChunkWorldRenderBounds> chunkWrbTypeHandle;
	[ReadOnly]
	public ComponentTypeHandle<WorldRenderBounds> wrbTypeHandle;
	[ReadOnly]
	public ComponentTypeHandle<AnimatedRendererComponent> animatedRendererTypeHandle;
	
	[NativeDisableParallelForRestriction]
	public ComponentLookup<CullAnimationsTag> cullAnimationsTagLookup;
	
/////////////////////////////////////////////////////////////////////////////////

	public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
	{
	#if RUKHANKA_DEBUG_INFO
		var cee = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

		var chunkWRB = chunk.GetChunkComponentData(ref chunkWrbTypeHandle);
		var isChunkVisible = CullAnimationsJob.IsAxisAlignedBoxVisible(chunkWRB.Value, actx);
		
		if (actx.drawSceneBoundingBoxes)
		{
			var chunkBBColor = isChunkVisible ? actx.visibleChunkColor : actx.invisibleChunkColor;
			var xformChunk = new RigidTransform(quaternion.identity, chunkWRB.Value.Center);
			dd.DrawWireCuboid(chunkWRB.Value.Size, chunkBBColor, xformChunk);
		}
		
		var wrbArr = chunk.GetNativeArray(ref wrbTypeHandle);
		var arArr = chunk.GetNativeArray(ref animatedRendererTypeHandle);
		
		while (cee.NextEntityIndex(out var i) && isChunkVisible)
		{
			var arEntity = arArr[i].animatorEntity;
			
			if (arEntity == Entity.Null || !cullAnimationsTagLookup.HasComponent(arEntity))
				continue;
			
			var isEntityVisible = !cullAnimationsTagLookup.IsComponentEnabled(arEntity);
			
			var wrb = wrbArr[i].Value;
			if (actx.drawSceneBoundingBoxes)
			{
				var cl = isEntityVisible ? actx.visibleRendererColor : actx.invisibleRendererColor;
				var xform = new RigidTransform(quaternion.identity, wrb.Center);
				dd.DrawWireCuboid(wrb.Size, cl, xform);
			}
		}
		
	#endif
	}
}
#endif

//=================================================================================================================//

[BurstCompile]
struct ResetAnimationCullingJob: IJobChunk
{
	public ComponentTypeHandle<CullAnimationsTag> cullAnimationsTypeHandle;
	
/////////////////////////////////////////////////////////////////////////////////

	public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
	{
	#if ENTITIES_V120_OR_NEWER
		chunk.SetComponentEnabledForAll(ref cullAnimationsTypeHandle, true);
	#else
		var cee = new ChunkEntityEnumerator(false, chunkEnabledMask, chunk.Count);
		while (cee.NextEntityIndex(out var i))
		{
			chunk.SetComponentEnabled(ref cullAnimationsTypeHandle, i, true);
		}
	#endif
	}
}
}
}
