#if UNITY_EDITOR

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEditor.Animations;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
[BurstCompile]
public partial class AnimatorControllerBaker
{
	uint motionCounter;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static void AddTransitionBlob(RTP.Transition t, UnsafeList<RTP.State> allStates, UnsafeList<RTP.Parameter> allParams, ref BlobBuilder bb, ref TransitionBlob tb)
	{
	#if RUKHANKA_DEBUG_INFO
		bb.AllocateString(ref tb.name, ref t.name);
	#endif

		var bbc = bb.Allocate(ref tb.conditions, t.conditions.Length);
		for (int ci = 0; ci < t.conditions.Length; ++ci)
		{
			ref var cb = ref bbc[ci];
			var src = t.conditions[ci];
			cb.conditionMode = src.conditionMode;
			cb.paramIdx = allParams.IndexOf(src.paramName);
			cb.threshold = src.threshold;

		#if RUKHANKA_DEBUG_INFO
			bb.AllocateString(ref cb.name, ref src.name);
		#endif
		}

		tb.hash = t.name.CalculateHash32();
		tb.duration = t.duration;
		tb.exitTime = t.exitTime;
		tb.hasExitTime = t.hasExitTime;
		tb.offset = t.offset;
		tb.hasFixedDuration = t.hasFixedDuration;
		tb.targetStateId = allStates.IndexOf(t.targetStateHash);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static void AddChildMotionBlob(RTP.ChildMotion cm, ref BlobBuilder bb, ref ChildMotionBlob cmb, in UnsafeList<RTP.Parameter> allParams)
	{
		cmb.threshold = cm.threshold;
		cmb.timeScale = cm.timeScale;
		cmb.position2D = cm.position2D;
		cmb.directBlendParameterIndex = allParams.IndexOf(cm.directBlendParameterName);
		AddMotionBlob(cm.motion, ref bb, ref cmb.motion, allParams);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static void AddMotionBlob(RTP.Motion m, ref BlobBuilder bb, ref MotionBlob mb, in UnsafeList<RTP.Parameter> allParams)
	{
	#if RUKHANKA_DEBUG_INFO
		bb.AllocateString(ref mb.name, ref m.name);
	#endif

		mb.type = m.type;
		mb.hash = (uint)m.name.GetHashCode();
		mb.animationIndex = m.animationIndex;

		if (m.type != MotionBlob.Type.None && m.type != MotionBlob.Type.AnimationClip)
		{
			ref var bt = ref mb.blendTree;
			var bbm = bb.Allocate(ref bt.motions, m.blendTree.motions.Length);
			for (int i = 0; i < bbm.Length; ++i)
			{
				AddChildMotionBlob(m.blendTree.motions[i], ref bb, ref bbm[i], allParams);
			}
			bt.blendParameterIndex = allParams.IndexOf(m.blendTree.blendParameterName);
			bt.blendParameterYIndex = allParams.IndexOf(m.blendTree.blendParameterYName);
			bt.normalizeBlendValues = m.blendTree.normalizeBlendValues;

		#if RUKHANKA_DEBUG_INFO
			bb.AllocateString(ref bt.name, ref m.blendTree.name);
		#endif
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static void AddStateBlob
	(
		RTP.State s,
		ref BlobBuilder bb,
		ref StateBlob sb,
		UnsafeList<RTP.Transition> anyStateTransitions,
		UnsafeList<RTP.State> allStates,
		UnsafeList<RTP.Parameter> allParams
	)
	{
	#if RUKHANKA_DEBUG_INFO
		bb.AllocateString(ref sb.name, ref s.name);
	#endif

		sb.hash = s.name.CalculateHash32();
		sb.speed = s.speed;
		sb.speedMultiplierParameterIndex = allParams.IndexOf(s.speedMultiplierParameter);
		sb.timeParameterIndex = allParams.IndexOf(s.timeParameter);
		sb.cycleOffset = s.cycleOffset;
		sb.cycleOffsetParameterIndex = allParams.IndexOf(s.cycleOffsetParameter);
		
		if (!s.tag.IsEmpty)
		{
		#if RUKHANKA_DEBUG_INFO
			bb.AllocateString(ref sb.tag, ref s.tag);
		#endif
			sb.tagHash = s.tag.CalculateHash32();
		}

		var bbt = bb.Allocate(ref sb.transitions, s.transitions.Length + anyStateTransitions.Length);

		//	Any state transitions are first priority
		for (int ti = 0; ti < anyStateTransitions.Length; ++ti)
		{
			var ast = anyStateTransitions[ti];
			//	Do not add transitions to self according to flag
			if (ast.canTransitionToSelf || ast.targetStateHash != s.hashCode)
				AddTransitionBlob(ast, allStates, allParams, ref bb, ref bbt[ti]);
		}

		for (int ti = 0; ti < s.transitions.Length; ++ti)
		{
			var src = s.transitions[ti];
			AddTransitionBlob(src, allStates, allParams, ref bb, ref bbt[ti + anyStateTransitions.Length]);
		}

		//	Add motion
		AddMotionBlob(s.motion, ref bb, ref sb.motion, allParams);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static void AddAllLayers(BlobBuilder bb, ref ControllerBlob c, RTP.Controller data)
	{
		var bbl = bb.Allocate(ref c.layers, data.layers.Length);
		for (int li = 0; li < data.layers.Length; ++li)
		{
			var src = data.layers[li];
			ref var l = ref bbl[li];

		#if RUKHANKA_DEBUG_INFO
			bb.AllocateString(ref l.name, ref src.name);
		#endif

			l.defaultStateIndex = src.defaultStateIndex;
			l.initialWeight = src.weight;
			l.blendingMode = src.blendMode;

			// States
			var bbs = bb.Allocate(ref l.states, src.states.Length);
			for (int si = 0; si < src.states.Length; ++si)
			{
				var s = src.states[si];
				AddStateBlob(s, ref bb, ref bbs[si], src.anyStateTransitions, src.states, data.parameters);
			}
			
			l.avatarMaskBlobHash = src.avatarMaskBlobHash;
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static void AddParameters(BlobBuilder bb, ref ControllerBlob c, in RTP.Controller data)
	{
		//	Now place parameters in its original places as in authoring animator
		var bba = bb.Allocate(ref c.parameters, data.parameters.Length);
		for	(int pi = 0; pi < data.parameters.Length; ++pi)
		{
			var src = data.parameters[pi];
			ref var p = ref bba[pi];
			p.defaultValue = src.defaultValue;
#if RUKHANKA_DEBUG_INFO
			bb.AllocateString(ref p.name, ref src.name);
#endif
			p.hash = data.parameters[pi].name.CalculateHash32();
			p.type = src.type;
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static BlobAssetReference<ParameterPerfectHashTableBlob> CreateParametersPerfectHashTableBlobInternal(in NativeArray<uint> hashesArr)
	{
		var hashesReinterpretedArr = hashesArr.Reinterpret<UIntPerfectHashed>();
		if (!PerfectHash<UIntPerfectHashed>.CreateMinimalPerfectHash(hashesReinterpretedArr, out var seedValues, out var shuffleIndices))
			return default;

		using var bb = new BlobBuilder(Allocator.Temp);
		ref var ppb = ref bb.ConstructRoot<ParameterPerfectHashTableBlob>();
		var bbh = bb.Allocate(ref ppb.seedTable, hashesArr.Length);
		for (var hi = 0; hi < hashesArr.Length; ++hi)
		{
			ref var paramRef = ref bbh[hi];
			paramRef = seedValues[hi];
		}
	
		var bbia = bb.Allocate(ref ppb.indirectionTable, shuffleIndices.Length);
		for (var ii = 0; ii < shuffleIndices.Length; ++ii)
		{
			ref var indirectionIndex = ref bbia[ii];
			indirectionIndex = shuffleIndices[ii];
		}

		seedValues.Dispose();
		shuffleIndices.Dispose();

		var rv = bb.CreateBlobAssetReference<ParameterPerfectHashTableBlob>(Allocator.Persistent);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe BlobAssetReference<ParameterPerfectHashTableBlob> CreateParametersPerfectHashTableBlob(BlobAssetReference<ControllerBlob> cb)
	{
		ref var parameters = ref cb.Value.parameters;
		
		//	Create blob asset for perfect hash table, but only if number of parameters is big enough
		if (parameters.Length <= 0)
			return default;
			
		var hashesArr = new NativeArray<uint>(parameters.Length, Allocator.Temp);
		for (int l = 0; l < parameters.Length; ++l)
		{
			hashesArr[l] = parameters[l].hash;
		}
		
		var hasher = new xxHash3.StreamingState();
		hasher.Update(hashesArr.GetUnsafeReadOnlyPtr(), hashesArr.Length * sizeof(uint));
		var phtBlobHash = new Hash128(hasher.DigestHash128());
		
		var blobAlreadyExists = TryGetBlobAssetReference<ParameterPerfectHashTableBlob>(phtBlobHash, out var phtBlob);
		if (blobAlreadyExists)
			return phtBlob;
		
		var rv = CreateParametersPerfectHashTableBlobInternal(hashesArr);
		AddBlobAssetWithCustomHash(ref rv, phtBlobHash);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	static void BuildControllerBlobInternal(ref RTP.Controller controllerData, ref BlobBuilder bb, ref ControllerBlob c, in Hash128 hash)
	{
		c.hash = hash;
	#if RUKHANKA_DEBUG_INFO
		bb.AllocateString(ref c.name, ref controllerData.name);
	#endif
		AddParameters(bb, ref c, controllerData);
		AddAllLayers(bb, ref c, controllerData);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	BlobAssetReference<ControllerBlob> BuildControllerBlob(AnimatorController controller)
	{
		var controllerHash = BakingUtils.ComputeControllerHash(controller);
		
		//	Try blob asset store first
		var isControllerAlreadyBaked = TryGetBlobAssetReference<ControllerBlob>(controllerHash, out var controllerBlob);
		if (isControllerAlreadyBaked)
			return controllerBlob;
		
		//	Try file cache
		controllerBlob = BlobCache.LoadBakedControllerFromCache(controller);
		if (controllerBlob != BlobAssetReference<ControllerBlob>.Null)
		{
			//	Don't forget to add loaded controller to blob asset store
			AddBlobAssetWithCustomHash(ref controllerBlob, controllerHash);
			return controllerBlob;
		}
		
	#if RUKHANKA_DEBUG_INFO
		var startTimeMarker = Time.realtimeSinceStartupAsDouble;
	#endif
		
		//	Only after failed caches build controller from scratch
		var controllerDataCollector = new AnimatorControllerDataCollector(controller);
		var controllerData = controllerDataCollector.Collect();
		
		var bb = new BlobBuilder(Allocator.Temp);
		ref var c = ref bb.ConstructRoot<ControllerBlob>();
		BuildControllerBlobInternal(ref controllerData, ref bb, ref c, controllerHash);
		
	#if RUKHANKA_DEBUG_INFO
		var dt = Time.realtimeSinceStartupAsDouble - startTimeMarker;
		c.bakingTime = (float)dt;
	#endif
		
		var rv = bb.CreateBlobAssetReference<ControllerBlob>(Allocator.Persistent);
		AddBlobAssetWithCustomHash(ref rv, controllerData.hash);
		
		//	Save created controller to cache
		BlobCache.SaveBakedControllerToCache(controller, rv);
		
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	BlobAssetReference<ControllerAnimationsBlob> BuildControllerAnimationHashesBlob(AnimatorController ac, Avatar avatar)
	{
		var bb = new BlobBuilder(Allocator.Temp);
		ref var cab = ref bb.ConstructRoot<ControllerAnimationsBlob>();
		var animsBlobArr = bb.Allocate(ref cab.animations, ac.animationClips.Length);
		for (var i = 0; i < animsBlobArr.Length; ++i)
		{
			var anim = ac.animationClips[i];
			animsBlobArr[i] = BakingUtils.ComputeAnimationHash(anim, avatar);
		}
		var rv = bb.CreateBlobAssetReference<ControllerAnimationsBlob>(Allocator.Persistent);
		AddBlobAsset(ref rv, out _);
		
		return rv;
	}
}
}

#endif