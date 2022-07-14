using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinTenDev.Zizi.Models.Attributes;
using WinTenDev.Zizi.Models.Interfaces;

namespace WinTenDev.Zizi.Extensions;

public static class StartupTaskHostExtensions
{
    public static async Task RunWithTasksAsync(
        this IHost host,
        bool taskOnly = true,
        CancellationToken cancellationToken = default
    )
    {
        await host.Services.ExecuteStartupTask(false, cancellationToken: cancellationToken);

        if (taskOnly) return;

        // Start the tasks as normal
        await host.RunAsync(cancellationToken);
    }

    public static async Task RunStartupTasksAsync(
        this IApplicationBuilder app,
        bool taskOnly = true,
        CancellationToken cancellationToken = default
    )
    {
        await app.ApplicationServices.ExecuteStartupTask(true, cancellationToken: cancellationToken);
    }

    public static IServiceCollection AddStartupTask<T>(this IServiceCollection services)
        where T : class, IStartupTask
        => services.AddTransient<IStartupTask, T>();

    private static async Task ExecuteStartupTask(
        this IServiceProvider serviceProvider,
        bool executeOnReady,
        CancellationToken cancellationToken = default
    )
    {
        var log = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(IStartupTask));

        // Load all tasks from DI
        var startupTasks = serviceProvider.GetServices<IStartupTask>().ToList();
        var filteredTasks = startupTasks
            .Where(task => task.GetType().GetCustomAttribute<StartupTaskAttribute>()?.AfterHostReady == executeOnReady)
            .ToList();

        log.LogInformation("Starting executing IStartup task: {Count} task(s)", filteredTasks.Count);

        // Execute all the tasks
        foreach (var startupTask in filteredTasks)
        {
            log.LogDebug("Executing task: {TaskName}", startupTask.GetType().FullName);

            await startupTask.ExecuteAsync(cancellationToken);
        }

        log.LogInformation("About {Count} startup Task successfully executed.", filteredTasks.Count);
    }
}