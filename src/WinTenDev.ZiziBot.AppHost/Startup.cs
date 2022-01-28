using Exceptionless;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using WinTenDev.Zizi.DbMigrations.Extensions;
using WinTenDev.Zizi.Utils.Extensions;
using WinTenDev.ZiziBot.AppHost.Extensions;

namespace WinTenDev.ZiziBot.AppHost;

public class Startup
{
    public Startup(
        IConfiguration configuration,
        IWebHostEnvironment env
    )
    {

    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddZiziBot();
        services.MappingAppSettings();
        services.AddMappingConfiguration();

        services.AddHealthChecks();

        services.AddSentry();
        services.AddExceptionless();
        services.AddHttpContextAccessor();

        services.AddFluentMigration();

        services.AddEasyCachingSqlite();
        services.AddCacheTower();

        services.AddRepoDb();
        services.AddSqlKataMysql();
        services.AddClickHouse();
        services.AddLiteDb();

        services.AddWtTelegramApi();

        services.AddCommonService();
        services.AddCommandHandlers();

        services.AddLocalTunnelClient();

        services.AddHangfireServerAndConfig();

        Log.Information("Services is ready..");
    }

    public void Configure(
        IApplicationBuilder app,
        IWebHostEnvironment env
    )
    {
        app.AboutApp();

        app.UseFluentMigration();
        app.ConfigureNewtonsoftJson();
        app.ConfigureDapper();

        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        app.UseRouting();
        app.UseStaticFiles();

        app.UseSerilogRequestLogging();
        app.UseSentryTracing();
        app.UseExceptionless();

        // app.UseLocalTunnel("zizibot-dev");

        app.RunZiziBot();

        app.UseHangfireDashboardAndServer();

        app.RegisterHangfireJobs();

        app.Run
        (
            async context =>
                await context.Response.WriteAsync("Hello World!")
        );

        app.UseEndpoints
        (
            endpoints => {
                endpoints.MapHealthChecks("/health");
            }
        );

        Log.Information("App is ready..");
    }
}