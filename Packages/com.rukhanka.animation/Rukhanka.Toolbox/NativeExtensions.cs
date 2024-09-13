using System;
using System.Threading;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public static class NativeExtensions
{
	public static unsafe void SetBitThreadSafe(this UnsafeBitArray ba, int pos)
    {
#if UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS
		var idx = pos >> 6;
		var shift = pos & 0x3f;
		var value = 1ul << shift;
		Common.InterlockedOr(ref UnsafeUtility.AsRef<ulong>(ba.Ptr + idx), value);
#endif
    }

/////////////////////////////////////////////////////////////////////////////////

	public static unsafe void InterlockedMax(int* ptr, int cmpVal)
	{
		ref var r = ref UnsafeUtility.ArrayElementAsRef<int>(ptr, 0);
		int maxVal;
		int v;
		
		do
		{
			v = r;
			maxVal = math.max(v, cmpVal);
		} while (r != Interlocked.CompareExchange(ref r, maxVal, v));
	}

}
}
