using Microsoft.AspNetCore.Builder;
using Nito.AsyncEx.Synchronous;

namespace WinTenDev.Zizi.Services.Extensions;

public static class DatasourceServiceExtension
{
    public static IApplicationBuilder RunMongoDbPreparation(this IApplicationBuilder app)
    {
        var databaseService = app.GetRequiredService<DatabaseService>();

        databaseService.MongoDbDatabaseMapping().WaitAndUnwrapException();
        databaseService.MongoDbEnsureCollectionIndex().WaitAndUnwrapException();

        return app;
    }
}