using System;
using System.Threading.Tasks;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceMemberWarnExtension
{
    public static async Task<object> WarnMemberAsync(this TelegramService telegramService)
    {
        int warnLimit = 4;
        var chatId = telegramService.ChatId;
        var fromId = telegramService.FromId;
        var reasonWarn = telegramService.GetCommandParam();

        var message = telegramService.Message;
        var replyToMessage = telegramService.ReplyToMessage;

        if (replyToMessage == null)
        {
            return await telegramService.SendTextMessageAsync("Please reply to a message to warn the member");
        }

        var warnUserId = replyToMessage.From!.Id;
        var warnNameLink = replyToMessage.From.GetNameLink();
        var warnMemberService = telegramService.GetRequiredService<WarnMemberService>();

        var beforeWarn = await warnMemberService.GetLatestWarn(chatId, warnUserId);

        var htmlMessage = HtmlMessage.Empty
            .Bold("⚠️ Warn Member").Br()
            .Bold("User Id: ").CodeBr(warnUserId.ToString())
            .Bold("Name: ").TextBr(warnNameLink);

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

            return telegramService.SendTextMessageAsync(
                sendText: htmlMessage.ToString(),
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                includeSenderMessage: true
            );
        }

        await warnMemberService.SaveWarnAsync(
            new WarnMember
            {
                ChatId = telegramService.ChatId,
                Reason = reasonWarn,
                MemberFromId = replyToMessage.From!.Id,
                MemberFirstName = replyToMessage.From?.FirstName,
                MemberLastName = replyToMessage.From?.LastName,
                AdminUserId = fromId,
                AdminFirstName = message.From?.FirstName,
                AdminLastName = message.From?.LastName,
            }
        );

        var latestWarn = await warnMemberService.GetLatestWarn(chatId, warnUserId);

        htmlMessage.Br().TextBr("⏳ Riwayat");

        latestWarn.ForEach(
            member => {
                htmlMessage.Text("- ");
                htmlMessage.Bold(member.CreatedOn.ToDetailDateTimeString()).Text(" - ");
                htmlMessage.Text(member.Reason.IsNullOrEmpty() ? "N/A" : member.Reason);
                htmlMessage.Br();
            }
        );

        return telegramService.SendTextMessageAsync(
            sendText: htmlMessage.ToString(),
            scheduleDeleteAt: DateTime.UtcNow.AddHours(1),
            includeSenderMessage: true
        );
    }
}