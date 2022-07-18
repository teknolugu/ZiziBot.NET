using System;
using System.Threading.Tasks;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceMemberWarnExtension
{
    public static async Task WarnMemberAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;
        var reasonWarn = telegramService.GetCommandParam();

        var replyToMessage = telegramService.ReplyToMessage;

        if (!await telegramService.CheckFromAdminOrAnonymous())
        {
            await telegramService.SendWarnMessageAsync(
                new WarnMember
                {
                    ChatId = chatId,
                    MemberUserId = fromId,
                    MemberFirstName = telegramService.From.FirstName,
                    MemberLastName = telegramService.From.LastName,
                    AdminUserId = fromId,
                    AdminFirstName = telegramService.From.FirstName,
                    AdminLastName = telegramService.From.LastName,
                    Reason = "Self-warn",
                }
            );

            return;
        }

        if (replyToMessage == null)
        {
            await telegramService.SendTextMessageAsync(
                sendText: "Balas seseorang yang ingin di Warn",
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                includeSenderMessage: true
            );

            return;
        }

        await telegramService.SendWarnMessageAsync(
            new WarnMember
            {
                ChatId = chatId,
                MemberUserId = replyToMessage.From!.Id,
                MemberFirstName = replyToMessage.From.FirstName,
                MemberLastName = replyToMessage.From.LastName,
                AdminUserId = fromId,
                AdminFirstName = telegramService.From.FirstName,
                AdminLastName = telegramService.From.LastName,
                Reason = reasonWarn ?? "Self-warn",
            }
        );
    }

    private static async Task SendWarnMessageAsync(
        this TelegramService telegramService,
        WarnMember warnMember,
        string note = null
    )
    {
        int warnLimit = 4;
        var chatId = warnMember.ChatId;
        var warnUserId = warnMember.MemberUserId;

        var warnMemberService = telegramService.GetRequiredService<WarnMemberService>();

        var htmlMessage = HtmlMessage.Empty
            .Bold("⚠️ Warn Member").Br()
            .Bold("User Id: ").CodeBr(warnMember.MemberUserId.ToString())
            .Bold("Name: ").User(warnMember.MemberUserId, (warnMember.MemberFirstName + " " + warnMember.MemberLastName).Trim()).Br();

        if (note.IsNotNullOrEmpty()) htmlMessage.Br().TextBr(note);

        var beforeWarn = await warnMemberService.GetLatestWarn(chatId, warnUserId);

        if (beforeWarn.Count >= warnLimit)
        {
            await warnMemberService.DeleteWarns(chatId, warnUserId);

            htmlMessage.TextBr($"Ditendang karena telah melewati batas warn.").Br()
                .TextBr("⏳ Riwayat");

            beforeWarn.ForEach(
                member => {
                    htmlMessage.Text("- ");
                    htmlMessage.Bold(member.CreatedOn.ToDetailDateTimeString()).Text(" - ");
                    htmlMessage.Text(member.Reason.IsNullOrEmpty() ? "N/A" : member.Reason);
                    htmlMessage.Br();
                }
            );

            await telegramService.KickMemberAsync(warnUserId, true);

            await telegramService.SendTextMessageAsync(
                sendText: htmlMessage.ToString(),
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                includeSenderMessage: true
            );

            return;
        }

        await warnMemberService.SaveWarnAsync(warnMember);

        var latestWarn = await warnMemberService.GetLatestWarn(warnMember.ChatId, warnMember.MemberUserId);

        htmlMessage.Br().TextBr("⏳ Riwayat");

        latestWarn.ForEach(
            member => {
                htmlMessage.Text("- ");
                htmlMessage.Bold(member.CreatedOn.ToDetailDateTimeString()).Text(" - ");
                htmlMessage.Text(member.Reason.IsNullOrEmpty() ? "N/A" : member.Reason);
                htmlMessage.Br();
            }
        );

        await telegramService.SendTextMessageAsync(
            sendText: htmlMessage.ToString(),
            scheduleDeleteAt: DateTime.UtcNow.AddHours(1),
            includeSenderMessage: true
        );
    }
}