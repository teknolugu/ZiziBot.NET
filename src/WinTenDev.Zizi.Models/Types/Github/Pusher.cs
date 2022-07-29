using Newtonsoft.Json;

namespace WinTenDev.Zizi.Models.Types.Github
{

    public partial class Pusher
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}