using Unity.Entities;
using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class FABRIKBaker: Baker<FABRIKAuthoring>
{
	public override void Bake(FABRIKAuthoring a)
	{
		if (a.target == null)
		{
			Debug.LogError($"'{a.name}': IK target is not set.");
			return;
		}
		
		if (a.tip == null)
		{
			Debug.LogError($"'{a.name}': IK tip (end of chain) is not set.");
			return;
		}
		
		if (a.tip == a.target)
		{
			Debug.LogError($"'{a.name}': IK tip and target must not be same transform.");
			return;
		}
		
		if (a.tip == a.transform)
		{
			Debug.LogError($"'{a.name}': IK tip and root must not be same transform.");
			return;
		}

		if (!CheckReachability(a.transform, a.tip))
		{
			Debug.LogError($"'{a.name}' transform cannot be reached from '{a.tip}'. Check your skeleton hierarchy.");
			return;
		}
		
		var ikComponent = new FABRIKComponent()
		{
			target = GetEntity(a.target, TransformUsageFlags.Dynamic),
			tip = GetEntity(a.tip, TransformUsageFlags.Dynamic),
			numIterations = a.numIterations,
			threshold = a.threshold,
			weight = a.weight
		};

		var e = GetEntity(TransformUsageFlags.Dynamic);
		AddComponent(e, ikComponent);
	}
	
////////////////////////////////////////////////////////////////////////////////////////

	bool CheckReachability(Transform root, Transform tip)
	{
		var isReachable = false;
		var curNode = tip;
		
		while (!isReachable && curNode != null)
		{
			isReachable = curNode == root;
			curNode = curNode.parent;
		}

		return isReachable;
	}
}
}
