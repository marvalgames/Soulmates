#if !RUKHANKA_NO_DEBUG_DRAWER
using Rukhanka.DebugDrawer;
#endif
using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[DisableAutoCreation]
[UpdateAfter(typeof(BoneVisualizationColoringSystem))]
public partial class BoneVisualizationSystem: SystemBase
{
	EntityQuery boneVisualizeQuery;
	
/////////////////////////////////////////////////////////////////////////////////

	protected override void OnCreate()
	{
		boneVisualizeQuery = SystemAPI.QueryBuilder()
			.WithAll<RigDefinitionComponent, BoneVisualizationComponent>()
			.Build();
		
		RequireForUpdate(boneVisualizeQuery);
	}
	
/////////////////////////////////////////////////////////////////////////////////

	protected override void OnUpdate()
	{
#if !RUKHANKA_NO_DEBUG_DRAWER
		if (!SystemAPI.TryGetSingleton<RuntimeAnimationData>(out var runtimeData))
			return;
		
		if (!SystemAPI.TryGetSingletonRW<Drawer>(out var dd))
			return;
		
		var renderBonesJob = new RenderBonesJob()
		{
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap,
			bonePoses = runtimeData.worldSpaceBonesBuffer,
			drawer = dd.ValueRW
		};

		renderBonesJob.ScheduleParallel();
#endif
	}
}
}
