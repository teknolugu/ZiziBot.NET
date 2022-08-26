using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Services.Internals;

public class ArticleSentService
{
    private readonly ILogger<ArticleSentService> _logger;
    private readonly IMapper _mapper;

    public ArticleSentService(
        ILogger<ArticleSentService> logger,
        IMapper mapper
    )
    {
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<bool> IsSentAsync(
        long chatId,
        string url
    )
    {
        var isAny = await DB.Find<ArticleSent>()
            .Match(sent =>
                sent.ChatId == chatId &&
                sent.Url == url
            )
            .ExecuteAnyAsync();

        _logger.LogDebug("Article for ChatId: {ChatId} with Url: {Url} IsSent? {IsSent}",
            chatId,
            url,
            isAny
        );

        return isAny;
    }

    public async Task SaveAsync(ArticleSentDto articleSentDto)
    {
        var articleSent = _mapper.Map<ArticleSent>(articleSentDto);

        await articleSent.InsertAsync();
    }
}