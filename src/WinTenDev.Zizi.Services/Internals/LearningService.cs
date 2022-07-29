using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using SqlKata.Execution;

namespace WinTenDev.Zizi.Services.Internals;

public class LearningService
{
    private readonly QueryService _queryService;
    private const string TableName = "words_learning";

    public LearningService(QueryService queryService)
    {
        _queryService = queryService;
    }

    public bool IsExist(LearnData learnData)
    {
        var select = _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .Where("message", learnData.Message)
            .Get();

        return select.Any();
    }

    public IEnumerable<dynamic> GetAll(LearnData learnData)
    {
        var select = _queryService
            .CreateMySqlFactory()
            .FromTable(TableName).Get();

        return select;
    }

    public async Task<int> Save(LearnData learnData)
    {
        var insert = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .InsertAsync(
                new Dictionary<string, object>()
                {
                    { "label", learnData.Label },
                    { "message", learnData.Message },
                    { "from_id", learnData.FromId },
                    { "chat_id", learnData.ChatId }
                }
            );

        Log.Information("Save Learn: {Insert}", insert);

        return insert;
    }
}