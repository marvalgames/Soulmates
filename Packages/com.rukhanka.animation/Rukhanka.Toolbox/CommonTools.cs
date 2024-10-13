
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Toolbox
{
public static class CommonTools
{
    public static string FormatMemory(long mem)
    {
        float floatMem = mem;
        string[] suffixes = {"B", "KB", "MB", "GB", "TB"};
        var suffixIndex = 0;
        while (floatMem > 1024.0f)
        {
            ++suffixIndex;
            floatMem /= 1024.0f;
        }
        
        return $"{floatMem:F2}{suffixes[suffixIndex]}";
    }
}
}
