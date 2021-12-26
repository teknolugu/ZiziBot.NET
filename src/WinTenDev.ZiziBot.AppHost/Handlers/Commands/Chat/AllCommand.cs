using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using TL;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Chat;

public class AllCommand : CommandBase
{
    private readonly WTelegramApiService _wTelegramApiService;
    private readonly TelegramService _telegramService;

    public AllCommand(
        WTelegramApiService wTelegramApiService,
        TelegramService telegramService
    )
    {
        _wTelegramApiService = wTelegramApiService;
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var channelId = _telegramService.ReducedChatId;

        var allMembers = await _wTelegramApiService.GetAllParticipants(channelId);

        var allMemberStr = allMembers.users.Select((participant, i) => {
            var user = (User) participant.Value;

            return user.id.GetMention();
        }).JoinStr("");

        await _telegramService.SendTextMessageAsync($"Hai {allMembers.participants.Count()} orang");

        // allMemberStr.SplitInParts(4000).ForEach(async s => { await _telegramService.SendTextMessageAsync(s); });
    }
}