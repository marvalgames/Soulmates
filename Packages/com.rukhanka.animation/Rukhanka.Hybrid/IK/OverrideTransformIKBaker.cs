using Unity.Entities;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class OverrideTransformIKBaker: Baker<OverrideTransformIKAuthoring>
{
	public override void Bake(OverrideTransformIKAuthoring a)
	{
		var e = GetEntity(a, TransformUsageFlags.None);
		var otik = new OverrideTransformIKComponent()
		{
			target = GetEntity(a.target, TransformUsageFlags.Dynamic),
			positionWeight = a.positionWeight,
			rotationWeight = a.rotationWeight,
		};
		
		AddComponent(e, otik);
	}
}
}
