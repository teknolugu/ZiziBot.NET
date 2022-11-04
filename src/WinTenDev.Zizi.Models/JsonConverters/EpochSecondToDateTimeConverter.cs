using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WinTenDev.Zizi.Models.JsonConverters;

public class EpochSecondToDateTimeConverter : JsonConverter
{
    public override void WriteJson(
        JsonWriter writer,
        object value,
        JsonSerializer serializer
    )
    {
        var jObject = new JObject();
        try
        {
            jObject["$date"] = new DateTimeOffset((DateTime)value).ToUnixTimeMilliseconds();
        }
        catch
        {
            jObject["$date"] = default;
        }

        jObject.WriteTo(writer);
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer
    )
    {
        try
        {
            var obj = reader.Value is long value ? value : 0;
            var date = DateTimeOffset.FromUnixTimeSeconds(obj).UtcDateTime;

            return date;
        }
        catch (Exception e)
        {
            return default;
        }
    }

    public override bool CanRead => true;

    public override bool CanConvert(Type objectType) => objectType == typeof(DateTime);
}