using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Serilog;

namespace WinTenDev.Zizi.Services.HostedServices;

public class HttpTunnelingHostedService : BackgroundService
{
    private readonly IServer _server;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger _logger;
    private readonly LocalXposeService _localXposeService;

    public HttpTunnelingHostedService(
        IServer server,
        IHostApplicationLifetime hostApplicationLifetime,
        IConfiguration config,
        ILogger logger,
        IServiceProvider serviceProvider
    )
    {
        _server = server;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
        _localXposeService = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<LocalXposeService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForApplicationStarted();

        var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses;
        var localUrl = addresses?.SingleOrDefault(u => u.StartsWith("http://"));
        var urls = localUrl.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
        var host = urls.ElementAtOrDefault(1).TrimStart('/');
        var port = urls.ElementAtOrDefault(2).ToInt();

        await Policy
            .Handle<Exception>()
            .WaitAndRetry(
                10,
                retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
            )
            .Execute(
                async () => {
                    _logger.Information("Starting HTTP tunnel to {Host}", localUrl);

                    var commandResult = _localXposeService.CreateTunnel(localUrl);

                    if (commandResult == null)
                    {
                        _logger.Warning("Seem NOT yet prepared for HTTP tunneling");
                        return;
                    }

                    _logger.Information("HTTP tunnel Result: {@TunnelId}", commandResult);

                    await commandResult.ExecuteAsync();
                }
            );
    }

    private Task WaitForApplicationStarted()
    {
        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _hostApplicationLifetime.ApplicationStarted.Register(() => completionSource.TrySetResult());
        return completionSource.Task;
    }
}