using Newtonsoft.Json;

namespace WinTenDev.WebHook.AppHost.Models.Github
{
    public partial class Author
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }
    }
}