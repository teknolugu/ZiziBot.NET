using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WinTenDev.Zizi.Models.JsonConverters;

namespace WinTenDev.Zizi.Models.Types.UupDump;

public partial class BuildUpdate
{
    [JsonProperty("response")]
    public Response Response { get; set; }

    [JsonProperty("jsonApiVersion")]
    public string JsonApiVersion { get; set; }
}

public partial class Response
{
    [JsonProperty("apiVersion")]
    public string ApiVersion { get; set; }

    [JsonProperty("builds")]
    public List<Build> Builds { get; set; }
}

public partial class Build
{
    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("build")]
    public string BuildNumber { get; set; }

    [JsonProperty("arch")]
    public Arch Arch { get; set; }

    [JsonProperty("created")]
    [JsonConverter(typeof(EpochSecondToDateTimeConverter))]
    public DateTime Created { get; set; }

    [JsonProperty("uuid")]
    public string Uuid { get; set; }
}

public enum Arch
{
    Amd64,
    Arm64,
    X86
};

internal static class Converter
{
    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters =
        {
            ArchConverter.Singleton,
            new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
        },
    };
}

internal class ArchConverter : JsonConverter
{
    public override bool CanConvert(Type t) => t == typeof(Arch) || t == typeof(Arch?);

    public override object ReadJson(
        JsonReader reader,
        Type t,
        object existingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var value = serializer.Deserialize<string>(reader);
        switch (value)
        {
            case "amd64":
                return Arch.Amd64;
            case "arm64":
                return Arch.Arm64;
            case "x86":
                return Arch.X86;
        }
        throw new Exception("Cannot unmarshal type Arch");
    }

    public override void WriteJson(
        JsonWriter writer,
        object untypedValue,
        JsonSerializer serializer
    )
    {
        if (untypedValue == null)
        {
            serializer.Serialize(writer, null);
            return;
        }
        var value = (Arch) untypedValue;
        switch (value)
        {
            case Arch.Amd64:
                serializer.Serialize(writer, "amd64");
                return;
            case Arch.Arm64:
                serializer.Serialize(writer, "arm64");
                return;
            case Arch.X86:
                serializer.Serialize(writer, "x86");
                return;
        }
        throw new Exception("Cannot marshal type Arch");
    }

    public static readonly ArchConverter Singleton = new ArchConverter();
}
