using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using SerilogTimings;
using SqlKata.Execution;

namespace WinTenDev.Zizi.Services.Internals;

public class BlockListService
{
    private const string TableName = "blocklist";
    private readonly CacheService _cacheService;
    private readonly QueryService _queryService;

    public BlockListService(
        CacheService cacheService,
        QueryService queryService
    )
    {
        _cacheService = cacheService;
        _queryService = queryService;
    }

    public async Task SaveBlockList(BlockList data)
    {
        await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .InsertAsync(data);
    }

    public async Task<BlockListData> ParseList(string url)
    {
        var op = Operation.Begin("Read BlockList");

        var data = await _cacheService.GetOrSetAsync(
            url,
            async () => {
                var allUrl = await url.GetStringAsync();
                return allUrl;
            }
        );

        var parseList = ParseListCore(data);

        op.Complete();

        return parseList;
    }

    public BlockListData ParseListCore(string rawUrl)
    {
        var lines = rawUrl.Split(Environment.NewLine);

        if (lines.Length == 0) return null;

        var data = new BlockListData();
        var metaData = new Dictionary<string, object>();

        foreach (var line in lines)
        {
            if (!line.Contains("#")) continue;

            var split = line.Split(": ");
            var key = split[0]
                .Replace("#", "")
                .Trim()
                .Replace(" ", "")
                .Trim();

            metaData.Add(key, split[1].Trim());
        }

        var name = lines[0];
        var source = lines[1];
        var lastUpdate = lines[2];

        var listUrl = lines.Skip(3).Where(s => !s.Trim().IsNullOrEmpty()).ToList();

        data.Name = name;
        data.Source = source;
        data.LastUpdate = lastUpdate;
        data.MetadataDic = metaData;
        data.MetaData = metaData.DictionaryMapper<BlockListMetaData>();
        data.ListDomain = listUrl;
        data.DomainCount = listUrl.Count;

        return data;
    }
}