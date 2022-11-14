using System.Threading;
using System.Threading.Tasks;

namespace WinTenDev.Zizi.Services.StartupTasks;

[StartupTask(AfterHostReady = true)]
public class StartupNotificationStartupTask : IStartupTask
{
    private readonly BotService _botService;

    public StartupNotificationStartupTask(BotService botService)
    {
        _botService = botService;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await _botService.SendStartupNotification();
    }
}