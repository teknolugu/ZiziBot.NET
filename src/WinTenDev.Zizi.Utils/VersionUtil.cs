using System;

namespace WinTenDev.Zizi.Utils;

public static class VersionUtil
{
    public static int GetBuildNumber()
    {
        var jan2000 = new DateTime(2000, 1, 1);
        var today = DateTime.UtcNow;
        var diffDay = (today - jan2000).Days;

        return diffDay;
    }

    public static double GetRevNumber()
    {
        var dateNow = DateTime.UtcNow.ToString("h:mm:ss");
        var seconds = TimeSpan.Parse(dateNow).TotalSeconds;
        return seconds;
    }
}