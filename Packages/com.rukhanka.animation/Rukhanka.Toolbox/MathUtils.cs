
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Animations;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public static class MathUtils
{
	public static quaternion ShortestRotation(quaternion from, quaternion to)
	{
		uint4 sign = math.asuint(math.dot(from, to)) & 0x80000000;
		var rv = math.asfloat(sign ^ math.asuint(to.value));
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static quaternion FromToRotation(float3 from, float3 to)
	{
		var rv = quaternion.identity;
		
		var fromNormalized = math.normalizesafe(from);
		var toNormalized = math.normalizesafe(to);
		float t = math.dot(fromNormalized, toNormalized);

		if (t < 1 && t > -1)
		{
			var ac = math.acos(t);
			var crossP = math.cross(from, to);
			crossP = math.normalizesafe(crossP);
			rv = quaternion.AxisAngle(crossP, ac);
		}
		else if (t <= -1)
		{
			var crossP = math.cross(from, math.right());
			if (math.lengthsq(crossP) < math.EPSILON)
				crossP = math.cross(from, math.up());

			rv = quaternion.AxisAngle(crossP, math.PI * 0.5f);
		}

		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static quaternion FromToRotationForNormalizedVectors(float3 from, float3 to)
	{
		var w = math.cross(from, to);
		var q = new quaternion(w.x, w.y, w.z, math.dot(from, to) + 1);
		q = math.normalize(q);
		return q;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	//	Shuffle array according to given indices
	public static void ShuffleArray<T>(Span<T> arr, in NativeArray<int> shuffleIndices) where T: unmanaged
	{
		if (arr.Length < 2) return;
		if (arr.Length != shuffleIndices.Length) return;
	
		Span<T> scatterArr = stackalloc T[arr.Length];
		for (int i = 0; i < arr.Length; ++i)
		{
			var shuffleIndex = shuffleIndices[i];
			var v = arr[shuffleIndex];
			scatterArr[i] = v;
		}

		for (int i = 0; i < arr.Length; ++i)
		{
			arr[i] = scatterArr[i];
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	//	Result is not normalized
	public static float3 MakePerpendicularVector(float3 v)
	{
		var av = math.abs(v);
		
		if (av.z < math.min(av.x, av.y))
			return new float3(v.y, -v.x, 0);
			
		if (av.y < av.x)
			return new float3(-v.z, 0, v.x);
			
		return new float3(0, v.z, -v.y);
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static bool PlaneLineIntersection(float3 linePoint, float3 lineVector, float4 plane, ref float3 intersectionPoint)
	{
		var fv = math.dot(lineVector, plane.xyz);
		if (math.abs(fv) > math.EPSILON)
		{
			intersectionPoint = linePoint - lineVector * math.dot(plane, new float4(linePoint, 1)) * math.rcp(fv);
			return true;
		}
		return false;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static float4 BuildPlaneFromNormalAndPoint(float3 n, float3 p)
	{
		var d = -math.dot(n, p);
		var pln = new float4(n, d);
		return pln;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public static float4 BuildPlaneFromThreePoints(float3 p0, float3 p1, float3 p2)
	{
		var dp0 = p1 - p0;
		var dp1 = p2 - p0;
		
		var n = math.cross(dp0, dp1);
		n = math.normalizesafe(n);
		return BuildPlaneFromNormalAndPoint(n, p0);
	}
}
}
