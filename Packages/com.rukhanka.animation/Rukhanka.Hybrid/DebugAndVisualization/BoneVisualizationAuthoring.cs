using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
class BoneVisualizationAuthoring: MonoBehaviour
{
	public Color boneColor = new Color(0, 1, 1, 0.3f);
	public Color outlineColor = new Color(0, 1, 1, 1);
}

/////////////////////////////////////////////////////////////////////////////////

class BoneVisualizationBaker: Baker<BoneVisualizationAuthoring>
{
	public override void Bake(BoneVisualizationAuthoring a)
	{
	#if !UNITY_SERVER
		var bvc = new BoneVisualizationComponent()
		{
			colorTri = new float4(a.boneColor.r, a.boneColor.g, a.boneColor.b, a.boneColor.a),
			colorLines = new float4(a.outlineColor.r, a.outlineColor.g, a.outlineColor.b, a.outlineColor.a),
			individualConfig = true
		};

		var e = GetEntity(TransformUsageFlags.Dynamic);
		AddComponent(e, bvc);
	#endif
		
	#if (RUKHANKA_NO_DEBUG_DRAWER && RUKHANKA_DEBUG_INFO)
		Debug.LogWarning($"'{a.name}' rig visualization was requested, but DebugDrawer is compiled out via RUKHANKA_NO_DEBUG_DRAWER script symbol. No visualization is available.");
	#endif
	}
}
}
