namespace WinTenDev.Zizi.Models.Configs;

public class BinderByteConfig
{
    public bool IsEnabled { get; set; }
    public string ApiUrl { get; set; }
    public string ApiToken { get; set; }

    public void Deconstruct(
        out bool isEnabled,
        out string apiUrl,
        out string apiToken
    )
    {
        isEnabled = IsEnabled;
        apiUrl = ApiUrl;
        apiToken = ApiToken;
    }
}