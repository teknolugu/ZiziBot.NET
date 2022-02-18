using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WinTenDev.Zizi.Models.Types.BinderByte;

public partial class BinderByteCekResi
{
    [JsonProperty("status")]
    public long Status { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("data")]
    public Data Data { get; set; }
}

public partial class Data
{
    [JsonProperty("summary")]
    public Summary Summary { get; set; }

    [JsonProperty("detail")]
    public Detail Detail { get; set; }

    [JsonProperty("history")]
    public List<History> History { get; set; }
}

public partial class Detail
{
    [JsonProperty("origin")]
    public string Origin { get; set; }

    [JsonProperty("destination")]
    public string Destination { get; set; }

    [JsonProperty("shipper")]
    public string Shipper { get; set; }

    [JsonProperty("receiver")]
    public string Receiver { get; set; }
}

public partial class History
{
    [JsonProperty("date")]
    public DateTimeOffset Date { get; set; }

    [JsonProperty("desc")]
    public string Desc { get; set; }

    [JsonProperty("location")]
    public string Location { get; set; }
}

public partial class Summary
{
    [JsonProperty("awb")]
    public string Awb { get; set; }

    [JsonProperty("courier")]
    public string Courier { get; set; }

    [JsonProperty("service")]
    public string Service { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("date")]
    public string Date { get; set; }

    [JsonProperty("desc")]
    public string Desc { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("weight")]
    public string Weight { get; set; }
}
