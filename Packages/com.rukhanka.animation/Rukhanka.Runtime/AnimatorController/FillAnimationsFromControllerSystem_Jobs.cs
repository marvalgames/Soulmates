using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static Rukhanka.AnimatorControllerSystemJobs;

/////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{ 
public partial struct FillAnimationsFromControllerSystem
{

[BurstCompile]
struct FillAnimationsBufferJob: IJobChunk
{
	[ReadOnly]
	public BufferTypeHandle<AnimatorControllerLayerComponent> controllerLayersBufferHandle;
	[ReadOnly]
	public BufferTypeHandle<AnimatorControllerParameterComponent> controllerParametersBufferHandle;
	[ReadOnly]
	public ComponentLookup<AnimatorOverrideAnimations> animatorOverrideAnimationLookup;
	[ReadOnly]
	public EntityTypeHandle entityTypeHandle;
	[ReadOnly]
	public NativeHashMap<Hash128, BlobAssetReference<AnimationClipBlob>> animationDatabase;
	[ReadOnly]
	public NativeHashMap<Hash128, BlobAssetReference<AvatarMaskBlob>> avatarMaskDatabase;
	
	public BufferTypeHandle<AnimationToProcessComponent> animationToProcessBufferHandle;
	
	BlobAssetReference<ControllerAnimationsBlob> controllerAnimationsBlob;

/////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
	{
		var layerBuffers = chunk.GetBufferAccessor(ref controllerLayersBufferHandle);
		var parameterBuffers = chunk.GetBufferAccessor(ref controllerParametersBufferHandle);
		var animationsToProcessBuffers = chunk.GetBufferAccessor(ref animationToProcessBufferHandle);
		var entities = chunk.GetNativeArray(entityTypeHandle);

		var cee = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

		while (cee.NextEntityIndex(out var i))
		{
			var layers = layerBuffers[i].AsNativeArray();
			var parameters = parameterBuffers.Length > 0 ? parameterBuffers[i].AsNativeArray() : default;
			var e = entities[i];

			var animsBuf = animationsToProcessBuffers[i];

			AddAnimationsForEntity(ref animsBuf, layers, parameters, e);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////
	
	void AnimationsPostSetup(Span<AnimationToProcessComponent> animations, ref LayerBlob lb, int layerIndex, float weightMultiplier, float layerWeight)
	{
		//	Set blending mode and adjust animations weight according to layer weight
		for (int k = 0; k < animations.Length; ++k)
		{
			var a = animations[k];
			a.blendMode = lb.blendingMode;
			a.layerWeight = layerWeight;
			a.layerIndex = layerIndex;
			a.weight *= weightMultiplier;
			a.avatarMask = BlobDatabaseSingleton.GetBlobAsset(lb.avatarMaskBlobHash, avatarMaskDatabase);
			animations[k] = a;
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe void AddAnimationsForEntity
	(
		ref DynamicBuffer<AnimationToProcessComponent> animations,
		in NativeArray<AnimatorControllerLayerComponent> aclc,
		in NativeArray<AnimatorControllerParameterComponent> runtimeParams,
		Entity entity
	)
	{
		if (entity == Entity.Null)
			return;

		animations.Clear();

		//	Need to skip zero weight layers
		var curLayerIndex = 0;
		for (int i = 0; i < aclc.Length; ++i)
		{
			var animationCurIndex = animations.Length;

			var l = aclc[i];
			controllerAnimationsBlob = GetControllerAnimationsBlob(entity, animatorOverrideAnimationLookup, l.animations);
			
			var cb = l.controller;
			ref var lb = ref cb.Value.layers[i];
			if (l.weight == 0 || l.rtd.srcState.id < 0)
				continue;

			ref var srcStateBlob = ref lb.states[l.rtd.srcState.id];

			var srcStateWeight = 1.0f;
			var dstStateWeight = 0.0f;

			if (l.rtd.activeTransition.id >= 0)
			{
				dstStateWeight = l.rtd.activeTransition.normalizedDuration;
				srcStateWeight = (1 - dstStateWeight);
			}

			var srcStateTime = GetDurationTime(ref srcStateBlob, runtimeParams, l.rtd.srcState.normalizedDuration);

			var dstStateAnimCount = 0;
			if (l.rtd.dstState.id >= 0)
			{
				ref var dstStateBlob = ref lb.states[l.rtd.dstState.id];
				var dstStateTime = GetDurationTime(ref dstStateBlob, runtimeParams, l.rtd.dstState.normalizedDuration);
				dstStateAnimCount = AddMotionForEntity(ref animations, ref dstStateBlob.motion, runtimeParams, 1, dstStateTime);
			}
			var srcStateAnimCount = AddMotionForEntity(ref animations, ref srcStateBlob.motion, runtimeParams, 1, srcStateTime);

			var animStartPtr = (AnimationToProcessComponent*)animations.GetUnsafePtr() + animationCurIndex;
			var dstAnimsSpan = new Span<AnimationToProcessComponent>(animStartPtr, dstStateAnimCount);
			var srcAnimsSpan = new Span<AnimationToProcessComponent>(animStartPtr + dstStateAnimCount, srcStateAnimCount);

			var dstLayerMultiplier = math.select(dstStateWeight, 1, srcStateAnimCount > 0);
			var srcLayerMultiplier = math.select(srcStateWeight, 1, dstStateAnimCount > 0);
			dstStateWeight = math.select(1, dstStateWeight, srcStateAnimCount > 0);
			srcStateWeight = math.select(1, srcStateWeight, dstStateAnimCount > 0);

			AnimationsPostSetup(dstAnimsSpan, ref lb, curLayerIndex, dstStateWeight, dstLayerMultiplier * l.weight);
			AnimationsPostSetup(srcAnimsSpan, ref lb, curLayerIndex, srcStateWeight, srcLayerMultiplier * l.weight);
			++curLayerIndex;
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	void AddAnimationForEntity
	(
		ref DynamicBuffer<AnimationToProcessComponent> outAnims,
		ref MotionBlob mb,
		float weight,
		float normalizedStateTime
	)
	{
		var atp = new AnimationToProcessComponent();

		var animationHash = controllerAnimationsBlob.Value.animations[mb.animationIndex];
		atp.animation = BlobDatabaseSingleton.GetBlobAsset(animationHash, animationDatabase);
		atp.weight = weight;
		atp.time = normalizedStateTime;
		atp.motionId = mb.hash;
		outAnims.Add(atp);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	void AddMotionsFromBlendtree
	(
		in NativeList<ScriptedAnimator.MotionIndexAndWeight> miws,
		ref DynamicBuffer<AnimationToProcessComponent> outAnims,
		in NativeArray<AnimatorControllerParameterComponent> runtimeParams,
		ref BlobArray<ChildMotionBlob> motions,
		float weight,
		float normalizedStateTime
	)
	{
		for (int i = 0; i < miws.Length; ++i)
		{
			var miw = miws[i];
			ref var m = ref motions[miw.motionIndex];
			AddMotionForEntity(ref outAnims, ref m.motion, runtimeParams, weight * miw.weight, normalizedStateTime);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	int AddMotionForEntity
	(
		ref DynamicBuffer<AnimationToProcessComponent> outAnims,
		ref MotionBlob mb,
		in NativeArray<AnimatorControllerParameterComponent> runtimeParams,
		float weight,
		float normalizedStateTime
	)
	{
		var startLen = outAnims.Length;
		
		switch (mb.type)
		{
		case MotionBlob.Type.None:
			break;
		case MotionBlob.Type.AnimationClip:
			AddAnimationForEntity(ref outAnims, ref mb, weight, normalizedStateTime);
			break;
		}

		var childMotions = ScriptedAnimator.GetChildMotionsList(ref mb, runtimeParams);
		if (childMotions.IsCreated)
		{
			AddMotionsFromBlendtree(childMotions, ref outAnims, runtimeParams, ref mb.blendTree.motions, weight, normalizedStateTime);
		}

		return outAnims.Length - startLen;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	float GetDurationTime(ref StateBlob sb, in NativeArray<AnimatorControllerParameterComponent> runtimeParams, float normalizedDuration)
	{
		var timeDuration = normalizedDuration;
		if (sb.timeParameterIndex >= 0)
		{
			timeDuration = runtimeParams[sb.timeParameterIndex].FloatValue;
		}
		var stateCycleOffset = sb.cycleOffset;
		if (sb.cycleOffsetParameterIndex >= 0)
		{
			stateCycleOffset = runtimeParams[sb.cycleOffsetParameterIndex].FloatValue;
		}
		timeDuration += stateCycleOffset;
		return timeDuration;
	}
}
}
}
