using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Group;

public class BanCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public BanCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        await _telegramService.DeleteSenderMessageAsync();

        if (!await _telegramService.CheckFromAdminOrAnonymous()) return;

        var kickTargets = new List<User>();

        var msg = _telegramService.Message;
        var fromId = _telegramService.FromId;

        kickTargets.Add(msg.From);

        if (msg.ReplyToMessage != null)
        {
            kickTargets.Clear();

            var repMsg = msg.ReplyToMessage;
            kickTargets.Add(repMsg.From);

            if (repMsg.NewChatMembers != null)
            {
                kickTargets.AddRange(repMsg.NewChatMembers);
            }
        }

        var containFromId = kickTargets.Where
        (
            (
                user,
                i
            ) => user.Id == fromId
        ).Any();

        if (!containFromId)
        {
            Log.Warning("No privilege for execute this command!");
            return;
        }

        var sbKickResult = new StringBuilder($"Sedang memblokir {kickTargets.Count} pengguna..");
        sbKickResult.AppendLine();

        foreach (var userId in kickTargets.Select(kickTarget => kickTarget.Id))
        {
            var isKicked = await _telegramService.KickMemberAsync(userId, true);

            if (isKicked)
            {
                sbKickResult.Append($"{userId} berhasil di blokir ");
                sbKickResult.AppendLine(userId == fromId ? $"oleh Self-ban" : "");
            }
            else
            {
                sbKickResult.AppendLine($"Gagal memblokir {userId}");
            }
        }

        await _telegramService.AppendTextAsync(sbKickResult.ToTrimmedString());
    }
}
