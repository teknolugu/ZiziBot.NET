using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MoreLinq.Extensions;
using WinTenDev.Zizi.Models.Attributes;

namespace WinTenDev.Zizi.Utils;

public static class AssemblyUtil
{
    public static Type[] GetTypesInNamespace(
        this Assembly assembly,
        string nameSpace
    )
    {
        return
            assembly.GetTypes()
                .Where(
                    t => String.Equals(
                        t.Namespace,
                        nameSpace,
                        StringComparison.Ordinal
                    )
                )
                .ToArray();
    }

    public static IEnumerable<Type> GetEntireTypeOfAssembly()
    {
        var enumerableTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Select(
                assembly => assembly
                    .GetTypes()
                    .AsEnumerable()
            )
            .Flatten()
            .Cast<Type>();

        return enumerableTypes;
    }

    public static DateTime GetBuildDate(this Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<BuildDateAttribute>();
        return attribute != null ? attribute.BuildDate : default(DateTime);
    }

    public static DateTime GetLinkerTime(this Assembly assembly)
    {
        return File.GetLastWriteTime(assembly.Location);
    }

    public static DateTime GetBuildDate()
    {
#if DEBUG
        var buildDate = Assembly.GetExecutingAssembly().GetLinkerTime();
#else
        var buildDate = Assembly.GetExecutingAssembly().GetBuildDate();
#endif

        return buildDate;
    }
}