
using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

//=================================================================================================================//

[assembly: InternalsVisibleTo("Rukhanka.Tests")]

namespace Rukhanka
{
partial struct AnimationProcessSystem
{
	
[BurstCompile]
public struct ComputeBoneAnimationJob: IJobParallelForDefer
{
	[NativeDisableParallelForRestriction]
	public NativeList<BoneTransform> animatedBonesBuffer;
	[NativeDisableParallelForRestriction]
	public NativeList<ulong> boneTransformFlagsArr;
	[ReadOnly]
	public NativeList<int3> boneToEntityArr;
	[ReadOnly]
	public BufferLookup<AnimationToProcessComponent> animationsToProcessLookup;
	[ReadOnly]
	public NativeList<RigDefinitionComponent> rigDefs;
	[ReadOnly]
	public NativeList<Entity> entityArr;
	
	[NativeDisableParallelForRestriction]
	public BufferLookup<RootMotionAnimationStateComponent> rootMotionAnimStateBufferLookup;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute(int globalBoneIndex)
	{
		var boneToEntityIndex = boneToEntityArr[globalBoneIndex];
		var (rigBoneIndex, entityIndex, rigBoneCount) = (boneToEntityIndex.y & 0xffff, boneToEntityIndex.x, boneToEntityIndex.y >> 16);
		var e = entityArr[entityIndex];

		var rigDef = rigDefs[entityIndex];
		var rigBlobAsset = rigDef.rigBlob;
		ref var rb = ref rigBlobAsset.Value.bones[rigBoneIndex];
		var animationsToProcess = animationsToProcessLookup[e];

		//	Early exit if no animations
		if (animationsToProcess.IsEmpty)
			return;

		var transformFlags = RuntimeAnimationData.GetAnimationTransformFlagsRW(boneToEntityArr, boneTransformFlagsArr, globalBoneIndex, rigBoneCount);
		GetHumanRotationDataForSkeletonBone(out var humanBoneInfo, ref rigBlobAsset.Value.humanData, rigBoneIndex);

		Span<float> layerWeights = stackalloc float[animationsToProcess.Length];
		var refPosWeight = CalculateFinalLayerWeights(layerWeights, animationsToProcess, rb.hash, rb.humanBodyPart);
		float3 totalWeights = refPosWeight;

		var blendedBonePose = BoneTransform.Scale(rb.refPose, refPosWeight);

		var rootMotionDeltaBone = rigDef.applyRootMotion && rigBoneIndex == 0;
		PrepareRootMotionStateBuffers(e, animationsToProcess, out var curRootMotionState, out var newRootMotionState, rootMotionDeltaBone);

		for (int i = 0; i < animationsToProcess.Length; ++i)
		{
			var atp = animationsToProcess[i];
			if (atp.animation == BlobAssetReference<AnimationClipBlob>.Null)
				continue;

			var animTime = NormalizeAnimationTime(atp.time, ref atp.animation.Value);

			var layerWeight = layerWeights[atp.layerIndex];
			if (layerWeight == 0) continue;

			var boneNameHash = rb.hash;
			if (rigDef.applyRootMotion && (rigBlobAsset.Value.rootBoneIndex == rigBoneIndex || rigBoneIndex == 0))
				ModifyBoneHashForRootMotion(ref boneNameHash);
			
			var animationBoneIndex = GetBoneIndexByHash(ref atp.animation.Value, boneNameHash);
			var isAnimationAdditive = atp.blendMode == AnimationBlendingMode.Additive;

			if (Hint.Likely(animationBoneIndex >= 0))
			{
				// Loop Pose calculus for all bones except root motion
				var calculateLoopPose = atp.animation.Value.loopPoseBlend && rigBoneIndex != 0;
				var additiveReferencePoseTime = math.select(-1.0f, atp.animation.Value.additiveReferencePoseTime, isAnimationAdditive);
				
				ref var boneAnimation = ref atp.animation.Value.bones[animationBoneIndex];
				var (bonePose, flags) = SampleAnimation(ref boneAnimation, animTime, atp, calculateLoopPose, additiveReferencePoseTime, humanBoneInfo);
				SetTransformFlags(flags, transformFlags, rigBoneIndex);

				float3 modWeight = flags * atp.weight * layerWeight;
				totalWeights += math.select(modWeight, 0, isAnimationAdditive);

				if (rootMotionDeltaBone)
					ProcessRootMotionDeltas(ref bonePose, ref boneAnimation, atp, curRootMotionState, ref newRootMotionState);
				
				MixPoses(ref blendedBonePose, bonePose, modWeight, atp.blendMode);
			}
		}

		//	Reference pose for root motion delta should be identity
		var boneRefPose = Hint.Unlikely(rootMotionDeltaBone) ? BoneTransform.Identity() : rb.refPose;
		
		BoneTransformMakePretty(ref blendedBonePose, boneRefPose, totalWeights);
		animatedBonesBuffer[globalBoneIndex] = blendedBonePose;

		if (rootMotionDeltaBone)
			SetRootMotionStateToComponentBuffer(newRootMotionState, e);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static void ModifyBoneHashForRootMotion(ref Hash128 h)
	{
		h.Value.z = 0xbaad;
		h.Value.w = 0xf00d;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int GetBoneIndexByHash(ref AnimationClipBlob acb, in Hash128 boneHash)
	{
		var queryIndex = PerfectHash<Hash128PerfectHashed>.QueryPerfectHashTable(ref acb.bonesPerfectHashSeedTable, boneHash);
		if (queryIndex >= acb.bones.Length || queryIndex < 0)
			return -1;
		var candidateBoneHash = acb.bones[queryIndex].hash;
		return candidateBoneHash == boneHash ? queryIndex : -1;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void PrepareRootMotionStateBuffers
	(
		Entity e,
		in DynamicBuffer<AnimationToProcessComponent> atps,
		out NativeArray<RootMotionAnimationStateComponent> curRootMotionState,
		out NativeList<RootMotionAnimationStateComponent> newRootMotionState,
		bool isRootMotionBone
	)
	{
		curRootMotionState = default;
		newRootMotionState = default;

		if (Hint.Likely(!isRootMotionBone)) return;

		if (rootMotionAnimStateBufferLookup.HasBuffer(e))
			curRootMotionState = rootMotionAnimStateBufferLookup[e].AsNativeArray();

		newRootMotionState = new NativeList<RootMotionAnimationStateComponent>(atps.Length, Allocator.Temp);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void ProcessRootMotionDeltas
	(
		ref BoneTransform bonePose,
		ref BoneClipBlob boneAnimation,
		in AnimationToProcessComponent atp,
		in NativeArray<RootMotionAnimationStateComponent> curRootMotionState,
		ref NativeList<RootMotionAnimationStateComponent> newRootMotionState
	)
	{
		//	Special care for root motion animation loops
		HandleRootMotionLoops(ref bonePose, ref boneAnimation, atp);
	
		BoneTransform rootMotionPrevPose = bonePose;

		// Find animation history in history buffer
		var historyBufferIndex = 0;
		for (; curRootMotionState.IsCreated && historyBufferIndex < curRootMotionState.Length && curRootMotionState[historyBufferIndex].uniqueMotionId != atp.motionId; ++historyBufferIndex){ }

		var initialFrame = historyBufferIndex >= curRootMotionState.Length;

		if (Hint.Unlikely(!initialFrame))
		{
			rootMotionPrevPose = curRootMotionState[historyBufferIndex].animationState;
		}

		var rmasc = new RootMotionAnimationStateComponent() { uniqueMotionId = atp.motionId, animationState = bonePose };
		newRootMotionState.Add(rmasc);

		var invPrevPose = BoneTransform.Inverse(rootMotionPrevPose);
		var deltaPose = BoneTransform.Multiply(invPrevPose, bonePose);

		bonePose = deltaPose;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void SetRootMotionStateToComponentBuffer(in NativeList<RootMotionAnimationStateComponent> newRootMotionData, Entity e)
	{
		rootMotionAnimStateBufferLookup[e].CopyFrom(newRootMotionData.AsArray());
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void SetTransformFlags(float3 flags, in AnimationTransformFlags flagArr, int boneIndex)
	{
		if (flags.x > 0)
			flagArr.SetTranslationFlag(boneIndex);
		if (flags.y > 0)
			flagArr.SetRotationFlag(boneIndex);
		if (flags.z > 0)
			flagArr.SetScaleFlag(boneIndex);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void GetHumanRotationDataForSkeletonBone(out HumanRotationData rv, ref BlobPtr<HumanData> hd, int rigBoneIndex)
	{
		rv = default;
		if (hd.IsValid)
		{
			rv = hd.Value.humanRotData[rigBoneIndex];
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	internal static float3 MuscleRangeToRadians(float3 minA, float3 maxA, float3 muscle)
	{
		//	Map [-1; +1] range into [minRot; maxRot]
		var negativeRange = math.min(muscle, 0);
		var positiveRange = math.max(0, muscle);
		var negativeRot = math.lerp(0, minA, -negativeRange);
		var positiveRot = math.lerp(0, maxA, +positiveRange);

		var rv = negativeRot + positiveRot;
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void MuscleValuesToQuaternion(in HumanRotationData humanBoneInfo, ref BoneTransform bt)
	{
		var r = MuscleRangeToRadians(humanBoneInfo.minMuscleAngles, humanBoneInfo.maxMuscleAngles, bt.rot.value.xyz);
		r *= humanBoneInfo.sign;

		var qx = quaternion.AxisAngle(math.right(), r.x);
		var qy = quaternion.AxisAngle(math.up(), r.y);
		var qz = quaternion.AxisAngle(math.forward(), r.z);
		var qzy = math.mul(qz, qy);
		qzy.value.x = 0;
		bt.rot = math.mul(math.normalize(qzy), qx);

		ApplyHumanoidPostTransform(humanBoneInfo, ref bt);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static float2 NormalizeAnimationTime(float at, ref AnimationClipBlob ac)
	{
		at += ac.cycleOffset;
		var normalizedTime = ac.looped ? math.frac(at) : math.saturate(at);
		var rv = normalizedTime * ac.length;
		return new (rv, normalizedTime);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CalculateLoopPose(ref BoneClipBlob boneAnimation, AnimationToProcessComponent atp, ref BoneTransform bonePose, in HumanRotationData hrd, float normalizedTime)
	{
		var animLen = atp.animation.Value.length;
		var lerpFactor = normalizedTime;
		var (rootPoseStart, _) = ProcessAnimationCurves(ref boneAnimation, hrd, 0);
		var (rootPoseEnd, _) = ProcessAnimationCurves(ref boneAnimation, hrd, animLen);

		var dPos = rootPoseEnd.pos - rootPoseStart.pos;
		var dRot = math.mul(math.conjugate(rootPoseEnd.rot), rootPoseStart.rot);
		bonePose.pos -= dPos * lerpFactor;
		bonePose.rot = math.mul(bonePose.rot, math.slerp(quaternion.identity, dRot, lerpFactor));
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void HandleRootMotionLoops(ref BoneTransform bonePose, ref BoneClipBlob boneAnimation, in AnimationToProcessComponent atp)
	{
		ref var animBlob = ref atp.animation.Value;
		if (!animBlob.looped)
			return;

		var numLoopCycles = (int)math.floor(atp.time + atp.animation.Value.cycleOffset);
		if (numLoopCycles < 1)
			return;

		var animLen = atp.animation.Value.length;
		var (endFramePose, _) = SampleAnimation(ref boneAnimation, animLen, atp, false, -1);
		var (startFramePose, _) = SampleAnimation(ref boneAnimation, 0, atp, false, -1);

		var deltaPose = BoneTransform.Multiply(endFramePose, BoneTransform.Inverse(startFramePose));

		BoneTransform accumCyclePose = BoneTransform.Identity();
		for (var c = numLoopCycles; c > 0; c >>= 1)
		{
			if ((c & 1) == 1)
				accumCyclePose = BoneTransform.Multiply(accumCyclePose, deltaPose);
			deltaPose = BoneTransform.Multiply(deltaPose, deltaPose);
		}
		bonePose = BoneTransform.Multiply(accumCyclePose, bonePose);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void MixPoses(ref BoneTransform curPose, BoneTransform inPose, float3 weight, AnimationBlendingMode blendMode)
	{
		if (blendMode == AnimationBlendingMode.Override)
		{
			inPose.rot = MathUtils.ShortestRotation(curPose.rot, inPose.rot);
			var scaledPose = BoneTransform.Scale(inPose, weight);

			curPose.pos += scaledPose.pos;
			curPose.rot.value += scaledPose.rot.value;
			curPose.scale += scaledPose.scale;
		}
		else
		{
			curPose.pos += inPose.pos * weight.x;
			quaternion layerRot = math.normalizesafe(new float4(inPose.rot.value.xyz * weight.y, inPose.rot.value.w));
			layerRot = MathUtils.ShortestRotation(curPose.rot, layerRot);
			curPose.rot = math.mul(layerRot, curPose.rot);
			curPose.scale *= (1 - weight.z) + (inPose.scale * weight.z);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static float CalculateFinalLayerWeights(in Span<float> layerWeights, in DynamicBuffer<AnimationToProcessComponent> atp, in Hash128 boneHash, AvatarMaskBodyPart humanAvatarMaskBodyPart)
	{
		var layerIndex = -1;
		var w = 1.0f;

		for (int i = atp.Length - 1; i >= 0; --i)
		{
			var a = atp[i];
			if (a.layerIndex == layerIndex) continue;

			var inAvatarMask = IsBoneInAvatarMask(boneHash, humanAvatarMaskBodyPart, a.avatarMask);
			var layerWeight = inAvatarMask ? a.layerWeight : 0;

			var lw = w * layerWeight;
			layerWeights[a.layerIndex] = lw;
			if (a.blendMode == AnimationBlendingMode.Override)
				w -= lw;
			layerIndex = a.layerIndex;
		}
		return atp[0].blendMode == AnimationBlendingMode.Override ? 0 : layerWeights[0];
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void ApplyHumanoidPostTransform(HumanRotationData hrd, ref BoneTransform bt)
	{
		bt.rot = math.mul(math.mul(hrd.preRot, bt.rot), hrd.postRot);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void BoneTransformMakePretty(ref BoneTransform bt, BoneTransform refPose, float3 weights)
	{
		var complWeights = math.saturate(new float3(1) - weights);
		bt.pos += refPose.pos * complWeights.x;
		var shortestRefRot = MathUtils.ShortestRotation(bt.rot.value, refPose.rot.value);
		bt.rot.value += shortestRefRot.value * complWeights.y;
		bt.scale += refPose.scale * complWeights.z;

		bt.rot = math.normalize(bt.rot);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static bool IsBoneInAvatarMask(in Hash128 boneHash, AvatarMaskBodyPart humanAvatarMaskBodyPart, BlobAssetReference<AvatarMaskBlob> am)
	{
		// If no avatar mask defined or bone hash is all zeroes assume that bone included
		if (!am.IsCreated || !math.any(boneHash.Value))
			return true;

		return (int)humanAvatarMaskBodyPart >= 0 ?
			IsBoneInHumanAvatarMask(humanAvatarMaskBodyPart, am) :
			IsBoneInGenericAvatarMask(boneHash, am);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static bool IsBoneInHumanAvatarMask(AvatarMaskBodyPart humanBoneAvatarMaskIndex, BlobAssetReference<AvatarMaskBlob> am)
	{
		var rv = (am.Value.humanBodyPartsAvatarMask & 1 << (int)humanBoneAvatarMaskIndex) != 0;
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static bool IsBoneInGenericAvatarMask(in Hash128 boneHash, BlobAssetReference<AvatarMaskBlob> am)
	{
		for (int i = 0; i < am.Value.includedBoneHashes.Length; ++i)
		{
			var avatarMaskBoneHash = am.Value.includedBoneHashes[i];
			if (avatarMaskBoneHash == boneHash)
				return true;
		}
		return false;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	(BoneTransform, float3) SampleAnimation
	(
		ref BoneClipBlob bcb,
		float2 animTime,
		in AnimationToProcessComponent atp,
		bool calculateLoopPose, 
		float additiveReferencePoseTime,
		in HumanRotationData hrd = default
	)
	{
		var time = animTime.x;
		var timeNrm = animTime.y;

		var (bonePose, flags) = ProcessAnimationCurves(ref bcb, hrd, time);
		
		//	Make additive animation if requested
		if (Hint.Unlikely(additiveReferencePoseTime >= 0))
		{
			var (zeroFramePose, _) = ProcessAnimationCurves(ref bcb, hrd, additiveReferencePoseTime);
			MakeAdditiveAnimation(ref bonePose, zeroFramePose);
		}
		
		if (Hint.Unlikely(calculateLoopPose))
		{
			CalculateLoopPose(ref bcb, atp, ref bonePose, hrd, timeNrm);
		}
		
		return (bonePose, flags);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void MakeAdditiveAnimation(ref BoneTransform rv, in BoneTransform zeroFramePose)
	{
		//	If additive layer make difference between reference pose and current animated pose
		rv.pos = rv.pos - zeroFramePose.pos;
		var conjugateZFRot = math.normalizesafe(math.conjugate(zeroFramePose.rot));
		conjugateZFRot = MathUtils.ShortestRotation(rv.rot, conjugateZFRot);
		rv.rot = math.mul(math.normalize(rv.rot), conjugateZFRot);
		rv.scale = rv.scale / zeroFramePose.scale;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	(BoneTransform, float3) ProcessAnimationCurves(ref BoneClipBlob bcb, HumanRotationData hrd, float time)
	{
		var rv = BoneTransform.Identity();

		bool eulerToQuaternion = false;

		float3 flags = 0;
		for (int i = 0; i < bcb.animationCurves.Length; ++i)
		{
			ref var ac = ref bcb.animationCurves[i];
			var interpolatedCurveValue = BlobCurve.SampleAnimationCurve(ref ac.keyFrames, time);

			switch (ac.bindingType)
			{
			case BindingType.Translation:
				rv.pos[ac.channelIndex] = interpolatedCurveValue;
				flags.x = 1;
				break;
			case BindingType.Quaternion:
				rv.rot.value[ac.channelIndex] = interpolatedCurveValue;
				flags.y = 1;
				break;
			case BindingType.EulerAngles:
				eulerToQuaternion = true;
				rv.rot.value[ac.channelIndex] = interpolatedCurveValue;
				flags.y = 1;
				break;
			case BindingType.HumanMuscle:
				rv.rot.value[ac.channelIndex] = interpolatedCurveValue;
				flags.y = 1;
				break;
			case BindingType.Scale:
				rv.scale[ac.channelIndex] = interpolatedCurveValue;
				flags.z = 1;
				break;
			default:
				BurstAssert.IsTrue(false, "Unknown binding type!");
				break;
			}
		}

		//	If we have got Euler angles instead of quaternion, convert them here
		if (eulerToQuaternion)
		{
			rv.rot = quaternion.Euler(math.radians(rv.rot.value.xyz), math.RotationOrder.XYZ);
		}

		if (bcb.isHumanMuscleClip)
		{
			MuscleValuesToQuaternion(hrd, ref rv);
		}

		return (rv, flags);
	}
}

//=================================================================================================================//

[BurstCompile]
partial struct ProcessGenericCurvesJob: IJobEntity
{
	[ReadOnly]
	public NativeParallelHashMap<Entity, RuntimeAnimationData.AnimatedEntityBoneDataProps> entityToDataOffsetMap;
	
	[NativeDisableParallelForRestriction]
	public NativeList<RuntimeAnimationData.GenericFloatAnimatedValue> genericAnimatedValues;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe void Execute(Entity e, in DynamicBuffer<AnimationToProcessComponent> animationsToProcess)
	{
		if (animationsToProcess.IsEmpty) return;

		Span<float> layerWeights = stackalloc float[animationsToProcess.Length];
		ComputeBoneAnimationJob.CalculateFinalLayerWeights(layerWeights, animationsToProcess, new Hash128(), (AvatarMaskBodyPart)(-1));

		for (int l = 0; l < animationsToProcess.Length; ++l)
		{
			var atp = animationsToProcess[l];
			if (atp.animation == BlobAssetReference<AnimationClipBlob>.Null)
				continue;
			
			var animTime = ComputeBoneAnimationJob.NormalizeAnimationTime(atp.time, ref atp.animation.Value);
			var layerWeight = layerWeights[atp.layerIndex];
			ref var curves = ref atp.animation.Value.curves;
			var weight = atp.weight * layerWeight;
			
			for (int k = 0; k < curves.Length && weight > 0; ++k)
			{
				ref var c = ref curves[k];
				
				for (var m = 0; m < c.animationCurves.Length; ++m)
				{
					ref var curve = ref c.animationCurves[m];
					var curveValue = SampleUserCurve(ref curve.keyFrames, atp, animTime.x);
					if (atp.animation.Value.loopPoseBlend)
						curveValue -= CalculateLoopPose(ref curve.keyFrames, atp, animTime.y);
					var curveHash128 = c.ComputeAnimationCurveHash(ref curve);
					StoreAnimatedValue(e, curveHash128, curveValue, weight);
				}
			}
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void StoreAnimatedValue(Entity e, in Hash128 curveHash128, float v, float weight)
	{
		var curveHash = Toolbox.HashUtils.Hash128To32(curveHash128);
		var entityOffsets = entityToDataOffsetMap[e];
		var startIndex = curveHash % entityOffsets.genericAnimationDataSize;
		var maxIterations = entityOffsets.genericAnimationDataSize;
		
		for (var i = 0; i < maxIterations; ++i)
		{
			var idx = (int)(startIndex + i) % maxIterations + entityOffsets.genericAnimationDataOffset;
			var gav = genericAnimatedValues[idx];
			if (!gav.hash.IsValid || gav.hash == curveHash128)
			{
				gav.hash = curveHash128;
				gav.value += v * weight;
				genericAnimatedValues[idx] = gav;
				return;
			}
		}
		
		BurstAssert.IsTrue(false, "Max iterations exceeded!");
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	float SampleUserCurve(ref BlobArray<KeyFrame> curve, in AnimationToProcessComponent atp, float animTime)
	{ 
		var curveValue = BlobCurve.SampleAnimationCurve(ref curve, animTime);
		//	Make additive animation if requested
		if (atp.blendMode == AnimationBlendingMode.Additive)
		{
			var additiveValue = BlobCurve.SampleAnimationCurve(ref curve, atp.animation.Value.additiveReferencePoseTime);
			curveValue -= additiveValue;
		}
		return curveValue;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	float CalculateLoopPose(ref BlobArray<KeyFrame> curve, in AnimationToProcessComponent atp, float normalizedTime)
	{
		var startV = SampleUserCurve(ref curve, atp, 0);
		var endV = SampleUserCurve(ref curve, atp, atp.animation.Value.length);

		var rv = (endV - startV) * normalizedTime;
		return rv;
	}
}

//=================================================================================================================//

[BurstCompile]
struct CalculateBoneOffsetsJob: IJobChunk
{
	[ReadOnly]
	public ComponentTypeHandle<RigDefinitionComponent> rigDefinitionTypeHandle;
	[ReadOnly]
	public ComponentLookup<CullAnimationsTag> cullAnimationsTagLookup;
	[ReadOnly]
	public BufferTypeHandle<AnimationToProcessComponent> animationToProcessBufferLookup;
	[ReadOnly]
	public NativeArray<int> chunkBaseEntityIndices;
	[ReadOnly]
	public NativeList<Entity> entities;
	
	[WriteOnly, NativeDisableContainerSafetyRestriction]
	public NativeList<int3> bonePosesOffsets;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
	{
		var rigDefAccessor = chunk.GetNativeArray(ref rigDefinitionTypeHandle);
		var animationToProcessBuffers = chunk.GetBufferAccessor(ref animationToProcessBufferLookup);
		int baseEntityIndex = chunkBaseEntityIndices[unfilteredChunkIndex];
		int validEntitiesInChunk = 0;

		var cee = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
		bonePosesOffsets[0] = 0;

		while (cee.NextEntityIndex(out var i))
		{
			var rigDef = rigDefAccessor[i];

			int entityInQueryIndex = baseEntityIndex + validEntitiesInChunk;
            ++validEntitiesInChunk;

            var e = entities[entityInQueryIndex];
            var boneCount = GetRigBoneCountWithRespectToCulling(e, rigDef, cullAnimationsTagLookup);

			var v = new int3
			(
				//	Bone count
				boneCount,
				//	Number of ulong values that can hold bone transform flags
				(boneCount * 4 >> 6) + 1,
				//	Number of generic curves present in all clips
				CalculateTotalGenericTracks(animationToProcessBuffers[i])
			);
			bonePosesOffsets[entityInQueryIndex + 1] = v;
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int CalculateTotalGenericTracks(in DynamicBuffer<AnimationToProcessComponent> atps)
	{
		var total = 0;
		//	Very conservatively (one slot for each clip track) count number of needed space to store generic curve data
		for (var i = 0; i < atps.Length; ++i)
		{
			var atp = atps[i];
			if (!atp.animation.IsCreated)
				continue;
			
			ref var userCurves = ref atp.animation.Value.curves;
			for (var k = 0; k < userCurves.Length; ++k)
			{
				total += userCurves[k].animationCurves.Length;
			}
		}
		return total;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int GetRigBoneCountWithRespectToCulling(Entity e, in RigDefinitionComponent rdc, in ComponentLookup<CullAnimationsTag> cullAnimationsTagLookup)
	{
		if (!cullAnimationsTagLookup.HasComponent(e) || !cullAnimationsTagLookup.IsComponentEnabled(e))
			return rdc.rigBlob.Value.bones.Length;
			
		return 1;
	}
}

//=================================================================================================================//

[BurstCompile]
struct CalculatePerBoneInfoJob: IJobChunk
{
	[ReadOnly]
	public NativeArray<int> chunkBaseEntityIndices;
	[ReadOnly]
	public NativeList<int3> bonePosesOffsets;
	[ReadOnly]
	public NativeList<Entity> entities;
	
	[WriteOnly, NativeDisableContainerSafetyRestriction]
	public NativeList<int3> boneToEntityIndices;
	[WriteOnly]
	public NativeParallelHashMap<Entity, RuntimeAnimationData.AnimatedEntityBoneDataProps>.ParallelWriter entityToDataOffsetMap;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
	{
		int baseEntityIndex = chunkBaseEntityIndices[unfilteredChunkIndex];
		int validEntitiesInChunk = 0;

		var cee = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

		while (cee.NextEntityIndex(out var i))
		{
			int entityInQueryIndex = baseEntityIndex + validEntitiesInChunk;
            ++validEntitiesInChunk;
			var offset = bonePosesOffsets[entityInQueryIndex];
			var nextOffset = bonePosesOffsets[entityInQueryIndex + 1]; // This is always valid because we have entities count + 1 array

			var e = entities[entityInQueryIndex];
			var boneCount = nextOffset.x - offset.x;
			
			var boneCountHighWORD = boneCount << 16;
			for (int k = 0; k < boneCount; ++k)
			{
				var boneIndexAndBoneCount = k | boneCountHighWORD;
				boneToEntityIndices[k + offset.x] = new int3(entityInQueryIndex, boneIndexAndBoneCount, offset.y);
			}

			var entityRigData = new RuntimeAnimationData.AnimatedEntityBoneDataProps()
			{
				bonePoseOffset = offset.x,
				boneFlagsOffset = offset.y,
				rigBoneCount = boneCount,
				genericAnimationDataOffset = offset.z,
				genericAnimationDataSize = nextOffset.z - offset.z
			};
			entityToDataOffsetMap.TryAdd(e, entityRigData);
		}
	}
}

//=================================================================================================================//

[BurstCompile]
struct DoPrefixSumJob: IJob
{
	public NativeList<int3> boneOffsets;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute()
	{
		var sum = new int3(0);
		for (int i = 0; i < boneOffsets.Length; ++i)
		{
			var v = boneOffsets[i];
			sum += v;
			boneOffsets[i] = sum;
		}
	}
}

//=================================================================================================================//

[BurstCompile]
struct ResizeDataBuffersJob: IJob
{
	[ReadOnly]
	public NativeList<int3> boneOffsets;
	public RuntimeAnimationData runtimeData;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute()
	{
		var boneBufferLen = boneOffsets[^1];
		runtimeData.animatedBonesBuffer.Resize(boneBufferLen.x, NativeArrayOptions.UninitializedMemory);
		runtimeData.worldSpaceBonesBuffer.Resize(boneBufferLen.x, NativeArrayOptions.UninitializedMemory);
		runtimeData.boneToEntityArr.Resize(boneBufferLen.x, NativeArrayOptions.UninitializedMemory);

		//	Clear flags by two resizes
		runtimeData.boneTransformFlagsHolderArr.Resize(0, NativeArrayOptions.UninitializedMemory);
		runtimeData.boneTransformFlagsHolderArr.Resize(boneBufferLen.y, NativeArrayOptions.ClearMemory);
		
		var entityCount = boneOffsets.Length;
		runtimeData.entityToDataOffsetMap.Clear();
		runtimeData.entityToDataOffsetMap.Capacity = math.max(entityCount, runtimeData.entityToDataOffsetMap.Capacity);
		
		//	Clear by two resizes
		runtimeData.genericCurveAnimatedValuesBuffer.Resize(0, NativeArrayOptions.UninitializedMemory);
		runtimeData.genericCurveAnimatedValuesBuffer.Resize(boneBufferLen.z, NativeArrayOptions.ClearMemory);
	}
}

//=================================================================================================================//

[BurstCompile]
partial struct CopyEntityBoneTransformsToAnimationBuffer: IJobEntity
{
	[NativeDisableContainerSafetyRestriction]
	public NativeList<BoneTransform> animatedBoneTransforms;
	[ReadOnly]
	public ComponentLookup<RigDefinitionComponent> rigDefComponentLookup;
	[ReadOnly]
	public ComponentLookup<Parent> parentComponentLookup;
	[NativeDisableContainerSafetyRestriction]
	public NativeList<ulong> boneTransformFlags;
	[ReadOnly]
	public NativeParallelHashMap<Entity, RuntimeAnimationData.AnimatedEntityBoneDataProps> entityToDataOffsetMap;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(Entity e, in AnimatorEntityRefComponent aer, in LocalTransform lt)
	{
		if (!rigDefComponentLookup.TryGetComponent(aer.animatorEntity, out var rdc))
			return;

		var entityBoneData = RuntimeAnimationData.CalculateBufferOffset(entityToDataOffsetMap, aer.animatorEntity);
		if (entityBoneData.bonePoseOffset < 0)
			return;
		
		//	If animation calculation was culled, we need operate only on valid bone range
		if (aer.boneIndexInAnimationRig >= entityBoneData.rigBoneCount)
			return;

		var bonePoses = RuntimeAnimationData.GetAnimationDataForRigRW(animatedBoneTransforms, entityBoneData.bonePoseOffset, entityBoneData.rigBoneCount);
		var transformFlags = AnimationTransformFlags.CreateFromBufferRW(boneTransformFlags, entityBoneData.boneFlagsOffset, entityBoneData.rigBoneCount);
		var boneFlags = new bool3
		(
			transformFlags.IsTranslationSet(aer.boneIndexInAnimationRig),
			transformFlags.IsRotationSet(aer.boneIndexInAnimationRig),
			transformFlags.IsScaleSet(aer.boneIndexInAnimationRig)
		);

		if (!math.any(boneFlags))
		{
			var entityPose = new BoneTransform(lt);
			//	Root motion delta should be zero
			if (rdc.applyRootMotion && aer.boneIndexInAnimationRig == 0)
				entityPose = BoneTransform.Identity();
			
			ref var bonePose = ref bonePoses[aer.boneIndexInAnimationRig];

			if (!boneFlags.x)
				bonePose.pos = entityPose.pos;
			if (!boneFlags.y)
				bonePose.rot = entityPose.rot;
			if (!boneFlags.z)
				bonePose.scale = entityPose.scale;
			
			//	For entities without parent we indicate that bone pose is in world space
			if (!parentComponentLookup.HasComponent(e))
				transformFlags.SetAbsoluteTransformFlag(aer.boneIndexInAnimationRig);
		}
	}
}

//=================================================================================================================//

[BurstCompile]
struct MakeAbsoluteTransformsJob: IJobChunk
{
	[ReadOnly]
	public ComponentTypeHandle<RigDefinitionComponent> rigDefTypeHandle;
	[ReadOnly]
	public EntityTypeHandle entityTypeHandle;
	[NativeDisableContainerSafetyRestriction]
	public NativeList<BoneTransform> localBoneTransforms;
	[NativeDisableContainerSafetyRestriction]
	public NativeList<BoneTransform> worldBoneTransforms;
	[ReadOnly]
	public NativeList<ulong> boneTransformFlags;
	[ReadOnly]
	public NativeParallelHashMap<Entity, RuntimeAnimationData.AnimatedEntityBoneDataProps> entityToDataOffsetMap;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
	{
		var rigDefAccessor = chunk.GetNativeArray(ref rigDefTypeHandle);
		var entityAccessor = chunk.GetNativeArray(entityTypeHandle);

		var cee = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
		var flagsHolder = new NativeBitArray(0xff, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

		while (cee.NextEntityIndex(out var i))
		{
			var rigDef = rigDefAccessor[i];
			var rigEntity = entityAccessor[i];
			
			if (!entityToDataOffsetMap.TryGetValue(rigEntity, out var boneDataOffset))
				return;

			ref var rigBones = ref rigDef.rigBlob.Value.bones;
			var rigBonesCount = boneDataOffset.rigBoneCount;
			flagsHolder.Resize(rigBonesCount);
			flagsHolder.Clear();

			var localBoneTransformsForRig = localBoneTransforms.GetSpan(boneDataOffset.bonePoseOffset, rigBonesCount);
			var worldBoneTransformsForRig = worldBoneTransforms.GetSpan(boneDataOffset.bonePoseOffset, rigBonesCount);
			var boneFlags = AnimationTransformFlags.CreateFromBufferRO(boneTransformFlags, boneDataOffset.boneFlagsOffset, rigBonesCount);

			// Iterate over all animated bones and calculate absolute transform in-place
			for (int animationBoneIndex = 0; animationBoneIndex < rigBonesCount; ++animationBoneIndex)
			{
				if (boneFlags.IsAbsoluteTransform(animationBoneIndex))
				{
					flagsHolder.Set(animationBoneIndex, true);
					worldBoneTransformsForRig[animationBoneIndex] = localBoneTransformsForRig[animationBoneIndex];
				}
				
				MakeAbsoluteTransform(flagsHolder, animationBoneIndex, localBoneTransformsForRig, worldBoneTransformsForRig, rigDef.rigBlob);
			}
			
			//	For all initially absolute bones calculate local transforms
			for (int animationBoneIndex = 0; animationBoneIndex < rigBonesCount; ++animationBoneIndex)
			{
				var parentBoneIndex = rigBones[animationBoneIndex].parentBoneIndex;
				if (!boneFlags.IsAbsoluteTransform(animationBoneIndex) || parentBoneIndex < 0)
					continue;
				
				var parentWorldTransform = worldBoneTransformsForRig[parentBoneIndex];
				var worldTransform = worldBoneTransformsForRig[animationBoneIndex];

				var localTransform = BoneTransform.Multiply(BoneTransform.Inverse(parentWorldTransform), worldTransform);
				localBoneTransformsForRig[animationBoneIndex] = localTransform;
			}
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void MakeAbsoluteTransform
	(
		NativeBitArray absTransformFlags,
		int boneIndex,
		Span<BoneTransform> localBoneTransformsForRig,
		Span<BoneTransform> worldBoneTransformsForRig,
		in BlobAssetReference<RigDefinitionBlob> rigBlob
	)
	{
		var resultBoneTransform = BoneTransform.Identity();
		var myBoneIndex = boneIndex;
		ref var rigBones = ref rigBlob.Value.bones;
		bool absTransformFlag;

		do
		{
			absTransformFlag = absTransformFlags.IsSet(boneIndex);
			var animatedBoneTransform = absTransformFlag ? worldBoneTransformsForRig[boneIndex] : localBoneTransformsForRig[boneIndex];
			resultBoneTransform = BoneTransform.Multiply(animatedBoneTransform, resultBoneTransform);
			
			boneIndex = rigBones[boneIndex].parentBoneIndex;
		}
		while (boneIndex >= 0 && !absTransformFlag);

		worldBoneTransformsForRig[myBoneIndex] = resultBoneTransform;
		absTransformFlags.Set(myBoneIndex, true);
	}
}

//=================================================================================================================//

[BurstCompile]
partial struct ComputeRootMotionJob: IJobEntity
{
	[NativeDisableContainerSafetyRestriction]
	public NativeList<BoneTransform> animatedBonePoses;
	[ReadOnly]
	public NativeParallelHashMap<Entity, RuntimeAnimationData.AnimatedEntityBoneDataProps> entityToDataOffsetMap;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(Entity e, in RigDefinitionComponent rdc, LocalTransform lt)
	{
		if (!rdc.applyRootMotion)
			return;
		
		var boneData = RuntimeAnimationData.GetAnimationDataForRigRW(animatedBonePoses, entityToDataOffsetMap, e);
		if (boneData.IsEmpty)
			return;
		
		var motionDeltaPose = boneData[0];
		var curEntityTransform = new BoneTransform(lt);
		var newEntityPose = BoneTransform.Multiply(curEntityTransform, motionDeltaPose);
		
		boneData[0] = newEntityPose;
	}
}

//=================================================================================================================//

[BurstCompile]
partial struct EmitAnimationEventsJob : IJobEntity
{
	public bool doDebugLogging;
	public float deltaTime;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Execute(Entity e, in DynamicBuffer<AnimationToProcessComponent> atp, in DynamicBuffer<PreviousProcessedAnimationComponent> ppa, ref DynamicBuffer<AnimationEventComponent> aec)
	{
		aec.Clear();

		var ppaArr = ppa.AsNativeArray();
		var atpArr = atp.AsNativeArray();
		ulong prevAnimationsProcessedIndices = 0;
		
		var maxBitsCount = UnsafeUtility.SizeOf<ulong>() * 8;
		BurstAssert.IsTrue(ppaArr.Length <= maxBitsCount, "Too many simultaneous animations! Change bits holder to the NativeBitArray if this error occurs.");
		
		for (var i = 0; i < atpArr.Length; ++i)
		{
			var a = atpArr[i];
			if (a.animation == BlobAssetReference<AnimationClipBlob>.Null)
				continue;
			
			var curTime = a.time;
			var prevBufferId = GetPreviousBufferAnimationIndex(a.motionId, i, ppaArr);
			var prevTime = 0.0f;
			if (prevBufferId < 0)
			{
				//	There is no such animation in "previous buffer". Assume that this animation advances by dt already
				prevTime = curTime - deltaTime;	
			}
			else
			{
				prevAnimationsProcessedIndices |= 1ul << prevBufferId;
				prevTime = ppaArr[prevBufferId].animationTime;
			}
			
			if (prevTime == curTime)
				continue;
			
			var negativeAnimationDT = prevTime > curTime;
			if (negativeAnimationDT)
				(prevTime, curTime) = (curTime, prevTime);
			
			ref var aes = ref a.animation.Value.events;
			if (a.animation.Value.looped)
			{
				ProcessEventsForLoopedAnimation(e, ref aec, ref aes, prevTime, curTime, negativeAnimationDT);
			}
			else
			{
                if (prevTime < 1 && curTime > 0)
					ProcessEventsForAnimation(e, ref aec, ref aes, prevTime, curTime, negativeAnimationDT);
			}
		}
		
		//	Now we need check for animations that missed now, but were there at previous frame
		//	Animation can ending playing for at least one frame (dt)
		for (var i = 0; i < ppaArr.Length; ++i)
		{
			var bitMask = 1ul << i;
			if ((prevAnimationsProcessedIndices & bitMask) != 0)
				continue;
			
			var p = ppaArr[i];
			ref var aes = ref p.animation.Value.events;
			ProcessEventsForAnimation(e, ref aec, ref aes, p.animationTime, p.animationTime + deltaTime, false);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void ProcessEventsForLoopedAnimation(Entity e, ref DynamicBuffer<AnimationEventComponent> aec, ref BlobArray<AnimationEventBlob> aes, float prevTime, float curTime, bool negativeAnimationDT)
	{
		var t0 = prevTime;
		var dt = curTime - prevTime;
		if (t0 < 0)
			t0 = t0 - math.floor(t0);
		
		var t1 = t0 + dt;
		var it0 = t0;
		
		//	Divide whole range to sections that fit in [0..1] range, and execute events calculation for them individually
		do
		{
			it0 = math.floor(it0);
			var tStart = math.max(it0, t0);
			var tEnd = math.min(it0 + 1, t1);
			tEnd -= tStart;
			tStart = math.frac(tStart);
			tEnd += tStart;
			ProcessEventsForAnimation(e, ref aec, ref aes, tStart, tEnd, negativeAnimationDT);
			it0 += 1;
		}
		while (it0 < t1);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void ProcessEventsForAnimation(Entity e, ref DynamicBuffer<AnimationEventComponent> outEvents, ref BlobArray<AnimationEventBlob> events, float fStart, float fEnd, bool reverseEventIteration)
	{
		for (var i = 0; i < events.Length; ++i)
		{
			//	Reverse events iteration order to preserve events order in output buffer
			var idx = reverseEventIteration ? events.Length - i - 1 : i;
			ref var ae = ref events[idx];
			var eventTime = ae.time;
			
			var emitEvent = eventTime >= fStart && eventTime <= fEnd;
		
			if (emitEvent)
			{	
				var evt = new AnimationEventComponent(ref ae);
				outEvents.Add(evt);
				
			#if RUKHANKA_DEBUG_INFO
				if (doDebugLogging)
					Debug.Log($"Emit event for entity {e}. Name: {ae.name.ToFixedString()}, Hash: {ae.nameHash}, F: {ae.floatParam}, I: {ae.intParam}, S: {ae.stringParam.ToFixedString()}");
			#endif
			}
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int GetPreviousBufferAnimationIndex(uint motionId, int animationBufferIndex, NativeArray<PreviousProcessedAnimationComponent> ppa)
	{
		//	Fast path
		if (animationBufferIndex < ppa.Length && ppa[animationBufferIndex].motionId == motionId)
			return animationBufferIndex;

		//	Full search
		for (var i = 0; i < ppa.Length; ++i)
		{
			if (motionId == ppa[i].motionId)
				return i;
		}

		return -1;
	}
}

//=================================================================================================================//

[BurstCompile]
partial struct MakeProcessedAnimationsSnapshotJob: IJobEntity
{
	void Execute(in DynamicBuffer<AnimationToProcessComponent> atp, ref DynamicBuffer<PreviousProcessedAnimationComponent> ppa)
	{
		ppa.Resize(atp.Length, NativeArrayOptions.UninitializedMemory);
		for (var i = 0; i < atp.Length; ++i)
		{
			var a = atp[i];
			var p = new PreviousProcessedAnimationComponent()
			{
				animationTime = a.time,
				motionId = a.motionId,
				animation = a.animation
			};
			ppa[i] = p;
		}
	}
}
}
}
