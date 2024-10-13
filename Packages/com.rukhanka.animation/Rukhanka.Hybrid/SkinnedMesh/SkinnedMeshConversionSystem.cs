using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[UpdateAfter(typeof(RigDefinitionConversionSystem))]
public partial class SkinnedMeshConversionSystem : SystemBase
{
	protected override void OnUpdate()
	{
		var rootBoneIndexSetJob = new ActualizeRootBoneIndexInRig()
		{
			animEntityRefLookup = SystemAPI.GetComponentLookup<AnimatorEntityRefComponent>(true)
		};
		rootBoneIndexSetJob.ScheduleParallel();
	}
}
}
