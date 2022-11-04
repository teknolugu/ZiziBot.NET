using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace WinTenDev.Zizi.Services.Extensions;

public static class BotCoreExtension
{
    public static async Task<IApplicationBuilder> ExecuteStartupTasks(this IApplicationBuilder app)
    {
        var config = app.GetRequiredService<IConfiguration>();
        var botService = app.GetRequiredService<BotService>();

        // app.GetRequiredService<CacheService>().InvalidateJsonCache();

        await app.GetRequiredService<DatabaseService>().FixTableCollation();

        await botService.EnsureCommandRegistration();
        // await botService.SendStartupNotification();

        await app.RunMongoDbPreparation();

        ChangeToken.OnChange(
            changeTokenProducer: () => config.GetReloadToken(),
            changeTokenConsumer: async () => await botService.EnsureCommandRegistration()
        );

        return app;
    }
}