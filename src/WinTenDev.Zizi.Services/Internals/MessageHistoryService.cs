using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlKata.Execution;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Internals;

public class MessageHistoryService
{
    private const string TableName = "message_history";
    private readonly ILogger<MessageHistoryService> _logger;
    private readonly QueryService _queryService;

    public MessageHistoryService(
        ILogger<MessageHistoryService> logger,
        QueryService queryService
    )
    {
        _logger = logger;
        _queryService = queryService;
    }

    public async Task<IEnumerable<MessageHistory>> GetMessageHistoryAsync(MessageHistoryFindDto findDto)
    {
        var where = findDto.ToDictionary();

        var query = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .Where(where)
            .GetAsync<MessageHistory>();

        return query;
    }

    public async Task<int> SaveToMessageHistoryAsync(MessageHistoryInsertDto messageHistory)
    {
        var values = messageHistory.ToDictionary();

        var query = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .InsertAsync(values);

        return query;
    }

    public async Task<int> UpdateDeleteAtAsync(MessageHistoryFindDto findDto, DateTime dateTime)
    {
        var where = findDto.ToDictionary();

        var query = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .Where(where)
            .UpdateAsync(new { delete_at = dateTime });

        return query;
    }

    public async Task<int> DeleteMessageHistoryAsync(MessageHistoryFindDto findDto)
    {
        var where = findDto.ToDictionary();

        var query = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .Where(where)
            .DeleteAsync();

        return query;
    }
}