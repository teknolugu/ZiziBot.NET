namespace WinTenDev.Zizi.Utils;

public static class CronUtil
{
    public static string InMinute(int minute)
    {
        return $"*/{minute} * * * *";
    }
}
