
using System;
using System.Diagnostics;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
//  This struct intended to be used as bitfield over existing memory
public unsafe struct BitFieldN
{
    uint* ptr;
    int sizeInInts;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public BitFieldN(uint* ptr, int sizeInInts, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
    {
        this.ptr = ptr;
        this.sizeInInts = sizeInInts;
        
        if (options == NativeArrayOptions.ClearMemory)
            Clear();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static int CalculateUIntsCountForGivenBitCount(int bitCount)
    {
        var rv = (bitCount >> 5);
        var k = math.min(bitCount & 0x1f, 1);
        return rv + k;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool IsSet(int pos)
    {
        CheckArgs(pos, 1);
        var intIdx = GetIntIndexForBitPos(pos);
        var rv = Bitwise.ExtractBits(ptr[intIdx], pos, 1);
        return rv != 0u;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Set(int pos, bool value)
    {
        CheckArgs(pos, 1);
        var intIdx = GetIntIndexForBitPos(pos);
        ptr[intIdx] = Bitwise.SetBits(ptr[intIdx], pos, 1, value);
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Clear()
    {
        UnsafeUtility.MemClear(ptr, sizeInInts * 4);
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool TestAny()
    {
        var v = 0u;
        for (var i = 0; i < sizeInInts; ++i)
        {
            v |= ptr[i];
        }
        return v != 0;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    int GetIntIndexForBitPos(int pos) => pos >> 5;
    public bool IsCreated => ptr != null;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Length
    {
        get => sizeInInts << 3;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
    void CheckArgs(int pos, int numBits)
    {
        if (pos + numBits < 0 || pos + numBits > sizeInInts * 32)
        {
            throw new ArgumentException($"BitFieldN invalid arguments: pos {pos} and numBits {numBits} must be within array capacity {sizeInInts * 32}");
        }
    }
}
}
