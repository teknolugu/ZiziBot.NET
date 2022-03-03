using System.Collections.Generic;
using System.Threading.Tasks;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Internals;

public class RulesService
{
    private readonly QueryService _queryService;
    private readonly CacheService _cacheService;

    public RulesService(
        QueryService queryService,
        CacheService cacheService
    )
    {
        _queryService = queryService;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<Rule>> GetRulesAsync(
        long chatId,
        bool disableCache = false
    )
    {
        var rules = await _cacheService.GetOrSetAsync(
            cacheKey: "rules_" + chatId.ReduceChatId(),
            disableCache: disableCache,
            action: () => {
                var rules = _queryService.GetAsync<Rule>(
                    new RuleFindDto()
                    {
                        ChatId = chatId
                    }
                );

                return rules;
            }
        );

        return rules;
    }

    public async Task<int> SaveRuleAsync(Rule values)
    {
        var insert = await _queryService.InsertAsync(values);

        return insert;
    }
}