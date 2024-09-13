
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Serialization;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
	public struct DebugConfigurationComponent: IComponentData
	{
		public bool logAnimatorControllerProcesses;
		public bool logAnimationCalculationProcesses;
		public bool logAnimationEvents;
		public bool logAnimatorControllerEvents;

		public bool visualizeAllRigs;
		public float4 clientRigColorTri;
		public float4 clientRigColorLines;
		public float4 serverRigColorTri;
		public float4 serverRigColorLines;

/////////////////////////////////////////////////////////////////////////////////

		public static DebugConfigurationComponent Default()
		{
			var rv = new DebugConfigurationComponent()
			{
				clientRigColorTri = new float4(0, 1, 1, 0.3f),
				clientRigColorLines = new float4(0, 1, 1, 1),
				serverRigColorTri = new float4(1, 1, 0, 0.3f),
				serverRigColorLines = new float4(1, 1, 0, 1),
			};
			return rv;
		}
	}
}

