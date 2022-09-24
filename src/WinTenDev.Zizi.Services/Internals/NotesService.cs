using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using SqlKata.Execution;

namespace WinTenDev.Zizi.Services.Internals;

public class NotesService
{
    private readonly QueryService _queryService;
    private readonly CacheService _cacheService;
    private const string TableName = "tags";

    public NotesService(
        QueryService queryService,
        CacheService cacheService
    )
    {
        _queryService = queryService;
        _cacheService = cacheService;
    }

    public async Task<bool> IsExistAsync(
        long chatId,
        string slug
    )
    {
        var note = await GetNotesBySlug(chatId, slug);

        var isExist = note.Any();
        return isExist;
    }

    public async Task<IEnumerable<CloudTag>> GetNotesByChatId(long chatId)
    {
        var data = await _cacheService.GetOrSetAsync(
            cacheKey: "chat_notes_" + chatId.ReduceChatId(),
            action: () => {
                var query = _queryService
                    .CreateMySqlFactory()
                    .FromTable(TableName)
                    .Where("chat_id", chatId)
                    .OrderBy("tag")
                    .GetAsync<CloudTag>();

                return query;
            }
        );

        return data;
    }

    public async Task<IEnumerable<CloudTag>> GetNotesBySlug(
        long chatId,
        string slug
    )
    {
        Log.Information("Getting Notes by Slug..");

        var query = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .Where("chat_id", chatId)
            .Where("tag", slug)
            .GetAsync<CloudTag>();

        return query;
    }

    public async Task SaveNote(Dictionary<string, object> data)
    {
        var json = data.ToJson();
        Log.Information("Json: {Json}", json);

        var insert = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .InsertAsync(data);

        Log.Information("SaveNote: {Insert}", insert);
    }

    public async Task<int> SaveNoteAsync(NoteSaveDto noteSaveDto)
    {
        var values = noteSaveDto.ToDictionary();

        var insert = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .InsertAsync(values);

        return insert;
    }

    public async Task<int> DeleteNoteAsync(
        long chatId,
        string slugOrId
    )
    {
        var query = await _queryService.CreateMySqlFactory()
            .FromTable(TableName)
            .Where("chat_id", chatId)
            .Where(
                where1 => where1
                    .Where("tag", slugOrId)
                    .OrWhere("id", slugOrId)
            ).DeleteAsync();

        return query;
    }

    public async Task UpdateCache(long chatId)
    {
        var data = await GetNotesByChatId(chatId);
        await data.WriteCacheAsync($"{chatId}/notes.json");
    }
}