using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using RepoDb;
using WinTenDev.Zizi.Models.RepoDb;
using WinTenDev.Zizi.Models.Tables;

namespace WinTenDev.Zizi.Services.Internals;

public class BotUpdateService
{
    private readonly ILogger<BotUpdateService> _logger;
    private readonly QueryService _queryService;

    public MySqlConnection DbConnection => _queryService.CreateMysqlConnectionCore();

    public BotUpdateService(
        ILogger<BotUpdateService> logger,
        QueryService queryService
    )
    {
        _logger = logger;
        _queryService = queryService;
    }

    public async Task SaveUpdateAsync(BotUpdate botUpdate)
    {
        await DbConnection.InsertAsync(botUpdate, trace: new DefaultTraceLog());
    }

    public async Task<IEnumerable<BotUpdate>> GetUpdateAsync()
    {
        return await DbConnection.QueryAllAsync<BotUpdate>(trace: new DefaultTraceLog());
    }

    public async Task<List<BotUpdate>> GetUpdateAsync(
        long chatId,
        long userId
    )
    {
        var botUpdates = await DbConnection.QueryAsync<BotUpdate>(
            where: update =>
                update.ChatId == chatId &&
                update.UserId == userId,
            trace: new DefaultTraceLog()
        );

        return botUpdates.ToList();
    }
}
