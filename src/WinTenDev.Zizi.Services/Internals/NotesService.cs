using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Serilog;
using SqlKata.Execution;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.Zizi.Services.Internals;

public class NotesService
{
    private readonly QueryService _queryService;
    private readonly CacheService _cacheService;
    private const string TableName = "notes";

    public NotesService(
        QueryService queryService,
        CacheService cacheService
    )
    {
        _queryService = queryService;
        _cacheService = cacheService;
    }

    public async Task<DataTable> GetNotesByChatId(long chatId)
    {
        // var sql = $"SELECT * FROM {tableName} WHERE chat_id = '{chatId}'";
        // var data = await _mySql.ExecQueryAsync(sql);

        var factory = _queryService.CreateMySqlConnection();
        var query = await factory.FromTable(TableName)
            .Where("chat_id", chatId)
            .GetAsync();

        var data = query.ToJson().MapObject<DataTable>();
        return data;
    }

    public async Task<List<CloudNote>> GetNotesBySlug(long chatId, string slug)
    {
        Log.Information("Getting Notes by Slug..");

        var factory = _queryService.CreateMySqlConnection();
        var query = await factory.FromTable(TableName)
            .Where("chat_id", chatId)
            .OrWhereContains("slug", slug)
            .GetAsync();

        var mapped = query.ToJson().MapObject<List<CloudNote>>();
        return mapped;

        // var sql = $"SELECT * FROM {tableName} WHERE chat_id = '{chatId}' " +
        // $"AND MATCH(slug) AGAINST('{slug.SqlEscape()}')";
        // var data = await _mySql.ExecQueryAsync(sql);
        // return data;
    }

    public async Task SaveNote(Dictionary<string, object> data)
    {
        var json = data.ToJson();
        Log.Information("Json: {0}", json);

        var factory = _queryService.CreateMySqlConnection();
        var insert = await factory.FromTable(TableName)
            .InsertAsync(data);

        Log.Information("SaveNote: {Insert}", insert);
    }

    public async Task UpdateCache(long chatId)
    {
        var data = await GetNotesByChatId(chatId);
        await data.WriteCacheAsync($"{chatId}/notes.json");
    }
}