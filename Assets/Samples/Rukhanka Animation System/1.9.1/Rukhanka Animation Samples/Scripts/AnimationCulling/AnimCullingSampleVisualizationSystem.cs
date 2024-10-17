#if !RUKHANKA_NO_DEBUG_DRAWER
using Rukhanka.DebugDrawer;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
[BurstCompile]
partial struct BoundsRenderJob: IJobEntity
{
	public Drawer dd;
	public uint colorVisible;
	public uint colorInvisible;
	public uint colorGeneric;
	
	[ReadOnly]
	public ComponentLookup<CullAnimationsTag> cullAnimationsTagLookup;
	
	void Execute(in WorldRenderBounds bb, in AnimatedRendererComponent arc)
	{
		var xform = new RigidTransform(quaternion.identity, bb.Value.Center);
		var color = colorGeneric;
		if (cullAnimationsTagLookup.HasComponent(arc.animatorEntity))
		{
			color = cullAnimationsTagLookup.IsComponentEnabled(arc.animatorEntity) ? colorInvisible : colorVisible;
		}
		dd.DrawWireCuboid(bb.Value.Size, color, xform);
	}
}
	
/////////////////////////////////////////////////////////////////////////////////
	
[BurstCompile]
struct FrustumRenderJob: IJob
{
	public NativeArray<float4x4> cullingMatrices;
	public Drawer dd;
	
	public void Execute()
	{
		foreach (var cm in cullingMatrices)
		{
			dd.DrawWireFrustum(cm, 0xffff);
		}
	}
}
	
/////////////////////////////////////////////////////////////////////////////////
	
partial class AnimCullingSampleVisualizationSystem: SystemBase
{
	protected override void OnUpdate()
	{
		if (AnimCullingSampleConf.Instance == null || AnimationCullingConfig.Instance == null)
			return;
		
		if (!SystemAPI.TryGetSingletonRW<Drawer>(out var dd))
			return;
		
		var acc = AnimationCullingConfig.Instance;
		
		if (acc.cullingCameras.Length > 0)
		{
			var cullingMatrices = CollectionHelper.CreateNativeArray<float4x4>(acc.cullingCameras.Length, CheckedStateRef.WorldUpdateAllocator);
			for (var i = 0; i < acc.cullingCameras.Length; ++i)
			{
				cullingMatrices[i] = acc.cullingCameras[i].cullingMatrix;
			}
			
			var frustumRenderJob = new FrustumRenderJob()
			{
				dd = dd.ValueRW,
				cullingMatrices = cullingMatrices
			};
			
			frustumRenderJob.Run();
		}
		
		var bbRenderJob = new BoundsRenderJob()
		{
			dd = dd.ValueRW,
			colorGeneric = 0xffffffff,
			colorInvisible = 0xff0000ff,
			colorVisible = 0x00ff00ff,
			cullAnimationsTagLookup = SystemAPI.GetComponentLookup<CullAnimationsTag>(true)
		};
		
		Dependency = bbRenderJob.ScheduleParallel(Dependency);
	}
}
}
#endif
