using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

[assembly: InternalsVisibleTo("Rukhanka.Tests")]

/////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{ 
public struct AnimatorControllerSystemJobs
{

[BurstCompile]
public struct StateMachineProcessJob: IJobChunk
{
	public float dt;
	public int frameIndex;
	public BufferTypeHandle<AnimatorControllerLayerComponent> controllerLayersBufferHandle;
	public BufferTypeHandle<AnimatorControllerParameterComponent> controllerParametersBufferHandle;
	public EntityTypeHandle entityTypeHandle;
	[NativeDisableParallelForRestriction]
	public BufferLookup<AnimatorControllerEventComponent> controllerEventsBufferLookup;
	[ReadOnly]
	public NativeHashMap<Hash128, BlobAssetReference<AnimationClipBlob>> animationDatabase;
	[ReadOnly]
	public ComponentLookup<AnimatorOverrideAnimations> animatorOverrideAnimationLookup;

#if RUKHANKA_DEBUG_INFO
	public bool doAnimatorProcessLogging;
	public bool doAnimatorEventsLogging;
#endif
	
	BlobAssetReference<ControllerAnimationsBlob> controllerAnimationsBlob;

/////////////////////////////////////////////////////////////////////////////////////////////////////

	public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
	{
		var layerBuffers = chunk.GetBufferAccessor(ref controllerLayersBufferHandle);
		var parameterBuffers = chunk.GetBufferAccessor(ref controllerParametersBufferHandle);
		var entities = chunk.GetNativeArray(entityTypeHandle);

		var cee = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

		while (cee.NextEntityIndex(out var i))
		{
			var layers = layerBuffers[i];
			var parameters = parameterBuffers.Length > 0 ? parameterBuffers[i].AsNativeArray() : default;
			var e = entities[i];
			
			DynamicBuffer<AnimatorControllerEventComponent> controllerEventsBuffer = default;
			if (controllerEventsBufferLookup.HasBuffer(e) && controllerEventsBufferLookup.IsBufferEnabled(e))
				controllerEventsBuffer = controllerEventsBufferLookup[e];

			ExecuteSingle(layers, parameters, ref controllerEventsBuffer, e);
			
		#if RUKHANKA_DEBUG_INFO
			DoEventsDebugLogging(e, controllerEventsBuffer);
		#endif
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	unsafe void ExecuteSingle
	(
		in DynamicBuffer<AnimatorControllerLayerComponent> aclc,
		in NativeArray<AnimatorControllerParameterComponent> acpc,
		ref DynamicBuffer<AnimatorControllerEventComponent> events,
		Entity entity
	)
	{
		if (events.IsCreated)
			events.Clear();

		var numIntsForBitMemory = BitFieldN.CalculateUIntsCountForGivenBitCount(acpc.Length);
		var triggersToResetMem = stackalloc uint[numIntsForBitMemory];
		var triggersToReset = new BitFieldN(triggersToResetMem, numIntsForBitMemory);
			
		var startIndex = 0;
		for (int i = 0; i < aclc.Length; ++i)
		{
			ref var acc = ref aclc.ElementAt(i);
		#if RUKHANKA_DEBUG_INFO
			//	Make state snapshot to compare it later and log differences
			var controllerDataPreSnapshot = acc;
		#endif
			
			//	Save controller animations blob asset reference in class variable, because passing it inside almost all functions will bloat signatures significantly
			controllerAnimationsBlob = FillAnimationsFromControllerSystem.GetControllerAnimationsBlob(entity, animatorOverrideAnimationLookup, acc.animations);

			ProcessLayer(ref acc.controller.Value, acc.layerIndex, acpc, ref acc, ref events, triggersToReset);
			if (events.IsCreated)
			{
				EmitStateUpdateEvents(ref events, acc, startIndex);
				startIndex = events.Length;
			}
			
		#if RUKHANKA_DEBUG_INFO
			DoProcessDebugLogging(controllerDataPreSnapshot, acc, frameIndex);
		#endif
		}
		
		//	Reset affected triggers
		ResetTriggers(acpc, triggersToReset);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	void ResetTriggers(NativeArray<AnimatorControllerParameterComponent> acpc, BitFieldN triggersToReset)
	{
		if (!triggersToReset.TestAny())
			return;
		
		for (var i = 0; i < acpc.Length; ++i)
		{
			var p = acpc[i];
			if (p.type == ControllerParameterType.Trigger && triggersToReset.IsSet(i))
			{
				p.BoolValue = false;
				acpc[i] = p;
			}
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	RuntimeAnimatorData.StateRuntimeData InitRuntimeStateData(int stateID)
	{
		var rv = new RuntimeAnimatorData.StateRuntimeData();
		rv.id = stateID;
		rv.normalizedDuration = 0;
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	void ExitTransition(ref AnimatorControllerLayerComponent acc, ref DynamicBuffer<AnimatorControllerEventComponent> events)
	{
		if (acc.rtd.activeTransition.id < 0)
			return;
		
		if (CheckTransitionExitConditions(acc.rtd.activeTransition))
		{
			//	Add state exit event
			EmitEvent(ref events, AnimatorControllerEventComponent.EventType.StateExit, acc.rtd.srcState.id, acc.layerIndex, acc.rtd.srcState.normalizedDuration);
				
			acc.rtd.srcState = acc.rtd.dstState;
			acc.rtd.dstState = acc.rtd.activeTransition = RuntimeAnimatorData.StateRuntimeData.MakeDefault();
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	void EnterTransition
	(
		ref AnimatorControllerLayerComponent acc,
		ref LayerBlob layer,
		NativeArray<AnimatorControllerParameterComponent> runtimeParams,
		float srcStateDurationFrameDelta,
		float curStateDuration,
		ref DynamicBuffer<AnimatorControllerEventComponent> events,
		BitFieldN triggersToReset
	)
	{
		if (acc.rtd.activeTransition.id >= 0)
			return;

		ref var currentState = ref layer.states[acc.rtd.srcState.id];

		for (int i = 0; i < currentState.transitions.Length; ++i)
		{
			ref var t = ref currentState.transitions[i];
			var b = CheckTransitionEnterExitTimeCondition(ref t, acc.rtd.srcState, srcStateDurationFrameDelta) &&
					CheckTransitionEnterConditions(ref t, runtimeParams, triggersToReset);
			if (b)
			{
				var timeShouldBeInTransition = GetTimeInSecondsShouldBeInTransition(ref t, acc.rtd.srcState, curStateDuration, srcStateDurationFrameDelta);
				acc.rtd.activeTransition.id	= i;
				acc.rtd.activeTransition.normalizedDuration = timeShouldBeInTransition / CalculateTransitionDuration(ref t, curStateDuration);
				var dstStateDur = CalculateStateDuration(ref layer.states[t.targetStateId], runtimeParams);
				acc.rtd.dstState = InitRuntimeStateData(t.targetStateId);
				acc.rtd.dstState.normalizedDuration += timeShouldBeInTransition / dstStateDur + t.offset;
				
				//	Add state enter event
				EmitEvent(ref events, AnimatorControllerEventComponent.EventType.StateEnter, acc.rtd.dstState.id, acc.layerIndex, acc.rtd.dstState.normalizedDuration);
				
				break;
			}
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	void EmitEvent(ref DynamicBuffer<AnimatorControllerEventComponent> events, AnimatorControllerEventComponent.EventType eventType, int stateID, int layerId, float stateDuration)
	{
		if (!events.IsCreated)
			return;
		
		var evt = new AnimatorControllerEventComponent()
		{
			eventType = eventType,
			stateId = stateID,
			layerId = layerId,
			timeInState = stateDuration
		};

		events.Add(evt);
		
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	void EmitStateUpdateEvents(ref DynamicBuffer<AnimatorControllerEventComponent> events, in AnimatorControllerLayerComponent acc, int startIndex)
	{
		if (!events.IsCreated)
			return;
		
		var srcStateEnterExit = false;
		var dstStateEnterExit = false;
		for (var i = startIndex; i < events.Length; ++i)
		{
			var e = events[i];
			if (acc.rtd.srcState.id >= 0 && e.stateId == acc.rtd.srcState.id)
				srcStateEnterExit = true;
			if (acc.rtd.dstState.id >= 0 && e.stateId == acc.rtd.dstState.id)
				dstStateEnterExit = true;
		}
		
		if (acc.rtd.srcState.id >= 0 && !srcStateEnterExit)
			EmitEvent(ref events, AnimatorControllerEventComponent.EventType.StateUpdate, acc.rtd.srcState.id, acc.layerIndex, acc.rtd.srcState.normalizedDuration);
		if (acc.rtd.dstState.id >= 0 && !dstStateEnterExit)
			EmitEvent(ref events, AnimatorControllerEventComponent.EventType.StateUpdate, acc.rtd.dstState.id, acc.layerIndex, acc.rtd.dstState.normalizedDuration);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	void ProcessLayer
	(
		ref ControllerBlob c,
		int layerIndex,
		in NativeArray<AnimatorControllerParameterComponent> runtimeParams,
		ref AnimatorControllerLayerComponent acc,
		ref DynamicBuffer<AnimatorControllerEventComponent> events,
		BitFieldN triggersToReset
	)
	{
		ref var layer = ref c.layers[layerIndex];
		
		var currentStateID = acc.rtd.srcState.id;
		if (currentStateID < 0)
			currentStateID = layer.defaultStateIndex;

		ref var currentState = ref layer.states[currentStateID];
		var curStateDuration = CalculateStateDuration(ref currentState, runtimeParams);

		if (Hint.Unlikely(acc.rtd.srcState.id < 0))
		{
			acc.rtd.srcState = InitRuntimeStateData(layer.defaultStateIndex);
			EmitEvent(ref events, AnimatorControllerEventComponent.EventType.StateEnter, acc.rtd.srcState.id, acc.layerIndex, acc.rtd.srcState.normalizedDuration);
		}

		var srcStateDurationFrameDelta = dt / curStateDuration;
		acc.rtd.srcState.normalizedDuration += srcStateDurationFrameDelta;

		if (acc.rtd.dstState.id >= 0)
		{
			var dstStateDuration = CalculateStateDuration(ref layer.states[acc.rtd.dstState.id], runtimeParams);
			acc.rtd.dstState.normalizedDuration += dt / dstStateDuration;
		}

		if (acc.rtd.activeTransition.id >= 0)
		{
			ref var currentTransitionBlob = ref currentState.transitions[acc.rtd.activeTransition.id];
			var transitionDuration = CalculateTransitionDuration(ref currentTransitionBlob, curStateDuration);
			acc.rtd.activeTransition.normalizedDuration += dt / transitionDuration;
		}

		ExitTransition(ref acc, ref events);
		EnterTransition(ref acc, ref layer, runtimeParams, srcStateDurationFrameDelta, curStateDuration, ref events, triggersToReset);
		//	Check tranision exit conditions one more time in case of Enter->Exit sequence appeared in single frame
		ExitTransition(ref acc, ref events);

		ProcessTransitionInterruptions();
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	float CalculateMotionDuration
	(
		ref MotionBlob mb,
		in NativeArray<AnimatorControllerParameterComponent> runtimeParams,
		float weight
	)
	{
		if (weight == 0) return 0;

		switch (mb.type)
		{
		case MotionBlob.Type.None:
			return 1;
		case MotionBlob.Type.AnimationClip:
			var animationHash = controllerAnimationsBlob.Value.animations[mb.animationIndex];
			var animBlob = BlobDatabaseSingleton.GetBlobAsset(animationHash, animationDatabase);
			if (animBlob != BlobAssetReference<AnimationClipBlob>.Null)
				return animBlob.Value.length * weight;
			return 1;
		}
		
		var childMotions = ScriptedAnimator.GetChildMotionsList(ref mb, runtimeParams);
		var rv = CalculateBlendTreeMotionDuration(childMotions, ref mb.blendTree.motions, runtimeParams, weight);
		
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	float CalculateBlendTreeMotionDuration
	(
		NativeList<ScriptedAnimator.MotionIndexAndWeight> miwArr,
		ref BlobArray<ChildMotionBlob> motions,
		in NativeArray<AnimatorControllerParameterComponent> runtimeParams,
		float weight
	)
	{
		if (!miwArr.IsCreated || miwArr.IsEmpty)
			return 1;

		var weightSum = 0.0f;
		for (int i = 0; i < miwArr.Length; ++i)
			weightSum += miwArr[i].weight;

		//	If total weight less then 1, normalize weights
		if (Hint.Unlikely(weightSum < 1))
		{
			for (int i = 0; i < miwArr.Length; ++i)
			{
				var miw = miwArr[i];
				miw.weight = miw.weight / weightSum;
				miwArr[i] = miw;
			}
		}

		var rv = 0.0f;
		for (int i = 0; i < miwArr.Length; ++i)
		{
			var miw = miwArr[i];
			ref var m = ref motions[miw.motionIndex];
			rv += CalculateMotionDuration(ref m.motion, runtimeParams, weight * miw.weight) / m.timeScale;
		}

		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	float CalculateTransitionDuration(ref TransitionBlob tb, float curStateDuration)
	{
		var rv = tb.duration;
		if (!tb.hasFixedDuration)
		{
			rv *= curStateDuration;
		}
		return math.max(rv, 0.0001f);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	float CalculateStateDuration(ref StateBlob sb, in NativeArray<AnimatorControllerParameterComponent> runtimeParams)
	{
		var motionDuration = CalculateMotionDuration(ref sb.motion, runtimeParams, 1);
		var speedMultiplier = 1.0f;
		if (sb.speedMultiplierParameterIndex >= 0)
		{
			speedMultiplier = runtimeParams[sb.speedMultiplierParameterIndex].FloatValue;
		}
		return motionDuration / (sb.speed * speedMultiplier);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	internal static float GetLoopAwareTransitionExitTime(float exitTime, float normalizedDuration, float speedSign)
	{
		var rv = exitTime;
		if (exitTime <= 1.0f)
		{
			//	Unity animator logic and documentation mismatch. Documentation says that exit time loop condition should be when transition exitTime less then 1, but in practice it will loop when exitTime is less or equal(!) to 1.
			exitTime = math.min(exitTime, 0.9999f);
			var snd = normalizedDuration * speedSign;

			var f = math.frac(snd);
			rv += (int)snd;
			if (f > exitTime)
				rv += 1;
		}
		return rv * speedSign;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	float GetTimeInSecondsShouldBeInTransition(ref TransitionBlob tb, RuntimeAnimatorData.StateRuntimeData curStateRTD, float curStateDuration, float frameDT)
	{
		if (!tb.hasExitTime) return 0;

		//	This should be always less then curStateRTD.normalizedDuration
		var loopAwareExitTime = GetLoopAwareTransitionExitTime(tb.exitTime, curStateRTD.normalizedDuration - frameDT, math.sign(frameDT));
		var loopDelta = curStateRTD.normalizedDuration - loopAwareExitTime;
		var rv = loopDelta * curStateDuration;
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	bool CheckTransitionEnterExitTimeCondition
	(
		ref TransitionBlob tb,
		RuntimeAnimatorData.StateRuntimeData curStateRuntimeData,
		float srcStateDurationFrameDelta
	)
	{
		var normalizedStateDuration = curStateRuntimeData.normalizedDuration; 

		var noNormalConditions = tb.conditions.Length == 0;
		if (!tb.hasExitTime) return !noNormalConditions;

		var l0 = normalizedStateDuration - srcStateDurationFrameDelta;
		var l1 = normalizedStateDuration;
		var speedSign = math.select(-1, 1, l0 < l1);

		var loopAwareExitTime = GetLoopAwareTransitionExitTime(tb.exitTime, l0, speedSign);

		if (speedSign < 0)
			(l0, l1) = (l1, l0);

		var rv = loopAwareExitTime > l0 && loopAwareExitTime <= l1;
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	bool CheckIntCondition(in AnimatorControllerParameterComponent param, ref ConditionBlob c)
	{
		var rv = true;
		switch (c.conditionMode)
		{
		case AnimatorConditionMode.Equals:
			if (param.IntValue != c.threshold.intValue) rv = false;
			break;
		case AnimatorConditionMode.Greater:
			if (param.IntValue <= c.threshold.intValue) rv = false;
			break;
		case AnimatorConditionMode.Less:
			if (param.IntValue >= c.threshold.intValue) rv = false;
			break;
		case AnimatorConditionMode.NotEqual:
			if (param.IntValue == c.threshold.intValue) rv = false;
			break;
		default:
			Debug.LogError($"Unsupported condition type for int parameter value!");
			break;
		}
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	bool CheckFloatCondition(in AnimatorControllerParameterComponent param, ref ConditionBlob c)
	{
		var rv = true;
		switch (c.conditionMode)
		{
		case AnimatorConditionMode.Greater:
			if (param.FloatValue <= c.threshold.floatValue) rv = false;
			break;
		case AnimatorConditionMode.Less:
			if (param.FloatValue >= c.threshold.floatValue) rv = false;
			break;
		default:
			Debug.LogError($"Unsupported condition type for int parameter value!");
			break;
		}
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	bool CheckBoolCondition(in AnimatorControllerParameterComponent param, ref ConditionBlob c)
	{
		var rv = true;
		switch (c.conditionMode)
		{
		case AnimatorConditionMode.If:
			rv = param.BoolValue;
			break;
		case AnimatorConditionMode.IfNot:
			rv = !param.BoolValue;
			break;
		default:
			Debug.LogError($"Unsupported condition type for int parameter value!");
			break;
		}
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	void MarkTriggersToReset(ref TransitionBlob tb, BitFieldN triggersToReset)
	{
		for (int i = 0; i < tb.conditions.Length; ++i)
		{
			ref var c = ref tb.conditions[i];
			//	Mark all transition parameters as "need to be reset". We will check actual parameter type later, after all layers processing
			triggersToReset.Set(c.paramIdx, true);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	bool CheckTransitionEnterConditions(ref TransitionBlob tb, NativeArray<AnimatorControllerParameterComponent> runtimeParams, BitFieldN triggersToReset)
	{
		if (tb.conditions.Length == 0)
			return true;

		var rv = true;
		var hasTriggers = false;
		for (int i = 0; i < tb.conditions.Length && rv; ++i)
		{
			ref var c = ref tb.conditions[i];
			var param = runtimeParams[c.paramIdx];

			switch (param.type)
			{
			case ControllerParameterType.Float:
				rv = CheckFloatCondition(param, ref c);
				break;
			case ControllerParameterType.Int:
				rv = CheckIntCondition(param, ref c);
				break;
			case ControllerParameterType.Bool:
				rv = CheckBoolCondition(param, ref c);
				break;
			case ControllerParameterType.Trigger:
				rv = CheckBoolCondition(param, ref c);
				hasTriggers = true;
				break;
			}
		}

		if (hasTriggers && rv)
			MarkTriggersToReset(ref tb, triggersToReset);

		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	bool CheckTransitionExitConditions(RuntimeAnimatorData.StateRuntimeData transitionRuntimeData)
	{
		return transitionRuntimeData.normalizedDuration >= 1;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	void ProcessTransitionInterruptions()
	{
		// Not implemented yet
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////

	void DoEventsDebugLogging(Entity e, in DynamicBuffer<AnimatorControllerEventComponent> events)
	{
	#if RUKHANKA_DEBUG_INFO
		if (!doAnimatorEventsLogging || !events.IsCreated)
			return;

		foreach (var evt in events)
		{
			Debug.Log($"Emit animator controller event for {e}. Type: {evt.eventType}, StateId: {evt.stateId}, LayerId: {evt.layerId}, Time: {evt.timeInState}");
		}
	#endif
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////

	void DoProcessDebugLogging(AnimatorControllerLayerComponent prevData, AnimatorControllerLayerComponent curData, int frameIndex)
	{
	#if RUKHANKA_DEBUG_INFO
		if (!doAnimatorProcessLogging) return;

		ref var c = ref curData.controller.Value;
		ref var layer = ref c.layers[curData.layerIndex];
		ref var currentState = ref layer.states[curData.rtd.srcState.id];

		var layerName = layer.name.ToFixedString();
		var controllerName = c.name.ToFixedString();
		var curStateName = currentState.name.ToFixedString();

		Debug.Log($"[{frameIndex}:{controllerName}:{layerName}] In state: '{curStateName}' with normalized duration: {curData.rtd.srcState.normalizedDuration}");

		//	Exit transition event
		if (prevData.rtd.activeTransition.id >= 0 && curData.rtd.activeTransition.id != prevData.rtd.activeTransition.id)
		{
			ref var t = ref layer.states[prevData.rtd.srcState.id].transitions[prevData.rtd.activeTransition.id];
			Debug.Log($"[{frameIndex}:{controllerName}:{layerName}] Exiting transition: '{t.name.ToFixedString()}'");
		}

		//	Enter transition event
		if (curData.rtd.activeTransition.id >= 0)
		{
			ref var t = ref layer.states[curData.rtd.srcState.id].transitions[curData.rtd.activeTransition.id];
			if (curData.rtd.activeTransition.id != prevData.rtd.activeTransition.id)
			{
				Debug.Log($"[{frameIndex}:{controllerName}:{layerName}] Entering transition: '{t.name.ToFixedString()}' with time: {curData.rtd.activeTransition.normalizedDuration}");
			}
			else
			{
				Debug.Log($"[{frameIndex}:{controllerName}:{layerName}] In transition: '{t.name.ToFixedString()}' with time: {curData.rtd.activeTransition.normalizedDuration}");
			}
			ref var dstState = ref layer.states[curData.rtd.dstState.id];
			Debug.Log($"[{frameIndex}:{controllerName}:{layerName}] Target state: '{dstState.name.ToFixedString()}' with time: {curData.rtd.dstState.normalizedDuration}");
		}
	#endif
	}
}
}
}
