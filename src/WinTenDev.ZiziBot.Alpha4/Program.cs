using Allowed.Telegram.Bot.Extensions;
using Allowed.Telegram.Bot.Models;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTelegramClients(builder.Configuration.GetSection("Telegram:Bots").Get<BotData[]>());


if (builder.Environment.IsDevelopment())
    builder.Services.AddTelegramManager();
else
    builder.Services.AddTelegramWebHookManager();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();