using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using Random = Unity.Mathematics.Random;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Tests
{ 
public class BitFieldNTest
{
    [Test]
    public unsafe void FunctionalityTest()
    {
        var rng = new Random((uint)(Time.time * 1000));
        var maxBitfieldSize = rng.NextInt(300, 1000);
        var bitFieldMem = stackalloc uint[maxBitfieldSize];
        
        var numTests = rng.NextUInt(100);
        for (var i = 0; i < numTests; ++i)
        {
            var sz = rng.NextInt(1, maxBitfieldSize);
            var bf = new BitFieldN(bitFieldMem, sz);
            
            var i0 = rng.NextInt(0, bf.Length);
            bf.Set(i0, true);
            Assert.IsTrue(bf.IsSet(i0));
            Assert.IsTrue(bf.TestAny());
            bf.Set(i0, false);
            Assert.IsFalse(bf.IsSet(i0));
            Assert.IsFalse(bf.TestAny());
        }
    }
}
}
