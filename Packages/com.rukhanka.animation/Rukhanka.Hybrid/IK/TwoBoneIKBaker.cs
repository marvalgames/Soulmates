using Unity.Entities;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class TwoBoneIKBaker: Baker<TwoBoneIKAuthoring>
{
	public override void Bake(TwoBoneIKAuthoring a)
	{
		var tbik = new TwoBoneIKComponent()
		{
			mid = GetEntity(a.mid, TransformUsageFlags.Dynamic),
			target = GetEntity(a.target, TransformUsageFlags.Dynamic),
			tip = GetEntity(a.tip, TransformUsageFlags.Dynamic),
			midBentHint = GetEntity(a.midBentHint, TransformUsageFlags.Dynamic),
			weight = a.weight
		};

		var e = GetEntity(TransformUsageFlags.Dynamic);
		AddComponent(e, tbik);
	}
}
}
