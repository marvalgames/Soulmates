#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using Rukhanka.Hybrid.RTP;
using Unity.Assertions;
using FixedStringName = Unity.Collections.FixedString512Bytes;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Entities;
using AnimationClip = UnityEngine.AnimationClip;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{ 
[BurstCompile]
public partial class AnimationClipBaker
{
	NativeList<BoneClip> genericCurvesArr, boneCurvesArr;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public AnimationClipBaker()
	{
		genericCurvesArr = new (Allocator.Temp);
		boneCurvesArr = new (Allocator.Temp);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	struct ParsedCurveBinding
	{
		public BindingType bindingType;
		public short channelIndex;
		public string boneName;
		public string channelName;

		public bool IsValid() => boneName.Length > 0;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	ValueTuple<string, string> SplitPath(string path)
	{
		var arr = path.Split('/');
		Assert.IsTrue(arr.Length > 0);
		var rv = (arr.Last(), arr.Length > 1 ? arr[arr.Length - 2] : "");
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	BindingType PickGenericBindingTypeByString(string bindingString) => bindingString switch
	{
		"m_LocalPosition" => BindingType.Translation,
		"m_LocalRotation" => BindingType.Quaternion,
		"localEulerAngles" => BindingType.EulerAngles,
		"localEulerAnglesRaw" => BindingType.EulerAngles,
		"m_LocalScale" => BindingType.Scale,
		"blendShape" => BindingType.BlendShape,
		_ => BindingType.Unknown
	};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	short ChannelIndexFromString(string c) => c switch
	{
		"x" => 0,
		"y" => 1,
		"z" => 2,
		"w" => 3,
		_ => -1
	};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	string ConstructBoneClipName(ValueTuple<string, string> nameAndPath)
	{
		string rv;
		//	Empty name string is unnamed root bone
		if (nameAndPath.Item1.Length == 0 && nameAndPath.Item2.Length == 0)
		{
			rv = SpecialBones.unnamedRootBoneName;
		}
		else
		{
			rv = nameAndPath.Item1;
		}
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	RTP.AnimationCurve PrepareAnimationCurve(Keyframe[] keysArr, ParsedCurveBinding pb)
	{
		var animCurve = new RTP.AnimationCurve();
		animCurve.channelIndex = pb.channelIndex;
		animCurve.bindingType = pb.bindingType;
		animCurve.keyFrames = new UnsafeList<KeyFrame>(keysArr.Length, Allocator.Temp);

		foreach (var k in keysArr)
		{
			var kf = new KeyFrame()
			{
				time = k.time,
				inTan = math.select(0, k.inTangent, math.isfinite(k.inTangent)),
				outTan = math.select(0, k.outTangent, math.isfinite(k.outTangent)),
				v = k.value
			};
			animCurve.keyFrames.Add(kf);
		}
		return animCurve;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	bool IsTransformCurve(BindingType bt)
	{
		switch (bt)
		{
			case BindingType.Translation:
			case BindingType.Quaternion:
			case BindingType.EulerAngles:
			case BindingType.HumanMuscle:
			case BindingType.Scale:
				return true;
		};
		return false;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int GetOrCreateBoneClipHolder(ref NativeList<RTP.BoneClip> clipsArr, in Hash128 nameHash, BindingType bt)
	{
		var rv = clipsArr.IndexOf(nameHash);
		if (rv < 0)
		{
			rv = clipsArr.Length;
			var bc = new RTP.BoneClip();
			bc.name = "MISSING_BONE_NAME";
			bc.nameHash = nameHash;
			bc.isHumanMuscleClip = bt == BindingType.HumanMuscle;
			bc.animationCurves = new UnsafeList<RTP.AnimationCurve>(32, Allocator.Temp);
			clipsArr.Add(bc);
		}
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int GetOrCreateBoneClipHolder(ref NativeList<RTP.BoneClip> clipsArr, ParsedCurveBinding pb, BindingType bt)
	{
		//	Hash for generic curves must match parameter name hash which is 32 bit instead of 128
		var nameHash = new Hash128(pb.boneName.CalculateHash32(), pb.channelName.CalculateHash32(), 0, 0);
		if (IsTransformCurve(bt))
		{
			nameHash = pb.boneName.CalculateHash128();
		}
		var rv = GetOrCreateBoneClipHolder(ref clipsArr, nameHash, bt);
		ref var c = ref clipsArr.ElementAt(rv);
		c.name = pb.boneName;
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	RTP.BoneClip MakeBoneClipCopy(in RTP.BoneClip bc)
	{
		var rv = bc;
		rv.animationCurves = new UnsafeList<RTP.AnimationCurve>(bc.animationCurves.Length, Allocator.Temp);
		for (int i = 0; i < bc.animationCurves.Length; ++i)
		{
			var inKf = bc.animationCurves[i].keyFrames;
			var outKf = new UnsafeList<KeyFrame>(inKf.Length, Allocator.Temp);
			for (int j = 0; j < inKf.Length; ++j)
			{
				outKf.Add(inKf[j]);
			}
			var ac = bc.animationCurves[i];
			ac.keyFrames = outKf;
			rv.animationCurves.Add(ac);
		}
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	ParsedCurveBinding ParseGenericCurveBinding(EditorCurveBinding b)
	{
		var rv = new ParsedCurveBinding();

		var t = b.propertyName.Split('.');
		var propName = t[0];
		var channel = t.Length > 1 ? t[1] : "";

		rv.channelIndex = ChannelIndexFromString(channel);
		rv.bindingType = PickGenericBindingTypeByString(propName);
		rv.channelName = b.propertyName;
		rv.boneName = b.path;

		if (IsTransformCurve(rv.bindingType) || rv.bindingType == BindingType.BlendShape)
		{
			var nameAndPath = SplitPath(b.path);
			rv.boneName = ConstructBoneClipName(nameAndPath);
		}

		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int GetHumanBoneIndexForHumanName(in HumanDescription hd, FixedStringName humanBoneName)
	{
		var humanBoneIndexInAvatar = Array.FindIndex(hd.human, x => x.humanName == humanBoneName);
		return humanBoneIndexInAvatar;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	ParsedCurveBinding ParseHumanoidCurveBinding(EditorCurveBinding b, Avatar avatar)
	{
		if (!humanoidMappingTable.TryGetValue(b.propertyName, out var rv))
			return ParseGenericCurveBinding(b);

		var hd = avatar.humanDescription;
		var humanBoneIndexInAvatar = GetHumanBoneIndexForHumanName(hd, rv.boneName);
		if (humanBoneIndexInAvatar < 0)
			return rv;

		if (rv.bindingType == BindingType.HumanMuscle)
		{
			var humanBoneDef = hd.human[humanBoneIndexInAvatar];
			rv.boneName = humanBoneDef.boneName;
		}

		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	ParsedCurveBinding ParseCurveBinding(AnimationClip ac, EditorCurveBinding b, Avatar avatar)
	{
		var rv = ac.isHumanMotion ?
			ParseHumanoidCurveBinding(b, avatar) :
			ParseGenericCurveBinding(b);

		return  rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void AddKeyFrameFromFloatValue(ref UnsafeList<KeyFrame> kfArr, float2 key, float v)
	{
		var kf = new KeyFrame()
		{
			time = key.x,
			inTan = key.y,
			outTan = key.y,
			v = v
		};
		kfArr.Add(kf);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	void ComputeTangents(ref RTP.AnimationCurve ac)
	{
		for (int i = 0; i < ac.keyFrames.Length; ++i)
		{
			var p0 = i == 0 ? ac.keyFrames[0] : ac.keyFrames[i - 1];
			var p1 = ac.keyFrames[i];
			var p2 = i == ac.keyFrames.Length - 1 ? ac.keyFrames[i] : ac.keyFrames[i + 1];

			var outV = math.normalizesafe(new float2(p2.time, p2.v) - new float2(p1.time, p1.v));
			var outTan = outV.x > 0.0001f ? outV.y / outV.x : 0;

			var inV = math.normalizesafe(new float2(p1.time, p1.v) - new float2(p0.time, p0.v));
			var inTan = inV.x > 0.0001f ? inV.y / inV.x : 0;

			var dt = math.abs(inTan) + math.abs(outTan);
			var f = dt > 0 ? math.abs(inTan) / dt : 0;

			var avgTan = math.lerp(inTan, outTan, f);

			var k = ac.keyFrames[i];
			k.outTan = avgTan;
			k.inTan = avgTan;
			ac.keyFrames[i] = k;
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	NativeList<float> CreateKeyframeTimes(float animationLength, float dt)
	{
		var numFrames = (int)math.ceil(animationLength / dt) + 1;
		var rv = new NativeList<float>(numFrames, Allocator.Temp);

		var curTime = 0.0f;
		for (var i = 0; i < numFrames; ++i)
		{
			rv.Add(curTime);
			curTime += dt;
		}
		
		if (rv.Length > 0)
			rv[^1] = math.min(animationLength, rv[^1]);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void ReadCurvesFromTransform(Transform tr, NativeArray<RTP.AnimationCurve> animCurves, float time)
	{
		quaternion q = tr.localRotation;
		float3 t = tr.localPosition;

		var vArr = new NativeArray<float>(7, Allocator.Temp);
		vArr[0] = t.x;
		vArr[1] = t.y;
		vArr[2] = t.z;
		vArr[3] = q.value.x;
		vArr[4] = q.value.y;
		vArr[5] = q.value.z;
		vArr[6] = q.value.w;

		for (int l = 0; l < vArr.Length; ++l)
		{
			var keysArr = animCurves[l];
			AddKeyFrameFromFloatValue(ref keysArr.keyFrames, time, vArr[l]);
			animCurves[l] = keysArr;
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void SetCurvesToAnimation(ref NativeList<BoneClip> outBoneClips, in Hash128 boneHash, NativeArray<RTP.AnimationCurve> animCurve)
	{
		var boneId = GetOrCreateBoneClipHolder(ref outBoneClips, boneHash, BindingType.Translation);
		ref var bc = ref outBoneClips.ElementAt(boneId);
		bc.DisposeCurves();

		for (var i = 0; i < animCurve.Length; ++i)
		{
			var hac = animCurve[i];
			ComputeTangents(ref hac);
			bc.animationCurves.Add(hac);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void SampleUnityAnimation(AnimationClip ac, Animator anm, ValueTuple<Transform, Hash128>[] trs, bool applyRootMotion, ref NativeList<RTP.BoneClip> boneClips)
	{
		if (trs.Length == 0)
			return;
		
		var sampleAnimationFrameTime = 1 / 60.0f;
		var keysList = CreateKeyframeTimes(ac.length, sampleAnimationFrameTime);

		var channelDesc = new ValueTuple<BindingType, short>[]
		{
			(BindingType.Translation, 0),
			(BindingType.Translation, 1),
			(BindingType.Translation, 2),
			(BindingType.Quaternion, 0),
			(BindingType.Quaternion, 1),
			(BindingType.Quaternion, 2),
			(BindingType.Quaternion, 3),
		};
 
		var rac = anm.runtimeAnimatorController;
		var origPos = anm.transform.position;
		var origRot = anm.transform.rotation;
		var origRootMotion = anm.applyRootMotion;
		var prevAnmCulling = anm.cullingMode;
		
		anm.runtimeAnimatorController = null;
		anm.cullingMode = AnimatorCullingMode.AlwaysAnimate;
		anm.applyRootMotion = true;
		anm.transform.position = Vector3.zero;
		anm.transform.rotation = quaternion.identity;
		
		var animationCurves = new NativeArray<RTP.AnimationCurve>(channelDesc.Length * trs.Length, Allocator.Temp);
		for (int k = 0; k < animationCurves.Length; ++k)
		{
			animationCurves[k] = new RTP.AnimationCurve()
			{
				bindingType = channelDesc[k % channelDesc.Length].Item1,
				channelIndex = channelDesc[k % channelDesc.Length].Item2,
				keyFrames = new UnsafeList<KeyFrame>(keysList.Length, Allocator.Temp)
			};
		}

		for (int i = 0; i < keysList.Length; ++i)
		{
			var time = keysList[i];
			ac.SampleAnimation(anm.gameObject, time);

			for (int l = 0; l < trs.Length; ++l)
			{
				var tr = trs[l].Item1;
				var curvesSpan = animationCurves.GetSubArray(l * channelDesc.Length, channelDesc.Length);
				ReadCurvesFromTransform(tr, curvesSpan, time);
			}
		}

		for (int l = 0; l < trs.Length; ++l)
		{
			var curvesSpan = animationCurves.GetSubArray(l * channelDesc.Length, channelDesc.Length);
			SetCurvesToAnimation(ref boneClips, trs[l].Item2, curvesSpan);
		}

		anm.cullingMode = prevAnmCulling;
		anm.runtimeAnimatorController = rac;
		anm.transform.position = origPos;
		anm.transform.rotation = origRot;
		anm.applyRootMotion = origRootMotion;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	(Transform, Hash128) GetRootBoneTransform(Animator anm)
	{
		if (anm.avatar.isHuman)
		{
			var hipsTransform = anm.GetBoneTransform(HumanBodyBones.Hips);
			var hd = anm.avatar.humanDescription;
			var humanBoneIndexInDesc = GetHumanBoneIndexForHumanName(hd, "Hips");
			var rigHipsBoneName = new FixedStringName(hd.human[humanBoneIndexInDesc].boneName).CalculateHash128();
			return (hipsTransform, rigHipsBoneName);
		}

		var rootBoneName =  anm.avatar.GetRootMotionNodeName();
		var rootBoneNameHash = new FixedStringName(rootBoneName).CalculateHash128();
		var rootBoneTransform = TransformUtils.FindChildRecursively(anm.transform, rootBoneName);
		return (rootBoneTransform, rootBoneNameHash);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void SampleMissingCurves(AnimationClip ac, Animator anm, ref NativeList<RTP.BoneClip> boneClips)
	{
		var trs = new List<ValueTuple<Transform, Hash128>>();
		var entityRootTransform = anm.transform;
		var rootBoneTransformData = GetRootBoneTransform(anm);

		if (anm.isHuman)
			trs.Add(rootBoneTransformData);

		//	Sample curves for non-rootmotion animations
		SampleUnityAnimation(ac, anm, trs.ToArray(), false, ref boneClips);
		
		//	Sample root motion curves
		trs.Clear();
		
		var entityRootHash = SpecialBones.unnamedRootBoneName.CalculateHash128();
		AnimationProcessSystem.ComputeBoneAnimationJob.ModifyBoneHashForRootMotion(ref entityRootHash);
		trs.Add((entityRootTransform, entityRootHash));
		
		//	Modify bone hash to separate root motion tracks and ordinary tracks
		AnimationProcessSystem.ComputeBoneAnimationJob.ModifyBoneHashForRootMotion(ref rootBoneTransformData.Item2);
		trs.Add(rootBoneTransformData);
		
		SampleUnityAnimation(ac, anm, trs.ToArray(), true, ref boneClips);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void BakeAnimationEvents(BlobBuilder bb, ref AnimationClipBlob acb, AnimationClip ac)
	{
		if (ac.events.Length == 0)
			return;

		var eventsArr = bb.Allocate(ref acb.events, ac.events.Length);
		for (var i = 0; i < eventsArr.Length; ++i)
		{
			var ae = ac.events[i];
			ref var bakedEvent = ref eventsArr[i];
		#if RUKHANKA_DEBUG_INFO
			if (ae.functionName.Length > 0)
				bb.AllocateString(ref bakedEvent.name, ae.functionName);
			if (ae.stringParameter.Length > 0)
				bb.AllocateString(ref bakedEvent.stringParam, ae.stringParameter);
		#endif
			bakedEvent.nameHash = new FixedStringName(ae.functionName).CalculateHash32();
			bakedEvent.time = ae.time / ac.length;
			bakedEvent.floatParam = ae.floatParameter;
			bakedEvent.intParam = ae.intParameter;
			bakedEvent.stringParamHash = new FixedStringName(ae.stringParameter).CalculateHash32();
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyClipsToBlobAsset(BlobBuilder bb, ref BlobArray<BoneClipBlob> acba, NativeList<RTP.BoneClip> clips)
	{
		var clipsArr = bb.Allocate(ref acba, clips.Length);
		for (var i = 0; i < clips.Length; ++i)
		{
			ref var bcb = ref clipsArr[i];
			CopyClipToBlobAsset(bb, ref bcb, clips[i]);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyClipToBlobAsset(BlobBuilder bb, ref BoneClipBlob bcb, RTP.BoneClip clip)
	{
	#if RUKHANKA_DEBUG_INFO
		if (clip.name.Length > 0)
			bb.AllocateString(ref bcb.name, clip.name.ToString());
	#endif
		bcb.hash = clip.nameHash;
		bcb.isHumanMuscleClip = clip.isHumanMuscleClip;
		
		var curvesArr = bb.Allocate(ref bcb.animationCurves, clip.animationCurves.Length);
		for (var i = 0; i < curvesArr.Length; ++i)
		{
			ref var cb = ref curvesArr[i];
			CopyKeyframesToBlobAsset(bb, ref cb, clip.animationCurves[i]);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CopyKeyframesToBlobAsset(BlobBuilder bb, ref AnimationCurve acb, RTP.AnimationCurve curve)
	{
		acb.bindingType = curve.bindingType;
		acb.channelIndex = curve.channelIndex;
		
		var keyframesArr = bb.Allocate(ref acb.keyFrames, curve.keyFrames.Length);
		for (var i = 0; i < keyframesArr.Length; ++i)
		{
			ref var kfBlob = ref keyframesArr[i];
			var kf = curve.keyFrames[i];
			kfBlob.time = kf.time;
			kfBlob.v = kf.v;
			kfBlob.inTan = kf.inTan;
			kfBlob.outTan = kf.outTan;
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	AnimationClip[] Deduplicate(AnimationClip[] animationClips)
	{
		var dedupList = new List<AnimationClip>();
		var dupSet = new NativeHashSet<int>(animationClips.Length, Allocator.Temp);

		foreach (var a in animationClips)
		{
			if (a != null && !dupSet.Add(a.GetInstanceID()))
				continue;

			dedupList.Add(a);
		}
		return dedupList.ToArray();
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	int BuildAnimationBakeList(IBaker baker, AnimationClip[] animationClips, Avatar avatar, out NativeArray<BlobAssetReference<AnimationClipBlob>> alreadyBakedList)
	{
		alreadyBakedList = new (animationClips.Length, Allocator.Temp);
		var rv = 0;
		for (var i = 0; i < animationClips.Length; ++i)
		{
			var ac = animationClips[i];
			if (ac == null)
				continue;
			
			//	Check for blob asset store first
			var animationHash = BakingUtils.ComputeAnimationHash(ac, avatar);
			var isAnimationExists = baker.TryGetBlobAssetReference<AnimationClipBlob>(animationHash, out var acb);
			if (!isAnimationExists)
			{
				//	Try cached baked animation
				acb = BlobCache.LoadBakedAnimationFromCache(ac, avatar);
				if (acb == BlobAssetReference<AnimationClipBlob>.Null)
				{
					rv += 1;
				}
				else
				{
					//	Don't forget to add loaded animation to blob asset store
					baker.AddBlobAssetWithCustomHash(ref acb, animationHash);
				}
			}
			
			alreadyBakedList[i] = acb;
		}
		
		//	Return count of animations need to perform full bake
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public NativeArray<BlobAssetReference<AnimationClipBlob>> BakeAnimations(IBaker baker, AnimationClip[] animationClips, Avatar avatar, GameObject animatedObjectRoot)
	{
		if (animationClips == null || animationClips.Length == 0)
			return default;
		
		animationClips = Deduplicate(animationClips);
		
		//	Firstly create list of animations that need to be baked (not present in cache and in not in blob asset store)
		var numClipsToBake = BuildAnimationBakeList(baker, animationClips, avatar, out var alreadyBakedAnimations);
		
		//	If nothing to bake, just return already baked list
		if (numClipsToBake == 0)
			return alreadyBakedAnimations;
		
		//	Now bake animations that require full rebake
		//	Need to make instance of object because when we will sample animations object placement can be modified.
		//	Also prefabs will not update its transforms
		GameObject objectCopy = null;
		Animator animatorCopy = null;
		if (avatar != null)
		{
			objectCopy = GameObject.Instantiate(animatedObjectRoot);
			objectCopy.hideFlags = HideFlags.HideAndDontSave;
			animatorCopy = objectCopy.GetComponent<Animator>();
			if (animatorCopy == null)
				animatorCopy = objectCopy.AddComponent<Animator>();
			animatorCopy.avatar = avatar;
		}
		
		for (var i = 0; i < animationClips.Length; ++i)
		{
			var clipBlob = alreadyBakedAnimations[i];
			var a = animationClips[i];
			if (clipBlob != BlobAssetReference<AnimationClipBlob>.Null)
				continue;
			
			var animationHash = BakingUtils.ComputeAnimationHash(a, avatar);
			var isAnimationExists = baker.TryGetBlobAssetReference(animationHash, out clipBlob);
			if (!isAnimationExists)
			{
				clipBlob = CreateAnimationBlobAsset(a, animatorCopy, animationHash);
				baker.AddBlobAssetWithCustomHash(ref clipBlob, animationHash);
			}
			else
			{
				Debug.Log($"Animation '{a.name}' is duplicate!");
			}
			
			alreadyBakedAnimations[i] = clipBlob;
			baker.DependsOn(a);
		}
		
		if (objectCopy != null)
			GameObject.DestroyImmediate(objectCopy);
		
		return alreadyBakedAnimations;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public BlobAssetReference<AnimationClipBlob> CreateAnimationBlobAsset(AnimationClip ac, Animator animator, Hash128 animationHash)
	{
		var avatar = animator?.avatar;
		var acSettings = AnimationUtility.GetAnimationClipSettings(ac);
		
		var bb = new BlobBuilder(Allocator.Temp);
		ref var rv = ref bb.ConstructRoot<AnimationClipBlob>();
		
#if RUKHANKA_DEBUG_INFO
		var startTimeMarker = Time.realtimeSinceStartupAsDouble;
		if (ac.name.Length > 0)
			bb.AllocateString(ref rv.name, ac.name);
#endif
		
		rv.length = ac.length;
		rv.looped = ac.isLooping;
		rv.hash = animationHash;
		rv.loopPoseBlend = acSettings.loopBlend;
		rv.cycleOffset = acSettings.cycleOffset;
		rv.additiveReferencePoseTime = acSettings.additiveReferencePoseTime;
		rv.hasRootMotionCurves = ac.hasRootCurves || ac.hasMotionCurves;

		BakeAnimationEvents(bb, ref rv, ac);
		
		var bindings = AnimationUtility.GetCurveBindings(ac);

		genericCurvesArr.Clear();
		boneCurvesArr.Clear();
		foreach (var b in bindings)
		{
			var ec = AnimationUtility.GetEditorCurve(ac, b);
			var pb = ParseCurveBinding(ac, b, animator?.avatar);
			
			var animCurve = PrepareAnimationCurve(ec.keys, pb);
			var isTransformCurve = IsTransformCurve(pb.bindingType);
			var curveHolder = isTransformCurve ? boneCurvesArr : genericCurvesArr ;

			if (isTransformCurve && pb.channelIndex < 0) continue;

			var boneId = GetOrCreateBoneClipHolder(ref curveHolder, pb, pb.bindingType);
			var boneClip = curveHolder[boneId];
			boneClip.animationCurves.Add(animCurve);
			curveHolder[boneId] = boneClip;
		}
		
		if (avatar != null)
		{
			//	Sample root and hips curves and from unity animations. Maybe sometime I will figure out all RootT/RootQ and body pose generation formulas and this step could be replaced with generation.
			SampleMissingCurves(ac, animator, ref boneCurvesArr);
		}
		
		CreateBonesPerfectHashTable(bb, ref rv, boneCurvesArr);
		
		CopyClipsToBlobAsset(bb, ref rv.bones, boneCurvesArr);
		CopyClipsToBlobAsset(bb, ref rv.curves, genericCurvesArr);

	#if RUKHANKA_DEBUG_INFO
		var dt = Time.realtimeSinceStartupAsDouble - startTimeMarker;
		rv.bakingTime = (float)dt;
	#endif
		
		var bar = bb.CreateBlobAssetReference<AnimationClipBlob>(Allocator.Persistent);
		
		//	Save baked animation into cache
		BlobCache.SaveBakedAnimationToCache(ac, avatar, bar);
		
		return bar;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void CreateBonesPerfectHashTable(BlobBuilder bb, ref AnimationClipBlob acb, NativeList<BoneClip> boneClips)
	{
		var boneHashes = CollectClipHashes(boneClips);
		var boneReinterpretedHashes = boneHashes.Reinterpret<Hash128PerfectHashed>();
		
		var hashTableCreated = PerfectHash<Hash128PerfectHashed>.CreateMinimalPerfectHash(boneReinterpretedHashes, out var seedValues, out var shuffleIndices);
		if (hashTableCreated)
		{
			MathUtils.ShuffleArray(boneClips.AsArray().AsSpan(), shuffleIndices.AsArray());
			MathUtils.ShuffleArray(boneHashes.AsSpan(), shuffleIndices.AsArray());

			var bonePerfectHashSeeds = bb.Allocate(ref acb.bonesPerfectHashSeedTable, seedValues.Length);
			for (var i = 0; i < seedValues.Length; ++i)
				bonePerfectHashSeeds[i] = seedValues[i];
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	NativeArray<Hash128> CollectClipHashes(NativeList<BoneClip> boneClips)
	{
		var rv = new NativeArray<Hash128>(boneClips.Length, Allocator.Temp);
		for (var i = 0; i < boneClips.Length; ++i)
		{
			rv[i] = boneClips[i].nameHash;
		}

		return rv;
	}
}
}

#endif