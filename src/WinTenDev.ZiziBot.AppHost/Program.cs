using DotNurse.Injector;
using Exceptionless;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
var builder = WebApplication.CreateBuilder(args);

builder.Services.MappingAppSettings();

builder.Configuration
    .AddJsonFile(
        path: "appsettings.json",
        optional: true,
        reloadOnChange: true
    )
    .AddAppSettingsJson();

builder
    .Host
    .UseSerilog(
        (
            context,
            provider,
            logger
        ) => logger.AddSerilogBootstrapper(provider)
    );

builder.Logging.AddSerilog();

builder.Services.MappingAppSettings();
builder.Services.ConfigureAutoMapper();

builder.Services.AddFluentMigration();

builder.Services.AddHealthChecks();

builder.Services.AddSentry();
builder.Services.AddExceptionless();
builder.Services.AddHttpContextAccessor();

builder.Services.AddServicesByAttributes();
builder.Services.AddCommonService();
builder.Services.AddHostedServices();

builder.Services.AddTelegramBot();
builder.Services.AddWtTelegramApi();
builder.Services.AddCommandHandlers();

builder.Services.AddLiteDb();
builder.Services.AddClickHouse();
builder.Services.AddRepoDb();
builder.Services.AddSqlKataMysql();
// builder.Services.AddRedisOm();

builder.Services.AddCacheTower();

builder.Services.AddLocalTunnelClient();

builder.Services.AddHangfireServerAndConfig();

var app = builder.Build();

await app.RunPreStartupTasksAsync();

app.MapGet("/", () => "Hello World!");

app.UseServiceInjection();
app.PrintAboutApp();
app.LoadJsonLocalization();

app.UseFluentMigration();
app.ConfigureNewtonsoftJson();
app.ConfigureDapper();

await app.ExecuteStartupTasks();

// if (env.IsDevelopment())
// app.UseDeveloperExceptionPage();

app.UseRouting();
app.UseStaticFiles();

app.UseSerilogRequestLogging();
app.UseSentryTracing();
app.UseExceptionless();

app.RunTelegramBot();

app.UseHangfireDashboardAndServer();

app.RegisterHangfireJobs();

app.UseEndpoints(
    endpoints => {
        endpoints.MapHealthChecks("/health");
    }
);

await app.RunStartupTasksAsync();

await app.RunAsync();