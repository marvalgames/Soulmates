using Unity.Collections;
using Hash128 = Unity.Entities.Hash128;
using FixedStringName = Unity.Collections.FixedString512Bytes;
using Unity.Core;

////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public static class FixedStringExtensions
{
	public unsafe static Hash128 CalculateHash128(in this FixedStringName s)
	{
		if (s.IsEmpty)
			return default;

		var hasher = new xxHash3.StreamingState();
		hasher.Update(s.GetUnsafePtr(), s.Length);
		var rv = new Hash128(hasher.DigestHash128());
		return rv;
	}

////////////////////////////////////////////////////////////////////////////////////

	public static Hash128 CalculateHash128(this string s)
	{
		if (s == null)
			return default;
		var fs = new FixedStringName(s);
		return fs.CalculateHash128();
	}

////////////////////////////////////////////////////////////////////////////////////

	public unsafe static uint CalculateHash32(in this FixedStringName s)
	{
		if (s.IsEmpty)
			return default;

		var rv = XXHash.Hash32(s.GetUnsafePtr(), s.Length);
		return rv;
	}
	
////////////////////////////////////////////////////////////////////////////////////

	public static uint CalculateHash32(this string s)
	{
		if (s == null)
			return 0;
		var fs = new FixedStringName(s);
		return fs.CalculateHash32();
	}
}
}

