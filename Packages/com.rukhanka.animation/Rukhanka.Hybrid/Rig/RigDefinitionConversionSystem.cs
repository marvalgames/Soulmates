using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Hybrid.Baking;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[RequireMatchingQueriesForUpdate]
[UpdateAfter(typeof(BakingOnlyEntityAuthoringBakingSystem))]
public partial class RigDefinitionConversionSystem : SystemBase
{
	protected override void OnUpdate()
	{
		using var ecb = new EntityCommandBuffer(Allocator.TempJob);
		var bakingOnlyEntityLookup = SystemAPI.GetComponentLookup<BakingOnlyEntity>(true);

		var createComponentDatasJob = new CreateComponentDatasJob()
		{
			ecb = ecb.AsParallelWriter(),
			bakingOnlyLookup = bakingOnlyEntityLookup
		};

		createComponentDatasJob.ScheduleParallel();
		Dependency.Complete();
		
		ecb.Playback(EntityManager);
	}
} 
}
