using System;
using System.Collections.Generic;
using Humanizer;
using Slapper;

namespace WinTenDev.Zizi.Utils;

public static class MapperUtil
{
    public static T DictionaryMapper<T>(this Dictionary<string, object> dictionary)
    {
        return AutoMapper.Map<T>(dictionary);
    }

    public static Dictionary<string, object> ToDictionary(
        this object values,
        bool enumToString = true,
        bool skipZeroNullOrEmpty = false
    )
    {
        var dictionary = new Dictionary<string, object>();

        if (values == null) return dictionary;

        var properties = values.GetType()
            .GetProperties();

        foreach (var property in properties)
        {
            var value = property.GetValue(values, null) ?? string.Empty;

            if ((value.ToString().IsNullOrEmpty() ||
                value.ToString() == "0") &&
                skipZeroNullOrEmpty) continue;

            if (property.PropertyType.IsEnum && enumToString)
            {
                value = value.ToString();
            }

            dictionary.Add(
                property.Name.Underscore(),
                value
            );
        }

        return dictionary;
    }

    public static string ToTableName<TEntity>()
    {
        var tableName = typeof(TEntity).Name.Pluralize().Underscore();

        return tableName;
    }

    public static TValue ToEnum<TValue>(
        this string value,
        TValue defaultValue
    ) where TValue : struct
    {
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return Enum.TryParse<TValue>(
            value,
            true,
            out var result
        )
            ? result
            : defaultValue;
    }
}