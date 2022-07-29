using System.Threading;
using Localtunnel;
using Localtunnel.Connections;
using Localtunnel.Tunnels;
using Microsoft.Extensions.Hosting;
using Nito.AsyncEx.Synchronous;
using Serilog;

namespace WinTenDev.Zizi.Services.Externals;

public class LocalTunnelService
{
    private readonly IHostApplicationLifetime _applicationLifetime;

    public LocalTunnelService(IHostApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public Tunnel CreateTunnel(
        string subdomain,
        string port
    )
    {
        return CreateTunnel(subdomain, port.ToInt());
    }

    public Tunnel CreateTunnel(
        string subdomain,
        int port
    )
    {
        var tunnelClient = new LocaltunnelClient();

        Log.Information(
            "Creating tunnel for subdomain {Subdomain}:{Port}",
            subdomain,
            port
        );

        var tunnel = tunnelClient.OpenAsync(
                handle => {
                    var options = new ProxiedHttpTunnelOptions()
                    {
                        Host = "localhost",
                        Port = port,
                        ReceiveBufferSize = 100
                    };
                    return new ProxiedHttpTunnelConnection(handle, options);
                },
                subdomain: subdomain,
                CancellationToken.None
            )
            .WaitAndUnwrapException();

        tunnel.StartAsync().WaitAndUnwrapException();

        var tunnelUrl = tunnel.Information.Url;
        var tunnelId = tunnel.Information.Id;

        Log.Information("Tunnel URL: {TunnelUrl}", tunnelUrl);
        Log.Information("Tunnel URL: {TunnelId}", tunnelId);

        return tunnel;
    }
}