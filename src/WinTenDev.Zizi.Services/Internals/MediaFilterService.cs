using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using SqlKata.Execution;

namespace WinTenDev.Zizi.Services.Internals;

public class MediaFilterService
{
    private readonly string baseTable = "media_filters";
    private readonly string fileJson = "media_filter.json";
    private readonly QueryService _queryService;

    public MediaFilterService(QueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<bool> IsExist(
        string key,
        string value
    )
    {
        var query = await _queryService
            .CreateMySqlFactory()
            .FromTable(baseTable)
            .Where(key, value)
            .GetAsync();

        return query.Any();
    }

    public async Task<bool> IsExistInCache(
        string key,
        string val
    )
    {
        var data = await ReadCacheAsync();
        var search = data.AsEnumerable()
            .Where(row => row.Field<string>(key) == val);
        if (!search.Any()) return false;

        var filtered = search.CopyToDataTable();
        Log.Information("Media found in Caches: {V}", filtered.ToJson());
        return true;
    }

    public async Task SaveAsync(Dictionary<string, object> data)
    {
        Log.Information("Data : {Json}", data.ToJson(true));

        var insert = await _queryService
            .CreateMySqlFactory()
            .FromTable(baseTable)
            .InsertAsync(data);

        Log.Information("SaveFile: {Insert}", insert);
    }

    public async Task<DataTable> GetAllMedia()
    {
        var query = await _queryService
            .CreateMySqlFactory()
            .FromTable(baseTable)
            .GetAsync();

        var data = query.ToJson().MapObject<DataTable>();
        return data;
    }

    public async Task UpdateCacheAsync()
    {
        var data = await GetAllMedia();
        Log.Information("Updating Media Filter caches to {FileJson}", fileJson);

        await data.WriteCacheAsync(fileJson);
    }

    public async Task<DataTable> ReadCacheAsync()
    {
        var dataTable = await fileJson.ReadCacheAsync<DataTable>();
        return dataTable;
    }
}