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
public class _006_Step_Migrate_From_MySql : IMigration
{
    public async Task UpgradeAsync()
    {
        var injection = InjectionUtil.GetRequiredService<IOptions<ConnectionStrings>>().Value;

        var mysqlConnectionString = injection.MySql;
        var connection = new MySqlConnection(mysqlConnectionString);
        var stepHistories = await connection.QueryAllAsync<StepHistory>();

        var stepHistoryEntities = stepHistories.Select(item => new StepHistoryEntity()
        {
            ChatId = item.ChatId,
            UserId = item.UserId,
            Name = item.Name,
            Status = item.Status,
            Reason = item.Reason,
            FirstName = item.FirstName,
            LastName = item.LastName,
            HangfireJobId = item.HangfireJobId,
            WarnMessageId = item.LastWarnMessageId
        });

        await stepHistoryEntities.InsertAsync();
    }
}