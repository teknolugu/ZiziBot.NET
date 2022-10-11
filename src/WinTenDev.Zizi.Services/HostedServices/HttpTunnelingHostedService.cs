using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace WinTenDev.Zizi.Services.HostedServices;

public class HttpTunnelingHostedService : BackgroundService
{
    private readonly IOptionsSnapshot<HttpTunnelConfig> _httpTunnelConfigSnapshot;
    private readonly IServer _server;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<HttpTunnelingHostedService> _logger;
    private readonly LocalXposeService _localXposeService;

    private HttpTunnelConfig HttpTunnelConfig => _httpTunnelConfigSnapshot.Value;

    public HttpTunnelingHostedService(
        ILogger<HttpTunnelingHostedService> logger,
        IOptionsSnapshot<HttpTunnelConfig> httpTunnelConfigSnapshot,
        IHostApplicationLifetime hostApplicationLifetime,
        IServer server,
        IConfiguration config,
        IServiceProvider serviceProvider
    )
    {
        _httpTunnelConfigSnapshot = httpTunnelConfigSnapshot;
        _server = server;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
        _localXposeService = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<LocalXposeService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForApplicationStarted();

        if (!HttpTunnelConfig.IsEnabled)
        {
            _logger.LogInformation("Http Tunneling is disabled");
            return;
        }

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
                    _logger.LogInformation("Starting HTTP tunnel to {Host}", localUrl);

                    var commandResult = _localXposeService.CreateTunnel(localUrl);

                    if (commandResult == null)
                    {
                        _logger.LogWarning("Seem NOT yet prepared for HTTP tunneling");
                        return;
                    }

                    _logger.LogInformation("HTTP tunnel Result: {@TunnelId}", commandResult);

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