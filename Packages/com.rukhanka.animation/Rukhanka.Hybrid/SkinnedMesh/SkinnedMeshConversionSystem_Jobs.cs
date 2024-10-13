using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public partial class SkinnedMeshConversionSystem
{

//=================================================================================================================//

	[BurstCompile]
	[WithOptions(EntityQueryOptions.IncludePrefab)]
	partial struct ActualizeRootBoneIndexInRig: IJobEntity
	{
		[ReadOnly]
		public ComponentLookup<AnimatorEntityRefComponent> animEntityRefLookup;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		void Execute(ref AnimatedSkinnedMeshComponent asmc, in SkinnedMeshRendererRootBoneEntity rbe)
		{
			if (animEntityRefLookup.HasComponent(rbe.value))
			{
				var are = animEntityRefLookup[rbe.value];
				asmc.rootBoneIndexInRig = are.boneIndexInAnimationRig;
			}
		}
	}
}
}
