using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public static partial class ScriptedAnimator
{
	public struct MotionIndexAndWeight: IComparable<MotionIndexAndWeight>
	{
		public int motionIndex;
		public float weight;

		public int CompareTo(MotionIndexAndWeight a)
		{
			if (weight < a.weight)
				return 1;
			if (weight > a.weight)
				return -1;

			return 0;
		}
	}

//-----------------------------------------------------------------------------------------------------------------//

    internal static NativeList<MotionIndexAndWeight> ComputeBlendTree1D(in ReadOnlySpan<float> blendTreeThresholds, float blendTreeParameter)
    {
		var i0 = 0;
		var i1 = 0;
		bool found = false;
		for (int i = 0; i < blendTreeThresholds.Length && !found; ++i)
		{
            var t = blendTreeThresholds[i]; 
			i0 = i1;
			i1 = i;
			if (t > blendTreeParameter)
				found = true;
		}
		if (!found)
		{
			i0 = i1 = blendTreeThresholds.Length - 1;
		}

		var motion0Threshold = blendTreeThresholds[i0];
		var motion1Threshold = blendTreeThresholds[i1];
		float f = i1 == i0 ? 0 : (blendTreeParameter - motion0Threshold) / (motion1Threshold - motion0Threshold);

		var rv = new NativeList<MotionIndexAndWeight>(2, Allocator.Temp);
		rv.Add(new MotionIndexAndWeight { motionIndex = i0, weight = 1 - f });
		rv.Add(new MotionIndexAndWeight { motionIndex = i1, weight = f });
		return rv;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static void HandleCentroidCase(ref NativeList<MotionIndexAndWeight> rv, float2 pt, in ReadOnlySpan<float2> blendTreePositions)
	{
		if (math.any(pt))
			return;

		int i = 0;
		for (; i < blendTreePositions.Length && math.any(blendTreePositions[i]); ++i) { }

		if (i < blendTreePositions.Length)
		{
			var miw = new MotionIndexAndWeight() { motionIndex = i, weight = 1 };
			rv.Add(miw);
		}
		else
		{
			var f = 1.0f / blendTreePositions.Length;
			for (int l = 0; l < blendTreePositions.Length; ++l)
			{
				var miw = new MotionIndexAndWeight() { motionIndex = l, weight = f };
				rv.Add(miw);
			}
		}
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////

	//	p0 = (0,0)
	static (float, float, float) CalculateBarycentric(float2 p1, float2 p2, float2 pt)
	{
		var np2 = new float2(0 - p2.y, p2.x - 0);
		var np1 = new float2(0 - p1.y, p1.x - 0);

		var l1 = math.dot(pt, np2) / math.dot(p1, np2);
		var l2 = math.dot(pt, np1) / math.dot(p2, np1);
		var l0 = 1 - l1 - l2;
		return (l0, l1, l2);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    internal static NativeList<MotionIndexAndWeight> ComputeBlendTree2DSimpleDirectional(in ReadOnlySpan<float2> blendTreePositions, float2 blendTreeParameter)
    {
		var rv = new NativeList<MotionIndexAndWeight>(Allocator.Temp);

		if (blendTreePositions.Length < 2)
		{
			if (blendTreePositions.Length == 1)
				rv.Add(new MotionIndexAndWeight() { weight = 1, motionIndex = 0 });
			return rv;
		}

		HandleCentroidCase(ref rv, blendTreeParameter, blendTreePositions);
		if (rv.Length > 0)
			return rv;

		var centerPtIndex = -1;
		//	Loop over all directions and search for sector that contains requested point
		var dotProductsAndWeights = new NativeList<MotionIndexAndWeight>(blendTreePositions.Length, Allocator.Temp);
		for (int i = 0; i < blendTreePositions.Length; ++i)
		{
			var motionDir = blendTreePositions[i];
			if (!math.any(motionDir))
			{
				centerPtIndex = i;
				continue;
			}
			var angle = math.atan2(motionDir.y, motionDir.x);
			var miw = new MotionIndexAndWeight() { motionIndex = i, weight = angle };
			dotProductsAndWeights.Add(miw);
		}

		var ptAngle = math.atan2(blendTreeParameter.y, blendTreeParameter.x);

		dotProductsAndWeights.Sort();

		// Pick two closest points
		MotionIndexAndWeight d0 = default, d1 = default;
		var l = 0;
		for (; l < dotProductsAndWeights.Length; ++l)
		{
			var d = dotProductsAndWeights[l];
			if (d.weight < ptAngle)
			{
				var ld0 = l == 0 ? dotProductsAndWeights.Length - 1 : l - 1;
				d1 = d;
				d0 = dotProductsAndWeights[ld0];
				break;
			}
		}

		//	Handle last sector
		if (l == dotProductsAndWeights.Length)
		{
			d0 = dotProductsAndWeights[dotProductsAndWeights.Length - 1];
			d1 = dotProductsAndWeights[0];
		}

		var p0 = blendTreePositions[d0.motionIndex];
		var p1 = blendTreePositions[d1.motionIndex];
		
		//	Barycentric coordinates for point pt in triangle <p0,p1,0>
		var (l0, l1, l2) = CalculateBarycentric(p0, p1, blendTreeParameter);

		var m0Weight = l1;
		var m1Weight = l2;
		if (l0 < 0)
		{
			var sum = m0Weight + m1Weight;
			m0Weight /= sum;
			m1Weight /= sum;
		}	

		l0 = math.saturate(l0);

		var evenlyDistributedMotionWeight = centerPtIndex < 0 ? 1.0f / blendTreePositions.Length * l0 : 0;

		var miw0 = new MotionIndexAndWeight() { motionIndex = d0.motionIndex, weight = m0Weight + evenlyDistributedMotionWeight };
		rv.Add(miw0);

		var miw1 = new MotionIndexAndWeight() { motionIndex = d1.motionIndex, weight = m1Weight + evenlyDistributedMotionWeight };
		rv.Add(miw1);

		//	Add other motions of blend tree
		if (evenlyDistributedMotionWeight > 0)
		{
			for (int i = 0; i < blendTreePositions.Length; ++i)
			{
				if (i != d0.motionIndex && i != d1.motionIndex)
				{
					var miw = new MotionIndexAndWeight() { motionIndex = i, weight = evenlyDistributedMotionWeight };
					rv.Add(miw);
				}
			}
		}

		//	Add centroid motion
		if (centerPtIndex >= 0)
		{
			var miw = new MotionIndexAndWeight() { motionIndex = centerPtIndex, weight = l0 };
			rv.Add(miw);
		}

		dotProductsAndWeights.Dispose();

		return rv;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static unsafe NativeList<MotionIndexAndWeight> ComputeBlendTree2DFreeformCartesian(in ReadOnlySpan<float2> blendTreePositions, float2 blendTreeParameter)
	{
		var p = blendTreeParameter;
		Span<float> hpArr = stackalloc float[blendTreePositions.Length];

		var hpSum = 0.0f;

		//	Calculate influence factors
		for (int i = 0; i < blendTreePositions.Length; ++i)
		{
			var pi = blendTreePositions[i];
			var pip = p - pi;

			var w = 1.0f;

			for (int j = 0; j < blendTreePositions.Length && w > 0; ++j)
			{
				if (i == j) continue;
				var pj = blendTreePositions[j];
				var pipj = pj - pi;
				var f = math.dot(pip, pipj) / math.lengthsq(pipj);
				var hj = math.max(1 - f, 0);
				w = math.min(hj, w);
			}
			hpSum += w;
			hpArr[i] = w;
		}

		var rv = new NativeList<MotionIndexAndWeight>(blendTreePositions.Length, Allocator.Temp);
		//	Calculate weight functions
		for (int i = 0; i < blendTreePositions.Length; ++i)
		{
			var w = hpArr[i] / hpSum;
			if (w > 0)
			{
				var miw = new MotionIndexAndWeight() { motionIndex = i, weight = w };
				rv.Add(miw);
			}
		}
		return rv;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////

	static float CalcAngle(float2 a, float2 b)
	{
		var cross = a.x * b.y - a.y * b.x;
		var dot = math.dot(a, b);
		var tanA = new float2(cross, dot);
		var rv = math.atan2(tanA.x, tanA.y);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	static float2 CalcAngleWeights(float2 i, float2 j, float2 s)
	{
		float2 rv = 0;
		if (!math. any(i))
		{
			rv.x = CalcAngle(j, s);
			rv.y = 0;
		}
		else if (!math.any(j))
		{
			rv.x = CalcAngle(i, s);
			rv.y = rv.x;
		}
		else
		{
			rv.x = CalcAngle(i, j);
			if (!math.any(s))
				rv.y = rv.x;
			else
				rv.y = CalcAngle(i, s);
		}
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	static unsafe NativeList<MotionIndexAndWeight> ComputeBlendTree2DFreeformDirectional(in ReadOnlySpan<float2> blendTreePositions, float2 blendTreeParameter)
	{
		var p = blendTreeParameter;
		var lp = math.length(p);

		Span<float> hpArr = stackalloc float[blendTreePositions.Length];

		var hpSum = 0.0f;

		//	Calculate influence factors
		for (int i = 0; i < blendTreePositions.Length; ++i)
		{
			var pi = blendTreePositions[i];
			var lpi = math.length(pi);

			var w = 1.0f;

			for (int j = 0; j < blendTreePositions.Length && w > 0; ++j)
			{
				if (i == j) continue;
				var pj = blendTreePositions[j];
				var lpj = math.length(pj);

				var pRcpMiddle = math.rcp((lpj + lpi) * 0.5f);
				var lpip = (lp - lpi) * pRcpMiddle;
				var lpipj = (lpj - lpi) * pRcpMiddle;
				var angleWeights = CalcAngleWeights(pi, pj, p);

				var pip = new float2(lpip, angleWeights.y);
				var pipj = new float2(lpipj, angleWeights.x);

				var f = math.dot(pip, pipj) / math.lengthsq(pipj);
				var hj = math.saturate(1 - f);
				w = math.min(hj, w);
			}
			hpSum += w;
			hpArr[i] = w;	
		}

		var rv = new NativeList<MotionIndexAndWeight>(blendTreePositions.Length, Allocator.Temp);
		//	Calculate weight functions
		for (int i = 0; i < blendTreePositions.Length; ++i)
		{
			var w = hpArr[i] / hpSum;
			if (w > 0)
			{
				var miw = new MotionIndexAndWeight() { motionIndex = i, weight = w };
				rv.Add(miw);
			}
		}
		return rv;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	internal static NativeList<MotionIndexAndWeight> GetChildMotionsList
	(
		ref MotionBlob mb,
		in NativeArray<AnimatorControllerParameterComponent> runtimeParams
	)
	{
		NativeList<MotionIndexAndWeight> blendTreeMotionsAndWeights = default;

		switch (mb.type)
		{
		//	If no child motions, simply return
		case MotionBlob.Type.None:
		case MotionBlob.Type.AnimationClip:
			return blendTreeMotionsAndWeights;
		case MotionBlob.Type.BlendTreeDirect:
			blendTreeMotionsAndWeights = GetBlendTreeDirectCurrentMotions(ref mb, runtimeParams);
			break;
		case MotionBlob.Type.BlendTree1D:
			blendTreeMotionsAndWeights = GetBlendTree1DCurrentMotions(ref mb, runtimeParams);
			break;
		case MotionBlob.Type.BlendTree2DSimpleDirectional:
		case MotionBlob.Type.BlendTree2DFreeformCartesian:
		case MotionBlob.Type.BlendTree2DFreeformDirectional:
			blendTreeMotionsAndWeights = GetBlendTree2DCurrentMotions(ref mb, runtimeParams, mb.type);
			break;
		}
		
		return blendTreeMotionsAndWeights;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////

	static NativeList<MotionIndexAndWeight> GetBlendTree1DCurrentMotions(ref MotionBlob mb, in NativeArray<AnimatorControllerParameterComponent> runtimeParams)
	{
		var blendTreeParameter = runtimeParams[mb.blendTree.blendParameterIndex];
		ref var motions = ref mb.blendTree.motions;
		
		Span<float> bttSpan = stackalloc float[motions.Length];
		for (var i = 0; i < motions.Length; ++i)
		{
			bttSpan[i] = motions[i].threshold;
		}
		var rv = ComputeBlendTree1D(bttSpan, blendTreeParameter.FloatValue);
		
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	static NativeList<MotionIndexAndWeight> GetBlendTreeDirectCurrentMotions(ref MotionBlob mb, in NativeArray<AnimatorControllerParameterComponent> runtimeParams)
	{
		ref var motions = ref mb.blendTree.motions;
		var rv = new NativeList<MotionIndexAndWeight>(motions.Length, Allocator.Temp);

		var weightSum = 0.0f;
		for (int i = 0; i < motions.Length; ++i)
		{
			ref var cm = ref motions[i];
			var w = cm.directBlendParameterIndex >= 0 ? runtimeParams[cm.directBlendParameterIndex].FloatValue : 0;
			if (w > 0)
			{
				var miw = new MotionIndexAndWeight() { motionIndex = i, weight = w };
				weightSum += miw.weight;
				rv.Add(miw);
			}
		}

		if (mb.blendTree.normalizeBlendValues && weightSum > 1)
		{
			for (int i = 0; i < rv.Length; ++i)
			{
				var miw = rv[i];
				miw.weight = miw.weight / weightSum;
				rv[i] = miw;
			}
		}

		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	static NativeList<MotionIndexAndWeight> GetBlendTree2DCurrentMotions(ref MotionBlob mb, in NativeArray<AnimatorControllerParameterComponent> runtimeParams, MotionBlob.Type btType)
	{
		var pX = runtimeParams[mb.blendTree.blendParameterIndex];
		var pY = runtimeParams[mb.blendTree.blendParameterYIndex];
		var pt = new float2(pX.FloatValue, pY.FloatValue);
		ref var motions = ref mb.blendTree.motions;
		
		Span<float2> bttSpan = stackalloc float2[motions.Length];
		for (var i = 0; i < motions.Length; ++i)
		{
			bttSpan[i] = motions[i].position2D;
		}
		
		BurstAssert.IsTrue(btType != MotionBlob.Type.None && btType != MotionBlob.Type.AnimationClip, "Not a 2D blend tree type!");
		var rv = btType switch
		{
			MotionBlob.Type.BlendTree2DSimpleDirectional   => ComputeBlendTree2DSimpleDirectional(bttSpan, pt),
			MotionBlob.Type.BlendTree2DFreeformCartesian   => ComputeBlendTree2DFreeformCartesian(bttSpan, pt),
			MotionBlob.Type.BlendTree2DFreeformDirectional => ComputeBlendTree2DFreeformDirectional(bttSpan, pt),
			_ => default
		};
		return rv;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////

	static void PlayMotion
	(
		ref DynamicBuffer<AnimationToProcessComponent> atps,
		ref MotionBlob motionBlob,
		in NativeArray<AnimatorControllerParameterComponent> acpc,
        in BlobAssetReference<ControllerAnimationsBlob> animationsBlob,
		in BlobDatabaseSingleton blobDatabase,
        float normalizedTime,
        float weight,
        BlobAssetReference<AvatarMaskBlob> avatarMask
	)
	{
        switch (motionBlob.type)
        {
        case MotionBlob.Type.None:
            return;
        case MotionBlob.Type.AnimationClip:
            var animBlobHash = animationsBlob.Value.animations[motionBlob.animationIndex];
            var animBlob = blobDatabase.GetAnimationClipBlob(animBlobHash);
            PlayAnimation(ref atps, animBlob, normalizedTime, weight, avatarMask);
            return;
        }
        
        var childMotions = GetChildMotionsList(ref motionBlob, acpc);
        for (var i = 0; i < childMotions.Length; ++i)
        {
	        var cm = childMotions[i];
			ref var m = ref motionBlob.blendTree.motions[cm.motionIndex];
	        PlayMotion(ref atps, ref m.motion, acpc, animationsBlob, blobDatabase, normalizedTime, weight * cm.weight, avatarMask);
        }
	}
    
}
}
