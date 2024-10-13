using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
public class RukhankaDebugConfiguration: MonoBehaviour
{
	[Header("Animator Controller System")]
	public bool logAnimatorControllerProcesses;
	public bool logAnimatorControllerEvents;

	[Header("Animation Process System")]
	public bool logAnimationCalculationProcesses;
	public bool logAnimationEvents;

	[Header("Bone Visualization")]
	public bool visualizeAllRigs;
	public Color boneColor = new Color(0, 1, 1, 0.3f);
	public Color outlineColor = new Color(0, 1, 1, 1);
	public Color serverRigBoneColor = new Color(1, 1, 0, 0.3f);
	public Color serverRigOutlineColor = new Color(1, 1, 0, 1f);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public class DebugConfigurationBaker: Baker<RukhankaDebugConfiguration>
{
	public override void Bake(RukhankaDebugConfiguration a)
	{
		var dcc = new DebugConfigurationComponent()
		{
			logAnimatorControllerProcesses = a.logAnimatorControllerProcesses,
			logAnimationCalculationProcesses = a.logAnimationCalculationProcesses,
			
			logAnimationEvents = a.logAnimationEvents,
			logAnimatorControllerEvents = a.logAnimatorControllerEvents,

			visualizeAllRigs = a.visualizeAllRigs,
			clientRigColorLines = new float4(a.outlineColor.r, a.outlineColor.g, a.outlineColor.b, a.outlineColor.a),
			clientRigColorTri = new float4(a.boneColor.r, a.boneColor.g, a.boneColor.b, a.boneColor.a),
			serverRigColorLines = new float4(a.serverRigOutlineColor.r, a.serverRigOutlineColor.r, a.serverRigOutlineColor.b, a.serverRigOutlineColor.a),
			serverRigColorTri = new float4(a.serverRigOutlineColor.r, a.serverRigBoneColor.r, a.serverRigBoneColor.b, a.serverRigBoneColor.a)
		};

		var e = GetEntity(TransformUsageFlags.None);
		AddComponent(e, dcc);
		
	#if (RUKHANKA_NO_DEBUG_DRAWER && RUKHANKA_DEBUG_INFO)
		if (a.visualizeAllRigs)
			Debug.LogWarning("All rigs visualization was requested, but DebugDrawer is compiled out via RUKHANKA_NO_DEBUG_DRAWER script symbol. No visualization is available.");
	#endif
	}
}
}

