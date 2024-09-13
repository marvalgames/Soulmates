using Unity.Entities;

////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Toolbox
{
public static class HashUtils
{
    public static uint Hash128To32(in Hash128 hash128)
    {
		var rv = hash128.Value.x;
		rv ^= hash128.Value.y + 0x9e3779b9 + (rv << 6) + (rv >> 2);
		rv ^= hash128.Value.z + 0x9e3779b9 + (rv << 6) + (rv >> 2);
		rv ^= hash128.Value.w + 0x9e3779b9 + (rv << 6) + (rv >> 2);
		return rv;
    }
}
}
