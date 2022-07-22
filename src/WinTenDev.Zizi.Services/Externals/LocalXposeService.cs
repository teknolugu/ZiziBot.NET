using CliWrap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WinTenDev.Zizi.Services.Externals;

// [Injectable(InjectType.Singleton)]
public class LocalXposeService
{
    private readonly ILogger<LocalXposeService> _logger;
    private readonly HttpTunnelConfig _httpTunnelConfig;

    public LocalXposeService(
        ILogger<LocalXposeService> logger,
        IOptions<HttpTunnelConfig> httpTunnelConfig
    )
    {
        _logger = logger;
        _httpTunnelConfig = httpTunnelConfig.Value;
    }

    public Command CreateTunnel(string tunnelTo = "localhost:5000")
    {
        var localXposeBin = _httpTunnelConfig.LocalXposeBinaryPath;
        var subDomain = _httpTunnelConfig.ReservedSubdomain;

        if (!_httpTunnelConfig.IsEnabled)
        {
            return default;
        }

        var command = Cli.Wrap(localXposeBin)
                .WithArguments($"tunnel http --subdomain {subDomain} --to {tunnelTo}")
            // .WithStandardOutputPipe(
            //     PipeTarget.ToDelegate(
            //         s => {
            //             _logger.LogDebug("Tunnel output: " + s);
            //         }
            //     )
            // )
            ;

        return command;
    }
}