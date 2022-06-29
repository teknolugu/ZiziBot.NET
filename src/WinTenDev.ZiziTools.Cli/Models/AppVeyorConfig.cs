using Newtonsoft.Json;

namespace WinTenDev.ZiziTools.Cli.Models;

public class AppVeyorConfig
{
    [JsonProperty("environment")]
    public AppVeyorEnvironment Environment { get; set; }
}

public partial class AppVeyorEnvironment
{
    [JsonProperty("VersionNumber")]
    public string VersionNumber { get; set; }
}
