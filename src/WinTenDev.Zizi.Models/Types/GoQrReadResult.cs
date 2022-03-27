using Newtonsoft.Json;

namespace WinTenDev.Zizi.Models.Types
{
    public class GoQrReadResult
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("symbol")]
        public Symbol[] Symbol { get; set; }
    }

    public class Symbol
    {
        [JsonProperty("seq")]
        public long Seq { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }
}
