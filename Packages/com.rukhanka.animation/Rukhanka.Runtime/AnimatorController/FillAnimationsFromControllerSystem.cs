using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{ 

[DisableAutoCreation]
[BurstCompile]
[UpdateAfter(typeof(AnimatorControllerSystem<AnimatorControllerQuery>))]
public partial struct FillAnimationsFromControllerSystem: ISystem
{
	EntityQuery fillAnimationsBufferQuery;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////
	
	[BurstCompile]
	public void OnCreate(ref SystemState ss)
	{
		fillAnimationsBufferQuery = SystemAPI.QueryBuilder()
			.WithAll<AnimatorControllerLayerComponent, AnimationToProcessComponent>()
			.Build();
		
		ss.RequireForUpdate(fillAnimationsBufferQuery);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	public void OnUpdate(ref SystemState ss)
	{
		var entityTypeHandle = SystemAPI.GetEntityTypeHandle();
		var controllerLayersBufferHandleRO = SystemAPI.GetBufferTypeHandle<AnimatorControllerLayerComponent>(true);
		var controllerParametersBufferHandleRO = SystemAPI.GetBufferTypeHandle<AnimatorControllerParameterComponent>(true);
		var animatorOverrideAnimationsLookup = SystemAPI.GetComponentLookup<AnimatorOverrideAnimations>(true);
		var animationToProcessBufferHandle = SystemAPI.GetBufferTypeHandle<AnimationToProcessComponent>();
		var animDBSingleton = SystemAPI.GetSingleton<BlobDatabaseSingleton>();

		var fillAnimationsBufferJob = new FillAnimationsBufferJob()
		{
			controllerLayersBufferHandle = controllerLayersBufferHandleRO,
			controllerParametersBufferHandle = controllerParametersBufferHandleRO,
			animationToProcessBufferHandle = animationToProcessBufferHandle,
			animatorOverrideAnimationLookup = animatorOverrideAnimationsLookup,
			entityTypeHandle = entityTypeHandle,
			animationDatabase = animDBSingleton.animations,
			avatarMaskDatabase = animDBSingleton.avatarMasks
		};

		ss.Dependency = fillAnimationsBufferJob.ScheduleParallel(fillAnimationsBufferQuery, ss.Dependency);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////

	internal static BlobAssetReference<ControllerAnimationsBlob> GetControllerAnimationsBlob
	(
		Entity e,
		ComponentLookup<AnimatorOverrideAnimations> animatorOverrideAnimationLookup,
		BlobAssetReference<ControllerAnimationsBlob> cab
	)
	{
		if (animatorOverrideAnimationLookup.TryGetComponent(e, out var animationOverrides) && animatorOverrideAnimationLookup.IsComponentEnabled(e))
		{
			//	Merge controller animations and override animations
			var bb = new BlobBuilder(Allocator.Temp);
			ref var mergedBlobAsset = ref bb.ConstructRoot<ControllerAnimationsBlob>();
			var animsArr = bb.Allocate(ref mergedBlobAsset.animations, cab.Value.animations.Length);
			for (var i = 0; i < animsArr.Length; ++i)
			{
				var overrideAnim = animationOverrides.value.Value.animations[i];
				animsArr[i] = overrideAnim.IsValid ? overrideAnim : cab.Value.animations[i];
			}
			return bb.CreateBlobAssetReference<ControllerAnimationsBlob>(Allocator.Temp);
		}
		return cab;
	}
}
}
