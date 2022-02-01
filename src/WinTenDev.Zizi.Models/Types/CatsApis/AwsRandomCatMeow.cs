using System;
using Newtonsoft.Json;

namespace WinTenDev.Zizi.Models.Types.CatsApis;

public class AwsRandomCatMeow
{
    [JsonProperty("file")]
    public Uri File { get; set; }
}