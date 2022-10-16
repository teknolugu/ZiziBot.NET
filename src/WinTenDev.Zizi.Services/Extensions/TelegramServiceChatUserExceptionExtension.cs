using System;
using System.Threading.Tasks;
using Serilog;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceChatUserExceptionExtension
{
    public static async Task AddUserException(this TelegramService telegramService)
    {
        if (!await telegramService.CheckFromAdminOrAnonymous())
        {
            Log.Information("User does not have permission to add User Exception");
            return;
        }

        var userExceptionService = telegramService.GetRequiredService<UserExceptionService>();

        var message = string.Empty;
        long userId = 0;
        var param = telegramService.GetCommandParamAt<string>(0);
        if (telegramService.ReplyToMessage != null)
        {
            var replyToMessage = telegramService.ReplyToMessage;
            userId = replyToMessage.From!.Id;
            if (replyToMessage.SenderChat != null)
            {
                userId = replyToMessage.SenderChat.Id;
            }
        }
        else if (param?.Contains('@') ?? false)
        {
            var wTelegramService = telegramService.GetRequiredService<WTelegramApiService>();

            var username = param.TrimStart('@');
            var resolvedPeer = await wTelegramService.FindPeerByUsername(username);

            if (resolvedPeer?.Chat != null)
            {
                var chat = resolvedPeer.Chat;
                userId = chat.ID;
            }
            else
            {
                message = "Peer tidak ditemukan";
            }
        }
        else if (param.IsNotNullOrEmpty())
        {
            if (!long.TryParse(param, out userId))
            {
                message = "Invalid user id";
            }
        }
        else
        {
            message = "kurang";
        }

        if (userId != 0)
        {
            var save = await userExceptionService.Save(new UserExceptionEntity()
            {
                ChatId = telegramService.ChatId,
                UserId = userId,
                AddedBy = telegramService.From.Id,
            });

            message = save == 1 ? "User berhasil disimpan" : "User telah disimpan";
        }

        await telegramService.SendTextMessageAsync(
            sendText: message,
            scheduleDeleteAt: DateTime.Now.AddMinutes(5),
            includeSenderMessage: true
        );
    }
}