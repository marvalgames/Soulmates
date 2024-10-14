using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(RukhankaAnimationSystemGroup))]
[UpdateBefore(typeof(AnimationProcessSystem))]
public partial class ScriptedAnimatorSampleSystem: SystemBase
{
	[BurstCompile]
	partial struct ScriptedAnimatorSampleJob: IJobEntity
	{
		[ReadOnly]
		public NativeHashMap<Hash128, BlobAssetReference<AnimationClipBlob>> animDB;
		public float weight;
		public float animTime;
		public int2 animationIndices;
		public bool blending;
		
		void Execute(ref DynamicBuffer<AnimationToProcessComponent> atps, ScriptedAnimatorSampleUsedAnimationsComponent c)
		{
			ScriptedAnimator.ResetAnimationState(ref atps);
			animDB.TryGetValue(c.clips[animationIndices.x], out var clip0Blob);
			animDB.TryGetValue(c.clips[animationIndices.y], out var clip1Blob);
			if (blending)
				ScriptedAnimator.BlendTwoAnimations(ref atps, clip0Blob, clip1Blob, animTime, weight);
			else
				ScriptedAnimator.PlayAnimation(ref atps, clip0Blob, animTime, weight);
		}
	}
	
//=================================================================================================================//

	protected override void OnUpdate()
	{
		var cfg = ScriptedAnimatorSampleConf.Instance;
		if (cfg == null)
			return;
		
		if (!SystemAPI.TryGetSingleton<BlobDatabaseSingleton>(out var blobDB))
			return;

		var job = new ScriptedAnimatorSampleJob()
		{
			animDB = blobDB.animations,
			weight = cfg.weight.value,
			animTime = cfg.animationTime,
			blending = cfg.doBlending,
			animationIndices = cfg.animationIndices
		};
		
		job.ScheduleParallel();
	}
}
}


