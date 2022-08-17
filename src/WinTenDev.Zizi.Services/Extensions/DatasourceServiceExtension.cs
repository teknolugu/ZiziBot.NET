using Microsoft.AspNetCore.Builder;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using Nito.AsyncEx.Synchronous;

namespace WinTenDev.Zizi.Services.Extensions;

public static class DatasourceServiceExtension
{
    public static IApplicationBuilder RunMongoDbPreparation(this IApplicationBuilder app)
    {
        var databaseService = app.GetRequiredService<DatabaseService>();

        ConventionRegistry.Register("DefaultConventions", new ConventionPack
        {
            new EnumRepresentationConvention(BsonType.String),
        }, type => true);

        databaseService.MongoDbDatabaseMapping().WaitAndUnwrapException();
        databaseService.MongoDbEnsureCollectionIndex().WaitAndUnwrapException();

        return app;
    }
}