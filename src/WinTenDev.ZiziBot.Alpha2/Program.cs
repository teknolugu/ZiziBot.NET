using Microsoft.AspNetCore.Builder;
using WinTenDev.Zizi.Utils.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.MappingAppSettings();

builder.Services.AddTelegramBot();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();