using Hangfire;
using Telegram.Bot;
using WinTenDev.Zizi.Models.Configs;

namespace WinTenDev.Zizi.Services.Internals;

public static class HangfireJobsService
{

    [AutomaticRetry(Attempts = 2, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public static void SendGroot()
    {
        BotSettings.Client.SendTextMessageAsync("-1001404591750", "I'm Groot");
    }
}