using AutoWrapper;
using DotNurse.Injector.AspNetCore;
using Serilog;
using WinTenDev.Zizi.Services.Extensions;
using WinTenDev.Zizi.Utils.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host
    .UseDotNurseInjector()
    .UseSerilog(
        (
            context,
            provider,
            logger
        ) => logger.AddSerilogBootstrapper(provider)
    );

builder.Configuration.AddAppSettingsJson();

// Add services to the container.
builder.Services.MappingAppSettings();
builder.Services.AddAutoMapper(
    expression => {
        expression.AddMaps("WinTenDev.Zizi.Models");
    }
);

builder.Services.AddCacheTower();

builder.Services.AddTelegramBotClient();
builder.Services.AddCommonService();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(
        options => {
            options.DocumentTitle = "WinTenDev Zizi API";
        }
    );
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

app.UseAuthorization();
app.RunMongoDbPreparation();

app.UseApiResponseAndExceptionWrapper(
    new AutoWrapperOptions()
    {
        IsDebug = true,
        ShowStatusCode = true
    }
);
app.MapControllers();

app.Run();