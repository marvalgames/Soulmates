using Unity.Entities;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
[UpdateBefore(typeof(RukhankaAnimationSystemGroup))]
public partial class AnimCullingSampleSystem: SystemBase
{
	protected override void OnUpdate()
	{
		var sampleConf = AnimCullingSampleConf.Instance;
		if (sampleConf == null)
			return;
		
		if (UILabelSetter_AnimationCulling.Instance != null)
		{
			sampleConf.cullingCamera.fieldOfView = UILabelSetter_AnimationCulling.Instance.floatParam1Slider.value;
		}
		
		var entitiesWithCulling = SystemAPI.QueryBuilder()
			.WithAll<CullAnimationsTag>()
			.WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
			.Build();
		
		var animatedEntities = SystemAPI.QueryBuilder()
			.WithAll<RigDefinitionComponent>()
			.WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
			.Build();
		
		if (entitiesWithCulling.IsEmpty)
		{
			if (sampleConf.enableAnimationCullingToggle.isOn)
				EntityManager.AddComponent<CullAnimationsTag>(animatedEntities);
		}
		else
		{
			if (!sampleConf.enableAnimationCullingToggle.isOn)
				EntityManager.RemoveComponent<CullAnimationsTag>(animatedEntities);
		}
		
		var skinnedMeshBoundsEntities = SystemAPI.QueryBuilder()
			.WithAll<SkinnedMeshBounds>()
			.WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
			.Build();
		
		EntityManager.SetComponentEnabled<SkinnedMeshBounds>(skinnedMeshBoundsEntities, sampleConf.enableRendererBBoxRecalculation.isOn);
	}
}
}

