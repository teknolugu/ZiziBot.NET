using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Entities;
using MySqlConnector;
using RepoDb;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Entities.MongoDb.Internal;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.DbMigrations.MongoDBEntities;

[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase")]
public class _005_Word_Migrate_From_MySql : IMigration
{

    public async Task UpgradeAsync()
    {
        var injection = InjectionUtil.GetRequiredService<IOptions<ConnectionStrings>>().Value;

        var mysqlConnectionString = injection.MySql;
        var connection = new MySqlConnection(mysqlConnectionString);
        var wordFilters = await connection.QueryAllAsync<WordFilter>();

        var wordFilterEntities = wordFilters.Select(item => new WordFilterEntity()
        {
            ChatId = item.ChatId,
            UserId = item.FromId,
            Word = item.Word,
            IsGlobal = item.IsGlobal
        }).ToList();

        if (wordFilterEntities.Count <= 0) return;

        await wordFilterEntities.InsertAsync();
    }
}