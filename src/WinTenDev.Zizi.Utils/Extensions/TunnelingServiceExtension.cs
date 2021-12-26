using System.Threading;
using Localtunnel;
using Localtunnel.Connections;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class TunnelingServiceExtension
{
    public static IServiceCollection AddLocalTunnelClient(this IServiceCollection services)
    {
        services.AddScoped(service => {
            var clientTunnel = new LocaltunnelClient();
            return clientTunnel;
        });
        return services;
    }

    public static IApplicationBuilder UseLocalTunnel(this IApplicationBuilder app, string subdomain)
    {
        var services = app.GetServiceProvider();
        var tunnelClient = services.GetRequiredService<LocaltunnelClient>();

        var tunnel = tunnelClient.OpenAsync(handle => {
            var options = new ProxiedHttpTunnelOptions()
            {
                Host = "localhost",
                Port = 5100,
                ReceiveBufferSize = 10
            };
            return new ProxiedHttpTunnelConnection(handle, options);
        }, subdomain: subdomain, CancellationToken.None).Result;

        tunnel.StartAsync();

        var tunnelUrl = tunnel.Information.Url;
        var tunnelId = tunnel.Information.Id;

        Log.Information("Tunnel URL: {TunnelUrl}", tunnelUrl);
        Log.Information("Tunnel URL: {TunnelId}", tunnelId);

        return app;
    }
}