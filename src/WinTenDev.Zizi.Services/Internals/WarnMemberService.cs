using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Services.Internals;

public class WarnMemberService
{
    private readonly ILogger<WarnMemberService> _logger;

    public WarnMemberService(ILogger<WarnMemberService> logger)
    {
        _logger = logger;
    }

    public async Task<List<WarnMemberEntity>> GetLatestWarn(
        long chatId,
        long userId
    )
    {
        var latestWarn = await DB.Find<WarnMemberEntity>()
            .ManyAsync(
                member =>
                    member.ChatId == chatId &&
                    member.MemberUserId == userId
            );

        return latestWarn;
    }

    public async Task SaveWarnAsync(WarnMemberEntity warnMember)
    {
        await DB.SaveAsync(warnMember);
    }

    public async Task<DeleteResult> DeleteWarns(
        long chatId,
        long userId
    )
    {
        var delete = await DB.DeleteAsync<WarnMemberEntity>(
            member =>
                member.ChatId == chatId &&
                member.MemberUserId == userId
        );

        _logger.LogDebug("Deleted {@Delete} warns", delete);

        return delete;
    }
}