using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class AimIKBaker: Baker<AimIKAuthoring>
{
	public override void Bake(AimIKAuthoring a)
	{
		var e = GetEntity(a, TransformUsageFlags.None);
		
		var aik = new AimIKComponent()
		{
			angleLimits = math.radians(new float2(a.angleLimitMin, a.angleLimitMax)),
			target = GetEntity(a.target, TransformUsageFlags.Dynamic),
			forwardVector = math.normalize(a.forwardVector),
			weight = a.weight,
		};

		AddComponent(e, aik);
		var aikAffectedBones = AddBuffer<AimIKAffectedBoneComponent>(e);
		
		for (var i = 0; i < a.affectedBones.Length; ++i)
		{
			var ab = a.affectedBones[i];
			if (ab.bone == null)
			{
				Debug.LogWarning($"'{a.name}': Affected bone list contains empty bone reference at index {i}.");
				continue;
			}
			
			var aimedBone = new AimIKAffectedBoneComponent()
			{
				weight = ab.weight,
				boneEntity = GetEntity(ab.bone, TransformUsageFlags.Dynamic)
			};
			
			aikAffectedBones.Add(aimedBone);
		}
	}
}
}
