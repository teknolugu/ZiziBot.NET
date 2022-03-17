using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Serilog;
using TimeSpanParserUtil;
using WinTenDev.Zizi.Exceptions;

namespace WinTenDev.Zizi.Utils;

public static class TimeUtil
{
    public static string GetDelay(this DateTime time)
    {
        var date1 = DateTime.Now.ToUniversalTime();
        var date2 = time;

        var timeSpan = (date1 - date2);

        return timeSpan.ToString(@"s\,fff");
    }

    public static string ToHumanDuration(
        this TimeSpan duration,
        bool displaySign = false
    )
    {
        var builder = new StringBuilder();
        if (displaySign)
        {
            builder.Append(duration.TotalMilliseconds < 0 ? "-" : "+");
        }

        duration = duration.Duration();

        if (duration.Days > 0)
        {
            builder.Append($"{duration.Days}d ");
        }

        if (duration.Hours > 0)
        {
            builder.Append($"{duration.Hours}h ");
        }

        if (duration.Minutes > 0)
        {
            builder.Append($"{duration.Minutes}m ");
        }

        if (duration.TotalHours < 1)
        {
            if (duration.Seconds > 0)
            {
                builder.Append(duration.Seconds);
                if (duration.Milliseconds > 0)
                {
                    builder.Append($".{duration.Milliseconds.ToString().PadLeft(3, '0')}");
                }

                builder.Append("s ");
            }
            else
            {
                if (duration.Milliseconds > 0)
                {
                    builder.Append($"{duration.Milliseconds}ms ");
                }
            }
        }

        if (builder.Length <= 1)
        {
            builder.Append(" <1ms ");
        }

        builder.Remove(builder.Length - 1, 1);

        return builder.ToString();
    }

    public static DateTimeOffset ConvertUtcTimeToTimeZone(
        this DateTime dateTime,
        string toTimeZoneDesc
    )
    {
        if (dateTime.Kind != DateTimeKind.Utc)
        {
            Log.Error("dateTime needs to have Kind property set to Utc");
        }

        var toUtcOffset = TimeZoneInfo.FindSystemTimeZoneById(toTimeZoneDesc).GetUtcOffset(dateTime);
        var convertedTime = DateTime.SpecifyKind(dateTime.Add(toUtcOffset), DateTimeKind.Unspecified);
        return new DateTimeOffset(convertedTime, toUtcOffset);
    }

    public static string ToStringFormat(
        this TimeSpan ts,
        string format
    )
    {
        return (ts < TimeSpan.Zero ? "-" : "") + ts.ToString(format);
    }

    [CanBeNull]
    public static TimeZoneInfo FindTimeZoneByOffsetBase(string offsetBase)
    {
        var timeZones = TimeZoneInfo.GetSystemTimeZones();

        var filteredTimeZones = timeZones
            .FirstOrDefault(
                info => {
                    var isMatch = info.DisplayName.ToString().Contains(offsetBase);

                    return isMatch;
                }
            );

        if (filteredTimeZones == null) throw new TimeZoneOffsetNotFoundException(offsetBase);

        return filteredTimeZones;
    }

    public static DateTimeOffset ConvertUtcTimeToTimeOffset(
        this DateTime dateTime,
        string toTimeZone
    )
    {
        var timeZones = TimeZoneInfo.GetSystemTimeZones();

        var filteredTimeZones = timeZones
            .FirstOrDefault(
                info => {
                    var isMatch = info.DisplayName.ToString().Contains(toTimeZone);

                    return isMatch;
                }
            );

        if (filteredTimeZones == null) throw new Exception("TimeZone not found");

        var dateTimeOffset = dateTime.ConvertUtcTimeToTimeZone(filteredTimeZones?.Id);

        return dateTimeOffset;
    }

    public static string GetTimeGreet()
    {
        var greet = "dini hari";
        var hour = DateTime.Now.Hour;

        if (hour <= 3) greet = "dini hari";
        else if (hour <= 11) greet = "pagi";
        else if (hour <= 14) greet = "siang";
        else if (hour <= 17) greet = "sore";
        else if (hour <= 18) greet = "petang";
        else if (hour <= 24) greet = "malam";

        Log.Debug(
            "Current hour: {Hour}, greet: {Greet}",
            hour,
            greet
        );

        return greet;
    }

    public static string OlsonTimeZoneToTimeZoneInfo(string timeInfo)
    {
        var olsonWindowsTimes = new Dictionary<string, string>()
        {
            { "Africa/Bangui", "W. Central Africa Standard Time" },
            { "Africa/Cairo", "Egypt Standard Time" },
            { "Africa/Casablanca", "Morocco Standard Time" },
            { "Africa/Harare", "South Africa Standard Time" },
            { "Africa/Johannesburg", "South Africa Standard Time" },
            { "Africa/Lagos", "W. Central Africa Standard Time" },
            { "Africa/Monrovia", "Greenwich Standard Time" },
            { "Africa/Nairobi", "E. Africa Standard Time" },
            { "Africa/Windhoek", "Namibia Standard Time" },
            { "America/Anchorage", "Alaskan Standard Time" },
            { "America/Argentina/San_Juan", "Argentina Standard Time" },
            { "America/Asuncion", "Paraguay Standard Time" },
            { "America/Bahia", "Bahia Standard Time" },
            { "America/Bogota", "SA Pacific Standard Time" },
            { "America/Buenos_Aires", "Argentina Standard Time" },
            { "America/Caracas", "Venezuela Standard Time" },
            { "America/Cayenne", "SA Eastern Standard Time" },
            { "America/Chicago", "Central Standard Time" },
            { "America/Chihuahua", "Mountain Standard Time (Mexico)" },
            { "America/Cuiaba", "Central Brazilian Standard Time" },
            { "America/Denver", "Mountain Standard Time" },
            { "America/Fortaleza", "SA Eastern Standard Time" },
            { "America/Godthab", "Greenland Standard Time" },
            { "America/Guatemala", "Central America Standard Time" },
            { "America/Halifax", "Atlantic Standard Time" },
            { "America/Indianapolis", "US Eastern Standard Time" },
            { "America/Indiana/Indianapolis", "US Eastern Standard Time" },
            { "America/La_Paz", "SA Western Standard Time" },
            { "America/Los_Angeles", "Pacific Standard Time" },
            { "America/Mexico_City", "Mexico Standard Time" },
            { "America/Montevideo", "Montevideo Standard Time" },
            { "America/New_York", "Eastern Standard Time" },
            { "America/Noronha", "UTC-02" },
            { "America/Phoenix", "US Mountain Standard Time" },
            { "America/Regina", "Canada Central Standard Time" },
            { "America/Santa_Isabel", "Pacific Standard Time (Mexico)" },
            { "America/Santiago", "Pacific SA Standard Time" },
            { "America/Sao_Paulo", "E. South America Standard Time" },
            { "America/St_Johns", "Newfoundland Standard Time" },
            { "America/Tijuana", "Pacific Standard Time" },
            { "Antarctica/McMurdo", "New Zealand Standard Time" },
            { "Atlantic/South_Georgia", "UTC-02" },
            { "Asia/Almaty", "Central Asia Standard Time" },
            { "Asia/Amman", "Jordan Standard Time" },
            { "Asia/Baghdad", "Arabic Standard Time" },
            { "Asia/Baku", "Azerbaijan Standard Time" },
            { "Asia/Bangkok", "SE Asia Standard Time" },
            { "Asia/Beirut", "Middle East Standard Time" },
            { "Asia/Calcutta", "India Standard Time" },
            { "Asia/Colombo", "Sri Lanka Standard Time" },
            { "Asia/Damascus", "Syria Standard Time" },
            { "Asia/Dhaka", "Bangladesh Standard Time" },
            { "Asia/Dubai", "Arabian Standard Time" },
            { "Asia/Irkutsk", "North Asia East Standard Time" },
            { "Asia/Jerusalem", "Israel Standard Time" },
            { "Asia/Kabul", "Afghanistan Standard Time" },
            { "Asia/Kamchatka", "Kamchatka Standard Time" },
            { "Asia/Karachi", "Pakistan Standard Time" },
            { "Asia/Katmandu", "Nepal Standard Time" },
            { "Asia/Kolkata", "India Standard Time" },
            { "Asia/Krasnoyarsk", "North Asia Standard Time" },
            { "Asia/Kuala_Lumpur", "Singapore Standard Time" },
            { "Asia/Kuwait", "Arab Standard Time" },
            { "Asia/Magadan", "Magadan Standard Time" },
            { "Asia/Muscat", "Arabian Standard Time" },
            { "Asia/Novosibirsk", "N. Central Asia Standard Time" },
            { "Asia/Oral", "West Asia Standard Time" },
            { "Asia/Rangoon", "Myanmar Standard Time" },
            { "Asia/Riyadh", "Arab Standard Time" },
            { "Asia/Seoul", "Korea Standard Time" },
            { "Asia/Shanghai", "China Standard Time" },
            { "Asia/Singapore", "Singapore Standard Time" },
            { "Asia/Taipei", "Taipei Standard Time" },
            { "Asia/Tashkent", "West Asia Standard Time" },
            { "Asia/Tbilisi", "Georgian Standard Time" },
            { "Asia/Tehran", "Iran Standard Time" },
            { "Asia/Tokyo", "Tokyo Standard Time" },
            { "Asia/Ulaanbaatar", "Ulaanbaatar Standard Time" },
            { "Asia/Vladivostok", "Vladivostok Standard Time" },
            { "Asia/Yakutsk", "Yakutsk Standard Time" },
            { "Asia/Yekaterinburg", "Ekaterinburg Standard Time" },
            { "Asia/Yerevan", "Armenian Standard Time" },
            { "Atlantic/Azores", "Azores Standard Time" },
            { "Atlantic/Cape_Verde", "Cape Verde Standard Time" },
            { "Atlantic/Reykjavik", "Greenwich Standard Time" },
            { "Australia/Adelaide", "Cen. Australia Standard Time" },
            { "Australia/Brisbane", "E. Australia Standard Time" },
            { "Australia/Darwin", "AUS Central Standard Time" },
            { "Australia/Hobart", "Tasmania Standard Time" },
            { "Australia/Perth", "W. Australia Standard Time" },
            { "Australia/Sydney", "AUS Eastern Standard Time" },
            { "Etc/GMT", "UTC" },
            { "Etc/GMT+11", "UTC-11" },
            { "Etc/GMT+12", "Dateline Standard Time" },
            { "Etc/GMT+2", "UTC-02" },
            { "Etc/GMT-12", "UTC+12" },
            { "Europe/Amsterdam", "W. Europe Standard Time" },
            { "Europe/Athens", "GTB Standard Time" },
            { "Europe/Belgrade", "Central Europe Standard Time" },
            { "Europe/Berlin", "W. Europe Standard Time" },
            { "Europe/Brussels", "Romance Standard Time" },
            { "Europe/Budapest", "Central Europe Standard Time" },
            { "Europe/Dublin", "GMT Standard Time" },
            { "Europe/Helsinki", "FLE Standard Time" },
            { "Europe/Istanbul", "GTB Standard Time" },
            { "Europe/Kiev", "FLE Standard Time" },
            { "Europe/London", "GMT Standard Time" },
            { "Europe/Minsk", "E. Europe Standard Time" },
            { "Europe/Moscow", "Russian Standard Time" },
            { "Europe/Paris", "Romance Standard Time" },
            { "Europe/Sarajevo", "Central European Standard Time" },
            { "Europe/Warsaw", "Central European Standard Time" },
            { "Indian/Mauritius", "Mauritius Standard Time" },
            { "Pacific/Apia", "Samoa Standard Time" },
            { "Pacific/Auckland", "New Zealand Standard Time" },
            { "Pacific/Fiji", "Fiji Standard Time" },
            { "Pacific/Guadalcanal", "Central Pacific Standard Time" },
            { "Pacific/Guam", "West Pacific Standard Time" },
            { "Pacific/Honolulu", "Hawaiian Standard Time" },
            { "Pacific/Pago_Pago", "UTC-11" },
            { "Pacific/Port_Moresby", "West Pacific Standard Time" },
            { "Pacific/Tongatapu", "Tonga Standard Time" }
        };

        var timeInfoKey = string.Empty;
        if (olsonWindowsTimes.ContainsValue(timeInfo))
            timeInfoKey = olsonWindowsTimes.FirstOrDefault(x => x.Value == timeInfo).Key;

        return timeInfoKey;
    }

    public static long GetMuteStep(this long step)
    {
        var muteResult = step switch
        {
            1 => 10,
            2 => 30,
            3 => 8 * 60,
            4 => 365 * 24 * 60,
            _ => -1
        };

        return muteResult;
    }

    public static TimeSpan YearSpan(int year)
    {
        var now = DateTime.UtcNow;
        var span = now.AddYears(year) - now;

        return span;
    }

    public static DateTime ToDateTime(
        this TimeSpan span,
        bool useUtc = false
    )
    {
        var today = DateTime.Now;
        if (useUtc) today = DateTime.UtcNow;

        var answer = today.Add(span);

        return answer;
    }

    public static string ToDetailDateTimeString(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss zz");
    }

    public static TimeSpan ToTimeSpan(this string timeStr)
    {
        try
        {
            var timeSpan = TimeSpanParser.Parse(timeStr);
            return timeSpan;
        }
        catch (Exception)
        {
            throw new TimeSpanInvalidException("Invalid time span. Value: " + timeStr);
            return TimeSpan.Zero;
        }
    }
}