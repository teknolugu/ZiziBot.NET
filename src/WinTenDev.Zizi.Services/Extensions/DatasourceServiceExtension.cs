using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;

namespace WinTenDev.Zizi.Services.Extensions;

public static class DatasourceServiceExtension
{
    public static async Task<IApplicationBuilder> RunMongoDbPreparation(this IApplicationBuilder app)
    {
        var databaseService = app.GetRequiredService<DatabaseService>();

        ConventionRegistry.Register("DefaultConventions", new ConventionPack
        {
            new EnumRepresentationConvention(BsonType.String),
        }, type => true);

        await databaseService.MongoDbDatabaseMapping();
        await databaseService.MongoDbMigration();
        await databaseService.MongoDbEnsureCollectionIndex();

        return app;
    }
}