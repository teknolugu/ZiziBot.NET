using System.Threading.Tasks;
using FluentAssertions;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace WinTenDev.Zizi.Tests.Units.Common;

public class NpmCLiTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public NpmCLiTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        testOutputHelper.UseSerilog();
    }

    [Fact]
    public void EnsureInstalled()
    {
        NpmCliProvider.EnsureNpm();
    }

    [Fact]
    public async Task RunQuickType()
    {
        Log.Information("Running quicktype");

        var packageName = "quicktype";
        var npmCLi = NpmCliProvider.GetInstance();
        npmCLi.InstallIfMissing(packageName);

        var quickTypeVersion = npmCLi.GetInstalledVersion(packageName);
        Log.Debug("quicktype version: {Version}", quickTypeVersion);

        var cmdResult = await npmCLi.ExecuteAsync(@"exec quicktype --src Storage\Data\user.json");
        Log.Debug("quicktype result: {@Result}", cmdResult);

        cmdResult.HasError.Should().BeFalse();
    }
}