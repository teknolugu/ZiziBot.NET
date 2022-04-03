using System;
using Newtonsoft.Json;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Models.Vendors.FathimahApi
{
    public class ShalatTimeResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("query")]
        public Query Query { get; set; }

        [JsonProperty("jadwal")]
        public Jadwal Jadwal { get; set; }
    }

    public class Jadwal
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("data")]
        public Data Data { get; set; }
    }

    public class Data
    {
        [JsonProperty("ashar")]
        public string Ashar { get; set; }

        [JsonProperty("dhuha")]
        public string Dhuha { get; set; }

        [JsonProperty("dzuhur")]
        public string Dzuhur { get; set; }

        [JsonProperty("imsak")]
        public string Imsak { get; set; }

        [JsonProperty("isya")]
        public string Isya { get; set; }

        [JsonProperty("maghrib")]
        public string Maghrib { get; set; }

        [JsonProperty("subuh")]
        public string Subuh { get; set; }

        [JsonProperty("tanggal")]
        public string Tanggal { get; set; }

        [JsonProperty("terbit")]
        public string Terbit { get; set; }
    }

    public class Query
    {
        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("kota")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Kota { get; set; }

        [JsonProperty("tanggal")]
        public DateTimeOffset Tanggal { get; set; }
    }
}
