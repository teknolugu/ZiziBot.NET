using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MoreLinq.Extensions;

namespace WinTenDev.Zizi.Utils;

public static class AssemblyUtil
{
    public static Type[] GetTypesInNamespace(this Assembly assembly, string nameSpace)
    {
        return
            assembly.GetTypes()
                .Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal))
                .ToArray();
    }

    public static IEnumerable<Type> GetEntireTypeOfAssembly()
    {
        var enumerableTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly
                .GetTypes()
                .AsEnumerable())
            .Flatten()
            .Cast<Type>();

        return enumerableTypes;
    }
}