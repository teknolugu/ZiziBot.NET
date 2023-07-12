using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WinTenDev.Zizi.DbMigrations.EfMigrations;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.DbMigrations.Extensions;

public static class ContextsExtension
{
    public static IServiceCollection AddDbContexts(this IServiceCollection services,
        string connStr)
    {
        // services.AddDbContextPool<BlockListContext>(options =>
        //     options.UseMySql(connStr, ServerVersion.AutoDetect(connStr))
        //         .EnableDetailedErrors()
        //         .EnableSensitiveDataLogging()
        // );


        return services;
    }

    public static IApplicationBuilder UseMigrateEf(this IApplicationBuilder app)
    {
        app.EnsureMigrationOfContext<BlockListContext>();
        return app;
    }
}