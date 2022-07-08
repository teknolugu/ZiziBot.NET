using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WinTenDev.Zizi.Models.Interfaces;

namespace WinTenDev.Zizi.Extensions;

public static class StartupTaskWebHostExtensions
{
    public static async Task RunWithTasksAsync(
        this IHost webHost,
        bool taskOnly = true,
        CancellationToken cancellationToken = default
    )
    {
        // Load all tasks from DI
        var startupTasks = webHost.Services.GetServices<IStartupTask>();

        // Execute all the tasks
        foreach (var startupTask in startupTasks)
        {
            await startupTask.ExecuteAsync(cancellationToken);
        }

        if (taskOnly)
        {
            return;
        }

        // Start the tasks as normal
        await webHost.RunAsync(cancellationToken);
    }

    public static IServiceCollection AddStartupTask<T>(this IServiceCollection services)
        where T : class, IStartupTask
        => services.AddTransient<IStartupTask, T>();
}
