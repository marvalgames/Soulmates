using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
#if RUKHANKA_WITH_NETCODE
using Unity.NetCode;
#endif

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[DisableAutoCreation]
[UpdateAfter(typeof(AnimationApplicationSystem))]
public partial struct BoneVisualizationColoringSystem: ISystem
{
	[BurstCompile]
	[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
	partial struct AdjustBoneVisualizationPropertiesJob: IJobEntity
	{
		public DebugConfigurationComponent dcc;
		public float4 colorTri;
		public float4 colorLines;
		
		void Execute(EnabledRefRW<BoneVisualizationComponent> bvcEnabled, RefRW<BoneVisualizationComponent> bvc)
		{
			if (bvc.ValueRW.individualConfig)
				return;

			bvc.ValueRW.colorTri = colorTri;
			bvc.ValueRW.colorLines = colorLines;

			bvcEnabled.ValueRW = dcc.visualizeAllRigs;
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////

	public void OnCreate(ref SystemState ss)
	{
#if !RUKHANKA_DEBUG_INFO
		ss.Enabled = false;
#endif
	}
	
/////////////////////////////////////////////////////////////////////////////////
	
	[BurstCompile]
	public void OnUpdate(ref SystemState ss)
	{
#if RUKHANKA_DEBUG_INFO
		if (!SystemAPI.TryGetSingleton<DebugConfigurationComponent>(out var dcc))
			return;

		var isServer = false;
		#if RUKHANKA_WITH_NETCODE
			isServer = ss.WorldUnmanaged.IsServer();
		#endif
		
		var colorJob = new AdjustBoneVisualizationPropertiesJob()
		{
			dcc = dcc,
			colorLines = isServer ? dcc.serverRigColorLines : dcc.clientRigColorLines,
			colorTri = isServer ? dcc.serverRigColorTri : dcc.clientRigColorTri
		};

		colorJob.ScheduleParallel();
#endif
	}
}
}
