using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using MongoDB.Entities;
using MySqlConnector;
using RepoDb;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Entities.MongoDb.Internal;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.DbMigrations.MongoDBEntities;

[UsedImplicitly]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase")]
public class _003_GlobalBan_Migrate_From_MySql : IMigration
{
    public async Task UpgradeAsync()
    {
        var injection = InjectionUtil.GetRequiredService<IOptions<ConnectionStrings>>().Value;
        var mysqlConnectionString = injection.MySql;

        var connection = new MySqlConnection(mysqlConnectionString);

        var globalBans = await connection.QueryAllAsync<GlobalBanItem>();

        var globalBanUserEntities = globalBans.Select(item => new GlobalBanUserEntity()
        {
            ChatId = item.ChatId,
            UserId = item.UserId,
            Reason = item.ReasonBan,
            BannedUserId = item.BannedUserId
        }).ToList();

        if (globalBanUserEntities.Count <= 0) return;

        await globalBanUserEntities.InsertAsync();
    }
}