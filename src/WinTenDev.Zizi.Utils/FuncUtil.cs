using System.Reflection;
using Humanizer;

namespace WinTenDev.Zizi.Utils;

/// <summary>
/// Extension method about Func
/// </summary>
public static class FuncUtil
{
    public static string CreateCacheKey(this MethodBase methodBase, object suffix)
    {
        var methodName = methodBase.DeclaringType?.Name.CleanExceptAlphaNumeric().Kebaberize();
        var cacheKey = $"{methodName}_{suffix}";

        return cacheKey;
    }
}