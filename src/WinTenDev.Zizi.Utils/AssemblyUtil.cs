using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MoreLinq.Extensions;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Attributes;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils.Telegram;
using File=System.IO.File;

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

    public static string FormatVersion(this Version version)
    {
        var versionStr = $"{version.Major}.{version.Minor} Build {version.Build}";
        return versionStr;
    }

    public static HtmlMessage GetAboutHeader(this User me)
    {
        var meFullName = me.GetFullName();
        var currentAssembly = Assembly.GetExecutingAssembly().GetName();
        var buildNumber = currentAssembly.Version?.Build.ToString();
        var assemblyVersion = currentAssembly.Version?.ToString();
        var buildDate = AssemblyUtil.GetBuildDate();

        var htmlMessage = HtmlMessage.Empty
            .Bold($"{meFullName} 4 Build {buildNumber}").Br()
            .Bold("Version: ").Code(assemblyVersion).Br()
            .Bold("Build Date: ").Code(buildDate.ToDetailDateTimeString()).Br();

        return htmlMessage;
    }
}