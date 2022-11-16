using Exceptionless;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nito.AsyncEx.Synchronous;
using Serilog;

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
        services.MappingAppSettings();
        services.ConfigureAutoMapper();

        services.AddTelegramBot();

        services.AddHealthChecks();

        services.AddSentry();
        services.AddExceptionless();
        services.AddHttpContextAccessor();

        services.AddCacheTower();

        services.AddRepoDb();
        services.AddSqlKataMysql();
        services.AddClickHouse();
        services.AddLiteDb();

        // services.AddFluentMigration();

        services.AddWtTelegramApi();
        services.AddCommonService();
        services.AddHostedServices();
        services.AddCommandHandlers();

        services.AddLocalTunnelClient();

        services.AddHangfireServerAndConfig();
    }

    public void Configure(
        IApplicationBuilder app,
        IWebHostEnvironment env
    )
    {
        app.UseServiceInjection();
        app.PrintAboutApp();
        app.LoadJsonLocalization();

        app.UseFluentMigration();
        app.ConfigureNewtonsoftJson();
        app.ConfigureDapper();
        app.ExecuteStartupTasks().WaitAndUnwrapException();

        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        app.UseRouting();
        app.UseStaticFiles();

        app.UseSerilogRequestLogging();
        app.UseSentryTracing();
        app.UseExceptionless();

        app.RunTelegramBot();

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

        app.RunStartupTasksAsync().WaitAndUnwrapException();

        Log.Information("App is ready..");
    }
}