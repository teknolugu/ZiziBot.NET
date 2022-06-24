using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoreLinq;
using Serilog;
using SerilogTimings;
using SqlKata.Execution;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Internals;

public class WordFilterService
{
    private readonly CacheService _cacheService;
    private readonly QueryService _queryService;
    private const string TableName = "word_filter";
    private const string CacheKey = "word-filter";

    public WordFilterService(
        CacheService cacheService,
        QueryService queryService
    )
    {
        _cacheService = cacheService;
        _queryService = queryService;
    }

    public async Task<bool> IsExistAsync(Dictionary<string, object> where)
    {
        var check = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .Where(where)
            .GetAsync();

        var isExist = check.Any();

        Log.Debug("Group setting IsExist: {IsExist}", isExist);

        return isExist;
    }

    public async Task<bool> SaveWordAsync(WordFilter wordFilter)
    {
        Log.Debug("Saving Word to Database");

        var insert = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .InsertAsync(wordFilter);

        return insert > 0;
    }

    public async Task<IEnumerable<WordFilter>> GetWordsListCore()
    {
        Log.Debug("Getting Words from Database");

        var wordFilters = await _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .GetAsync<WordFilter>();

        return wordFilters;
    }

    public async Task<IEnumerable<WordFilter>> GetWordsList()
    {
        var data = await _cacheService.GetOrSetAsync(
            cacheKey: CacheKey,
            action: async () => {
                var data = await GetWordsListCore();

                return data;
            }
        );

        return data;
    }

    public async Task<int> DeleteKata(WordFilter word)
    {
        var query = _queryService
            .CreateMySqlFactory()
            .FromTable(TableName)
            .Where("word", word.Word);

        if (word.ChatId != 0)
            query.Where("chat_id", word.ChatId);

        var delete = await query.DeleteAsync();

        return delete;
    }

    public async Task UpdateWordListsCache()
    {
        await _cacheService.EvictAsync(CacheKey);

        Log.Debug("Update Wordlist Cache..");
        await GetWordsList();
    }

    public async Task<TelegramResult> IsMustDelete(string words)
    {
        var op = Operation.Begin("Check Message");

        var isShould = false;
        var telegramResult = new TelegramResult();

        if (words == null)
        {
            Log.Information("Scan message skipped because Words is null");
            return telegramResult;
        }

        var listWords = await GetWordsList();

        var partedWord = words.Split(
                new[] { '\n', '\r', ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries
            )
            .Distinct()
            .ToList();

        var skipWords = new[]
        {
            "ping",
            "telegram"
        };

        skipWords.ForEach
        (
            (word1) => {
                partedWord.RemoveAll
                (
                    word2 =>
                        word2.Length <= 2 ||
                        word2.CleanExceptAlphaNumeric().ToLowerCase() == word1
                );
            }
        );

        Log.Debug("Message Word Scan Lists: {V}", partedWord);

        foreach (var word in partedWord)
        {
            var forCompare = word;
            forCompare = forCompare.ToLowerCase().CleanExceptAlphaNumeric();

            foreach (var wordFilter in listWords)
            {
                var isGlobal = wordFilter.IsGlobal;

                var forFilter = wordFilter.Word.ToLowerCase();

                if (forFilter.EndsWith("*", StringComparison.CurrentCultureIgnoreCase))
                {
                    forFilter = forFilter.CleanExceptAlphaNumeric();
                    isShould = forCompare.Contains(forFilter);

                    Log.Verbose(
                        "Message compare '{ForCompare}' LIKE '{ForFilter}' ? {IsShould}. Global: {IsGlobal}",
                        forCompare,
                        forFilter,
                        isShould,
                        isGlobal
                    );
                }
                else
                {
                    forFilter = wordFilter.Word.ToLowerCase().CleanExceptAlphaNumeric();
                    if (forCompare == forFilter) isShould = true;

                    Log.Verbose(
                        "Message compare '{ForCompare}' == '{ForFilter}' ? {IsShould}, Global: {IsGlobal}",
                        forCompare,
                        forFilter,
                        isShould,
                        isGlobal
                    );
                }

                if (!isShould) continue;

                var htmlMessage = HtmlMessage.Empty
                    .Bold("Filter: ").CodeBr(wordFilter.Word)
                    .Bold("Comparer: ").CodeBr(forCompare)
                    .Bold("Kata: ").CodeBr(word);

                telegramResult.Notes = htmlMessage.ToString();
                telegramResult.IsSuccess = true;
                Log.Debug("Should break L2 loop!");
                break;
            }

            if (!isShould) continue;
            Log.Debug("Should break L1 Loop!");
            break;
        }

        op.Complete();

        return telegramResult;
    }
}
