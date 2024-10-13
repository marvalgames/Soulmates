using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public partial class RigDefinitionConversionSystem
{
	[BurstCompile]
	[WithOptions(EntityQueryOptions.IncludePrefab)]
	partial struct CreateComponentDatasJob: IJobEntity
	{
		[ReadOnly]
		public ComponentLookup<BakingOnlyEntity> bakingOnlyLookup;

		public EntityCommandBuffer.ParallelWriter ecb;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		void Execute(Entity e, [ChunkIndexInQuery] int chunkIndex, ref RigDefinitionComponent rdc, DynamicBuffer<BoneEntityRef> boneEntityRefs)
		{
			for (int l = 0; l < boneEntityRefs.Length; ++l)
			{
				var boneEntityRef = boneEntityRefs[l];
				var boneEntity = boneEntityRef.boneEntity;
				if (boneEntity != Entity.Null && !bakingOnlyLookup.HasComponent(boneEntity))
				{
					var animatorEntityRefComponent = new AnimatorEntityRefComponent()
					{
						animatorEntity = e,
						boneIndexInAnimationRig = boneEntityRef.rigBoneIndex
					};
					ecb.AddComponent(chunkIndex, boneEntity, animatorEntityRefComponent);
				}
			}
		}
	}
} 
}
