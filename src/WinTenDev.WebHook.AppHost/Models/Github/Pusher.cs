using Newtonsoft.Json;

namespace WinTenDev.WebHook.AppHost.Models.Github
{

    public partial class Pusher
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}