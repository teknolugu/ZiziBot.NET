using System.Threading;
using System.Threading.Tasks;
using DotNurse.Injector.Attributes;
using WinTenDev.Zizi.Models.Interfaces;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.Zizi.Services.StartupTasks;

public class StartupNotificationStartupTask : IStartupTask
{
    [InjectService]
    private BotService BotService { get; set; }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {

    }
}
