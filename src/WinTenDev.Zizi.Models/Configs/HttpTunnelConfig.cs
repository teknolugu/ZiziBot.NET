namespace WinTenDev.Zizi.Models.Configs;

public class HttpTunnelConfig
{
    public bool IsEnabled { get; set; }
    public string LocalXposeBinaryPath { get; set; }
    public string ReservedSubdomain { get; set; }
}