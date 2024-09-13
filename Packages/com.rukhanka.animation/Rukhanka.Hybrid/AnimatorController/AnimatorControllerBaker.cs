#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public partial class AnimatorControllerBaker: Baker<Animator>
{
	public override void Bake(Animator a)
	{
		//	Skip animators without rig definition
		var rd = a.GetComponent<RigDefinitionAuthoring>();
		if (rd == null)
			return;
		
		if (a.runtimeAnimatorController == null)
		{
			Debug.LogWarning($"There is no controller attached to '{a.name}' animator. Skipping this object");
			return;
		}
		
		var e = GetEntity(TransformUsageFlags.Dynamic);
		
		var rac = GetRuntimeAnimatorController(a);
		var ac = GetAnimatorControllerFromRuntime(rac);
		var controllerBlob = BuildControllerBlob(ac);
		var controllerAnimationHashesBlob = BuildControllerAnimationHashesBlob(ac, a.avatar);
		
		var animationsFromOverrideController = GetAnimationsFromOverrideController(a);
		var allClips = new AnimationClip[animationsFromOverrideController.Length + ac.animationClips.Length];
		animationsFromOverrideController.CopyTo(allClips, 0);
		ac.animationClips.CopyTo(allClips, animationsFromOverrideController.Length);
		BakeAllControllerAnimations(e, a.avatar, allClips, a.gameObject);
		BakeAllUsedAvatarMasks(e, ac);
		
		CreateControllerEntityComponents(rd, e, controllerBlob, controllerAnimationHashesBlob);
		CreateOverrideAnimationsBuffer(e, a, ac);
		
		DependsOn(ac);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
	RuntimeAnimatorController GetRuntimeAnimatorController(Animator a)
	{
		var rv = a.runtimeAnimatorController;
		//	Check for animator override controller
		var aoc = rv as AnimatorOverrideController;
		if (aoc != null)
		{
			rv = aoc.runtimeAnimatorController;
		}
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	AnimatorController GetAnimatorControllerFromRuntime(RuntimeAnimatorController rac)
	{
		if (rac == null)
			return null;
		
		var acPath = AssetDatabase.GetAssetPath(rac);
		var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(acPath);
		return controller;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	AnimationClip[] GetAnimationsFromOverrideController(Animator animator)
	{
		var aoc = GetOverrideController(animator);
		if (aoc == null)
			return Array.Empty<AnimationClip>();
		
		var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
		aoc.GetOverrides(overrides);
		
		var rv = new List<AnimationClip>();
		for (var i = 0; i < overrides.Count; ++i)
		{
			var dstAnm = overrides[i].Value;
			if (dstAnm != null)
				rv.Add(dstAnm);
		}
		return rv.ToArray();
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void BakeAllUsedAvatarMasks(Entity e, AnimatorController controller)
	{
		var bakedAvatarMasks = new NativeList<NewBlobAssetDatabaseRecord<AvatarMaskBlob>>(Allocator.Temp);
		for (int i = 0; i < controller.layers.Length; ++i)
		{
			var l = controller.layers[i];
			if (l.avatarMask != null)
			{
				var amb = new AvatarMaskBaker();
				var avatarMaskBlobAsset = amb.CreateAvatarMaskBlob(this, l.avatarMask);
				var newAvatarMaskBlob = new NewBlobAssetDatabaseRecord<AvatarMaskBlob>()
				{
					hash = avatarMaskBlobAsset.Value.hash,
					value = avatarMaskBlobAsset
				};
				bakedAvatarMasks.Add(newAvatarMaskBlob);
			}
		}
		
		if (bakedAvatarMasks.Length > 0)
		{
			var buf = AddBuffer<NewBlobAssetDatabaseRecord<AvatarMaskBlob>>(e);
			buf.AddRange(bakedAvatarMasks.AsArray());
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void BakeAllControllerAnimations(Entity e, Avatar avatar, AnimationClip[] animationClips, GameObject go)
	{
		var animationBaker = new AnimationClipBaker();
		var bakedClipBlobs = animationBaker.BakeAnimations(this, animationClips, avatar, go);
		var newAnimationClips = AddBuffer<NewBlobAssetDatabaseRecord<AnimationClipBlob>>(e);
		foreach (var bcb in bakedClipBlobs)
		{
			if (!bcb.IsCreated)
				continue;
			
			newAnimationClips.Add(new ()
			{
				value = bcb,
				hash = bcb.Value.hash
			});
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateControllerParametersComponents(Entity e, BlobAssetReference<ControllerBlob> cb)
	{
		ref var parameters = ref cb.Value.parameters;
		if (parameters.Length == 0)
			return;
		
		//	Add dynamic parameters
		var paramArray = AddBuffer<AnimatorControllerParameterComponent>(e);
		for (int p = 0; p < parameters.Length; ++p)
		{
			ref var pm = ref parameters[p];
			var acpc = new AnimatorControllerParameterComponent()
			{
				value = pm.defaultValue,
				hash = pm.hash,
				type = pm.type,
			};

		#if RUKHANKA_DEBUG_INFO
			pm.name.CopyTo(ref acpc.name);
		#endif

			paramArray.Add(acpc);
		}
		
		//	Add perfect hash table used to fast runtime parameter value lookup
		var parametersPerfectHashTableBlob = CreateParametersPerfectHashTableBlob(cb);
		if (parametersPerfectHashTableBlob.IsCreated)
		{
			var pht = new AnimatorControllerParameterIndexTableComponent()
			{
				seedTable = parametersPerfectHashTableBlob
			};
			AddComponent(e, pht);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	AnimatorOverrideController GetOverrideController(Animator animator)
	{
		var rac = animator.runtimeAnimatorController;
		var aoc = rac as AnimatorOverrideController;
		return aoc;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateOverrideAnimationsBuffer(Entity e, Animator animator, AnimatorController ac)
	{
		var aoc = GetOverrideController(animator);
		if (aoc == null)
			return;
		
		var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
		aoc.GetOverrides(overrides);
		
		var bb = new BlobBuilder(Allocator.Temp);
		ref var cab = ref bb.ConstructRoot<ControllerAnimationsBlob>();
		var animsArr = bb.Allocate(ref cab.animations, ac.animationClips.Length);
		for (var i = 0; i < overrides.Count; ++i)
		{
			var kv = overrides[i];
			if (kv.Value == null)
				continue;
			
			var replacedIndex = Array.IndexOf(ac.animationClips, kv.Key);
			if (replacedIndex >= 0)
				animsArr[replacedIndex] = BakingUtils.ComputeAnimationHash(kv.Value, animator.avatar);
		}
		
		var blobAsset = bb.CreateBlobAssetReference<ControllerAnimationsBlob>(Allocator.Persistent);
		AddBlobAsset(ref blobAsset, out _);
		
		AddComponent(e, new AnimatorOverrideAnimations() { value = blobAsset });
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateControllerEntityComponents
	(
		RigDefinitionAuthoring rda,
		Entity e,
		BlobAssetReference<ControllerBlob> controllerBlob,
		BlobAssetReference<ControllerAnimationsBlob> controllerAnimationsBlob
	)
	{
		var acc = new AnimatorControllerLayerComponent();
		acc.rtd = RuntimeAnimatorData.MakeDefault();
		acc.controller = controllerBlob;
		acc.animations = controllerAnimationsBlob;

		var buf = AddBuffer<AnimatorControllerLayerComponent>(e);
		ref var cb = ref controllerBlob.Value;
		for (int k = 0; k < cb.layers.Length; ++k)
		{
			acc.layerIndex = k;
			acc.weight = cb.layers[k].initialWeight;
			buf.Add(acc);
		}
		
		if (rda.hasAnimatorControllerEvents)
			AddBuffer<AnimatorControllerEventComponent>(e);
		
		CreateControllerParametersComponents(e, controllerBlob);
	}
}
}

#endif