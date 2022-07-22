using System;
using Newtonsoft.Json;

namespace WinTenDev.Zizi.Models.JsonConverters;

public class EpochSecondOrDateStrToDateTimeOffsetConverter : JsonConverter
{
    public override void WriteJson(
        JsonWriter writer,
        object value,
        JsonSerializer serializer
    )
    {
        throw new NotImplementedException();
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer
    )
    {
        var t = Nullable.GetUnderlyingType(objectType);

        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType == JsonToken.Date)
        {
            var dateTimeOffset = DateTime.SpecifyKind(DateTime.Parse(reader.Value.ToString()), DateTimeKind.Utc);
            DateTimeOffset? date = dateTimeOffset;

            return date;
        }
        else if (reader.TokenType == JsonToken.Integer)
        {
            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds((long) reader.Value);

            return dateTimeOffset;
        }
        else
        {
            return reader.Value;
        }

        // else if (reader.TokenType == JsonToken.String)
        // {
        //     var str = reader.Value.ToString();
        //     if (string.IsNullOrEmpty(str))
        //         return null;
        //     return DateTimeOffset.FromUnixTimeSeconds(long.Parse(str));
        // }
        // else if (t == typeof(DateTimeOffset))
        // {
        //     return DateTimeOffset.ParseExact(
        //         input: reader.Value.ToString(),
        //         format: string.Empty,
        //         formatProvider: CultureInfo.CurrentCulture,
        //         styles: DateTimeStyles.RoundtripKind
        //     );
        // }
        // else
        // {
        //     return reader.Value;
        // }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DateTimeOffset);
    }
}