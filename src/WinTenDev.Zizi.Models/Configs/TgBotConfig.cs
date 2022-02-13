using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Configs;

public class TgBotConfig
{
    public string Username { get; set; }
    public string ApiToken { get; set; }

    public string WebhookDomain { get; set; }
    public string WebhookPath { get; set; }

    public EngineMode EngineMode { get; set; }
    public bool EnableLocalTunnel { get; set; }
}