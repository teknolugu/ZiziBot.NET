using System;
using System.Globalization;

namespace WinTenDev.Zizi.Models.Attributes;

[AttributeUsage(AttributeTargets.Assembly)]
public class BuildDateAttribute : Attribute
{
    public DateTime BuildDate { get; set; }

    public BuildDateAttribute(string value)
    {
        BuildDate = DateTime.ParseExact(
            value,
            "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None
        );
    }
}