namespace WinTenDev.Zizi.Models.Configs;

public class GrafanaConfig
{
    public bool IsEnabled { get; set; }
    public string LokiUrl { get; set; }
    public string LokiLogin { get; set; }
    public string LokiPassword { get; set; }
}