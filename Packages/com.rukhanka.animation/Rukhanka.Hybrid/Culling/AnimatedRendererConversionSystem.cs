using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Entities.Hybrid.Baking;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[TemporaryBakingType]
internal struct AnimatedRendererBakingComponent: IComponentData
{
	public Entity animatorEntity;
	public bool needUpdateRenderBounds;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(SkinnedMeshConversionSystem))]
public partial class AnimatedRendererBakingSystem : SystemBase
{
	[BurstCompile]
	partial struct CreateAnimatedRendererComponentsJob: IJobEntity
	{
		[ReadOnly]
		public BufferLookup<AdditionalEntitiesBakingData> additionalEntitiesBufferLookup;
		[ReadOnly]
		public ComponentLookup<AnimatedSkinnedMeshComponent> animatedSkinnedMeshComponentLookup;
		
		public EntityCommandBuffer ecb;
		
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		void Execute(Entity e, AnimatedRendererBakingComponent arbc)
		{
			var arc = new AnimatedRendererComponent()
			{
				animatorEntity = arbc.animatorEntity,
				skinnedMeshEntity = e,
			};
			
			//	If this is skinned mesh renderer, add AnimatedRendererComponent to its additional mesh render entities
			if (animatedSkinnedMeshComponentLookup.HasComponent(e))
			{
				if (additionalEntitiesBufferLookup.TryGetBuffer(e, out var additionalEntitiesBuf))
				{
					foreach (var ae in additionalEntitiesBuf)
					{
						ecb.AddComponent(ae.Value, arc);
						if (arbc.needUpdateRenderBounds)
							ecb.AddComponent<ShouldUpdateBoundingBoxTag>(ae.Value);
					}
				}
			}
			else
			{
				ecb.AddComponent(e, arc);
			}
		}
	}

//=================================================================================================================//

	protected override void OnUpdate()
	{
		var ecb = new EntityCommandBuffer(CheckedStateRef.WorldUpdateAllocator);
		var q = SystemAPI.QueryBuilder()
			.WithAll<AnimatedRendererBakingComponent>()
			.WithOptions(EntityQueryOptions.IncludePrefab)
			.Build();
		
		var createAnimatedRendererComponentsJob = new CreateAnimatedRendererComponentsJob()
		{
			ecb	= ecb,
			additionalEntitiesBufferLookup = SystemAPI.GetBufferLookup<AdditionalEntitiesBakingData>(true),
			animatedSkinnedMeshComponentLookup = SystemAPI.GetComponentLookup<AnimatedSkinnedMeshComponent>(true)
		};
		
		createAnimatedRendererComponentsJob.Run(q);
		
		ecb.Playback(EntityManager);
	}
} 
}
