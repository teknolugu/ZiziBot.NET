using System;
using FluentAssertions;
using WinTenDev.Zizi.Utils;
using Xunit;

namespace WinTenDev.Zizi.Tests;

public class TimeUtilTest
{
    [Fact]
    public void ConvertTimeZoneTest()
    {
        var currentDateUtc = DateTime.UtcNow;

        var tzAsia = currentDateUtc.ConvertUtcTimeToTimeZone("SE Asia Standard Time").Should();
        var tzPacific = currentDateUtc.ConvertUtcTimeToTimeZone("Pacific Standard Time");
    }
}