using Microsoft.AspNetCore.Builder;
using Serilog;
using WinTenDev.Zizi.DbMigrations.Extensions;
using WinTenDev.Zizi.Extensions;
using WinTenDev.Zizi.Hangfire;
using WinTenDev.Zizi.Utils.Extensions;
var builder = WebApplication.CreateBuilder(args);

builder.Services.MappingAppSettings();

builder.Host
    .ConfigureAppConfiguration(
        (
            context,
            configurationBuilder
        ) => {
            configurationBuilder.AddAppSettingsJson();
        }
    )
    .UseSerilog(
        (
            context,
            provider,
            logger
        ) => logger.AddSerilogBootstrapper(provider)
    );

builder.Logging.AddSerilog();

builder.Services.AddFluentMigration();
builder.Services.ConfigureAutoMapper();

builder.Services.AddCommonService();

builder.Services.AddTelegramBot();
builder.Services.AddWtTelegramApi();

builder.Services.AddLiteDb();
builder.Services.AddRepoDb();
builder.Services.AddSqlKataMysql();

builder.Services.AddCacheTower();

builder.Services.AddHangfireServerAndConfig();

var app = builder.Build();

app.ConfigureLibrary();

app.UseServiceInjection();

await app.ExecuteStartupTasks();

app.UseHangfireDashboardAndServer();

app.MapGet("/", () => "Hello World!");

await app.RunStartupTasksAsync();

await app.RunAsync();