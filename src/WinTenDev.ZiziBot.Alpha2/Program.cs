using Microsoft.AspNetCore.Builder;
using Serilog;
using WinTenDev.Zizi.DbMigrations.Extensions;
using WinTenDev.Zizi.Utils.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.MappingAppSettings();

builder.Host.ConfigureAppConfiguration(
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

builder.Services.AddCommonService();

builder.Services.AddTelegramBot();
builder.Services.AddWtTelegramApi();

builder.Services.AddLiteDb();
builder.Services.AddRepoDb();
builder.Services.AddSqlKataMysql();

builder.Services.AddCacheTower();
builder.Services.AddEasyCachingDisk();

builder.Services.AddHangfireServerAndConfig();

var app = builder.Build();

app.ConfigureLibrary();

app.UseServiceInjection();

app.UseHangfireDashboardAndServer();

app.MapGet("/", () => "Hello World!");

app.Run();
