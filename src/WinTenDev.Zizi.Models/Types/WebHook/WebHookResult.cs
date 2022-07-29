using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Types.WebHook;

public class WebHookResult
{
    public WebhookSource WebhookSource { get; set; }
    public string ParsedMessage { get; set; }
}