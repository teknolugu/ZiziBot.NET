using System;
using FluentAssertions;
using Xunit;

namespace WinTenDev.Zizi.Tests.Units.Text;

public class TimeUtilTest
{
    [Fact]
    public void ConvertTimeZoneTest()
    {
        var currentDateUtc = DateTime.UtcNow;
        var dateUtc = new DateTime(2022, 03, 02, 12, 00, 00);

        var tzAsia = currentDateUtc.ConvertUtcTimeToTimeZone("SE Asia Standard Time").Should();
        var tzPacific = currentDateUtc.ConvertUtcTimeToTimeZone("Pacific Standard Time");

        var tzAsiaDate2 = dateUtc.ConvertUtcTimeToTimeZone("SE Asia Standard Time");
    }

    [Fact]
    public void ConvertTimeZoneOffset()
    {
        var dateUtc = new DateTime(2022, 03, 03, 08, 00, 00);

        var dateTimeOffset1 = dateUtc.ConvertUtcTimeToTimeOffset("+07:00");
    }
}