using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WinTenDev.Zizi.Models.Types;

public class EgsFreeGameParsed
{
    public string Text { get; set; }
    public string Detail { get; set; }
    public Uri Images { get; set; }
}

public class EgsFreeGame
{
    public List<Element> AllGames { get; set; }
    public List<Element> FreeGames { get; set; }
    public List<Element> DiscountGames { get; set; }
}

public class EgsFreeGameRaw
{
    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public Data Data { get; set; }

    [JsonProperty("extensions", NullValueHandling = NullValueHandling.Ignore)]
    public Extensions Extensions { get; set; }
}

public class Data
{
    [JsonProperty("Catalog", NullValueHandling = NullValueHandling.Ignore)]
    public Catalog Catalog { get; set; }
}

public class Catalog
{
    [JsonProperty("searchStore", NullValueHandling = NullValueHandling.Ignore)]
    public SearchStore SearchStore { get; set; }
}

public class SearchStore
{
    [JsonProperty("elements", NullValueHandling = NullValueHandling.Ignore)]
    public List<Element> Elements { get; set; }

    [JsonProperty("paging", NullValueHandling = NullValueHandling.Ignore)]
    public Paging Paging { get; set; }
}

public class Element
{
    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string Title { get; set; }

    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    [JsonProperty("namespace", NullValueHandling = NullValueHandling.Ignore)]
    public string Namespace { get; set; }

    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    public string Description { get; set; }

    [JsonProperty("effectiveDate", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? EffectiveDate { get; set; }

    [JsonProperty("offerType", NullValueHandling = NullValueHandling.Ignore)]
    public OfferType OfferType { get; set; }

    [JsonProperty("expiryDate")]
    public DateTimeOffset? ExpiryDate { get; set; }

    [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
    public Status? Status { get; set; }

    [JsonProperty("isCodeRedemptionOnly", NullValueHandling = NullValueHandling.Ignore)]
    public bool? IsCodeRedemptionOnly { get; set; }

    [JsonProperty("keyImages", NullValueHandling = NullValueHandling.Ignore)]
    public List<KeyImage> KeyImages { get; set; }

    [JsonProperty("seller", NullValueHandling = NullValueHandling.Ignore)]
    public Seller Seller { get; set; }

    [JsonProperty("productSlug")]
    public string ProductSlug { get; set; }

    [JsonProperty("urlSlug", NullValueHandling = NullValueHandling.Ignore)]
    public string UrlSlug { get; set; }

    [JsonProperty("url")]
    public object Url { get; set; }

    [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
    public List<Item> Items { get; set; }

    [JsonProperty("customAttributes", NullValueHandling = NullValueHandling.Ignore)]
    public List<CustomAttribute> CustomAttributes { get; set; }

    [JsonProperty("categories", NullValueHandling = NullValueHandling.Ignore)]
    public List<Category> Categories { get; set; }

    [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
    public List<Tag> Tags { get; set; }

    [JsonProperty("catalogNs", NullValueHandling = NullValueHandling.Ignore)]
    public CatalogNs CatalogNs { get; set; }

    [JsonProperty("offerMappings", NullValueHandling = NullValueHandling.Ignore)]
    public List<Mapping> OfferMappings { get; set; }

    [JsonProperty("price", NullValueHandling = NullValueHandling.Ignore)]
    public Price Price { get; set; }

    [JsonProperty("promotions")]
    public Promotions Promotions { get; set; }
}

public class CatalogNs
{
    [JsonProperty("mappings", NullValueHandling = NullValueHandling.Ignore)]
    public List<Mapping> Mappings { get; set; }
}

public class Mapping
{
    [JsonProperty("pageSlug", NullValueHandling = NullValueHandling.Ignore)]
    public string PageSlug { get; set; }

    [JsonProperty("pageType", NullValueHandling = NullValueHandling.Ignore)]
    public string PageType { get; set; }
}

public class Category
{
    [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
    public string Path { get; set; }
}

public class CustomAttribute
{
    [JsonProperty("key", NullValueHandling = NullValueHandling.Ignore)]
    public string Key { get; set; }

    [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
    public string Value { get; set; }
}

public class Item
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    [JsonProperty("namespace", NullValueHandling = NullValueHandling.Ignore)]
    public string Namespace { get; set; }
}

public class KeyImage
{
    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
    public Uri Url { get; set; }
}

public class Price
{
    [JsonProperty("totalPrice", NullValueHandling = NullValueHandling.Ignore)]
    public TotalPrice TotalPrice { get; set; }

    [JsonProperty("lineOffers", NullValueHandling = NullValueHandling.Ignore)]
    public List<LineOffer> LineOffers { get; set; }
}

public class LineOffer
{
    [JsonProperty("appliedRules", NullValueHandling = NullValueHandling.Ignore)]
    public List<AppliedRule> AppliedRules { get; set; }
}

public class AppliedRule
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    [JsonProperty("endDate", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? EndDate { get; set; }

    [JsonProperty("discountSetting", NullValueHandling = NullValueHandling.Ignore)]
    public AppliedRuleDiscountSetting DiscountSetting { get; set; }
}

public class AppliedRuleDiscountSetting
{
    [JsonProperty("discountType", NullValueHandling = NullValueHandling.Ignore)]
    public string DiscountType { get; set; }
}

public class TotalPrice
{
    [JsonProperty("discountPrice", NullValueHandling = NullValueHandling.Ignore)]
    public long? DiscountPrice { get; set; }

    [JsonProperty("originalPrice", NullValueHandling = NullValueHandling.Ignore)]
    public long? OriginalPrice { get; set; }

    [JsonProperty("voucherDiscount", NullValueHandling = NullValueHandling.Ignore)]
    public long? VoucherDiscount { get; set; }

    [JsonProperty("discount", NullValueHandling = NullValueHandling.Ignore)]
    public long? Discount { get; set; }

    [JsonProperty("currencyCode", NullValueHandling = NullValueHandling.Ignore)]
    public CurrencyCode? CurrencyCode { get; set; }

    [JsonProperty("currencyInfo", NullValueHandling = NullValueHandling.Ignore)]
    public CurrencyInfo CurrencyInfo { get; set; }

    [JsonProperty("fmtPrice", NullValueHandling = NullValueHandling.Ignore)]
    public FmtPrice FmtPrice { get; set; }
}

public class CurrencyInfo
{
    [JsonProperty("decimals", NullValueHandling = NullValueHandling.Ignore)]
    public long? Decimals { get; set; }
}

public class FmtPrice
{
    [JsonProperty("originalPrice", NullValueHandling = NullValueHandling.Ignore)]
    public string OriginalPrice { get; set; }

    [JsonProperty("discountPrice", NullValueHandling = NullValueHandling.Ignore)]
    public string DiscountPrice { get; set; }

    [JsonProperty("intermediatePrice", NullValueHandling = NullValueHandling.Ignore)]
    public string IntermediatePrice { get; set; }
}

public class Promotions
{
    [JsonProperty("promotionalOffers", NullValueHandling = NullValueHandling.Ignore)]
    public List<PromotionalOffer>? PromotionalOffers { get; set; }

    [JsonProperty("upcomingPromotionalOffers", NullValueHandling = NullValueHandling.Ignore)]
    public List<PromotionalOffer>? UpcomingPromotionalOffers { get; set; }
}

public class PromotionalOffer
{
    [JsonProperty("promotionalOffers", NullValueHandling = NullValueHandling.Ignore)]
    public List<PromotionalOfferPromotionalOffer> PromotionalOffers { get; set; }
}

public class PromotionalOfferPromotionalOffer
{
    [JsonProperty("startDate", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? StartDate { get; set; }

    [JsonProperty("endDate", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? EndDate { get; set; }

    [JsonProperty("discountSetting", NullValueHandling = NullValueHandling.Ignore)]
    public PromotionalOfferDiscountSetting DiscountSetting { get; set; }
}

public class PromotionalOfferDiscountSetting
{
    [JsonProperty("discountType", NullValueHandling = NullValueHandling.Ignore)]
    public string DiscountType { get; set; }

    [JsonProperty("discountPercentage", NullValueHandling = NullValueHandling.Ignore)]
    public long? DiscountPercentage { get; set; }
}

public class Seller
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }
}

public class Tag
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(ParseStringConverter))]
    public long? Id { get; set; }
}

public class Paging
{
    [JsonProperty("count", NullValueHandling = NullValueHandling.Ignore)]
    public long? Count { get; set; }

    [JsonProperty("total", NullValueHandling = NullValueHandling.Ignore)]
    public long? Total { get; set; }
}

public class Extensions
{
}

[JsonConverter(typeof(OfferTypeConverter))]
public enum OfferType
{
    BaseGame,
    Dlc,
    Others
}

public enum CurrencyCode
{
    Idr
}

public enum Status
{
    Active
}

internal static class Converter
{
    public static readonly JsonSerializerSettings Settings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters =
        {
            OfferTypeConverter.Singleton,
            CurrencyCodeConverter.Singleton,
            StatusConverter.Singleton,
            new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
        }
    };
}

internal class OfferTypeConverter : JsonConverter
{
    public override bool CanConvert(Type t) => t == typeof(OfferType) || t == typeof(OfferType?);

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
            case "BASE_GAME":
                return OfferType.BaseGame;
            case "DLC":
                return OfferType.Dlc;
            case "OTHERS":
                return OfferType.Others;
        }

        return null;
        // throw new Exception("Cannot unmarshal type OfferType");
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
        var value = (OfferType) untypedValue;
        switch (value)
        {
            case OfferType.BaseGame:
                serializer.Serialize(writer, "BASE_GAME");
                return;
            case OfferType.Dlc:
                serializer.Serialize(writer, "DLC");
                return;
            case OfferType.Others:
                serializer.Serialize(writer, "OTHERS");
                return;
        }
        // throw new Exception("Cannot marshal type OfferType");
    }

    public static readonly OfferTypeConverter Singleton = new();
}

internal class CurrencyCodeConverter : JsonConverter
{
    public override bool CanConvert(Type t) => t == typeof(CurrencyCode) || t == typeof(CurrencyCode?);

    public override object ReadJson(
        JsonReader reader,
        Type t,
        object existingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var value = serializer.Deserialize<string>(reader);
        if (value == "IDR")
        {
            return CurrencyCode.Idr;
        }

        return null;
        // throw new Exception("Cannot unmarshal type CurrencyCode");
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
        var value = (CurrencyCode) untypedValue;
        if (value == CurrencyCode.Idr)
        {
            serializer.Serialize(writer, "IDR");
            return;
        }
        // throw new Exception("Cannot marshal type CurrencyCode");
    }

    public static readonly CurrencyCodeConverter Singleton = new();
}

internal class StatusConverter : JsonConverter
{
    public override bool CanConvert(Type t) => t == typeof(Status) || t == typeof(Status?);

    public override object ReadJson(
        JsonReader reader,
        Type t,
        object existingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var value = serializer.Deserialize<string>(reader);
        if (value == "ACTIVE")
        {
            return Status.Active;
        }
        throw new Exception("Cannot unmarshal type Status");
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
        var value = (Status) untypedValue;
        if (value == Status.Active)
        {
            serializer.Serialize(writer, "ACTIVE");
            return;
        }
        throw new Exception("Cannot marshal type Status");
    }

    public static readonly StatusConverter Singleton = new();
}

internal class ParseStringConverter : JsonConverter
{
    public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

    public override object ReadJson(
        JsonReader reader,
        Type t,
        object existingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var value = serializer.Deserialize<string>(reader);
        long l;
        if (Int64.TryParse(value, out l))
        {
            return l;
        }
        throw new Exception("Cannot unmarshal type long");
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
        var value = (long) untypedValue;
        serializer.Serialize(writer, value.ToString());
    }

    public static readonly ParseStringConverter Singleton = new();
}