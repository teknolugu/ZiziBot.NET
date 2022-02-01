using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WinTenDev.Zizi.Models.Types.CatsApis;

public class TheCatApiCat
{
    [JsonProperty("breeds")]
    public List<object> Breeds { get; set; }

    [JsonProperty("categories")]
    public List<CatCategory> Categories { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("url")]
    public Uri Url { get; set; }

    [JsonProperty("width")]
    public long Width { get; set; }

    [JsonProperty("height")]
    public long Height { get; set; }
}

public class CatCategory
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}