using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace WinTenDev.Zizi.Utils.BuildTasks;

public class VersionUpdateBuildTask : Task
{
    [Output]
    public string Version { get; set; }

    public override bool Execute()
    {
        var buildNumber = VersionUtil.GetBuildNumber();
        var revNumber = VersionUtil.GetRevNumber();

        Version = $"1.0.{buildNumber}.{revNumber}";
        Log.LogMessage($"Version: {Version}");

        return true;
    }
}