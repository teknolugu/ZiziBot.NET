using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceForceSubExtension
{
    public static async Task AddForceSubsChannelAsync(this TelegramService telegramService)
    {
        if (!await telegramService.CheckFromAdminOrAnonymous())
        {
            await telegramService.DeleteSenderMessageAsync();
            return;
        }

        var chatId = telegramService.ChatId;
        var fSubsService = telegramService.GetRequiredService<ForceSubsService>();
        var chatService = telegramService.GetRequiredService<ChatService>();
        var channelId = telegramService.GetCommandParamAt<long>(0);

        if (channelId == 0)
        {
            await telegramService.SendTextMessageAsync(
                sendText: "Silakan masukkan ID Channel yang ingin ditambahkan",
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                includeSenderMessage: true
            );

            return;
        }

        var currentSubs = await fSubsService.GetSubsAsync(chatId);
        if (currentSubs.Count >= 1)
        {
            await telegramService.AppendTextAsync(
                sendText: "Grup ini sudah memiliki kanal langganan eksternal. \nKetik <code>/fsublist</code> untuk melihat daftar.",
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                includeSenderMessage: true
            );

            return;
        }

        await telegramService.AppendTextAsync("<b>Force Subscription</b>");
        await telegramService.AppendTextAsync($"Channel ID: {channelId}");
        try
        {
            await telegramService.AppendTextAsync("Sedang memeriksa..");
            var chat = await chatService.GetChatAsync(channelId);

            if (chat.Type != ChatType.Channel)
            {
                await telegramService.AppendTextAsync("ID diatas bukan sebuah Channel", reappendText: true);
                return;
            }

            await telegramService.AppendTextAsync($"Title: {chat.Title}", reappendText: true);

            var fSubData = new ForceSubscriptionEntity()
            {
                ChatId = telegramService.ChatId,
                UserId = telegramService.FromId,
                ChannelId = channelId,
                ChannelTitle = chat.Title,
                InviteLink = chat.InviteLink,
            };

            var save = await fSubsService.SaveSubsAsync(fSubData);
            var saveMessage = save == 1 ? "Sudah disimpan" : "Berhasil disimpan";

            await telegramService.AppendTextAsync(
                sendText: saveMessage,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                includeSenderMessage: true
            );
        }
        catch (Exception e)
        {
            var errorMessage = e.Message switch
            {
                {} a when a.Contains("not found") => $"Channel tidak ditemukan",
                _ => "Terjadi kesalahan ketika menyimpan data"
            };

            await telegramService.AppendTextAsync(
                errorMessage,
                reappendText: true,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                includeSenderMessage: true
            );
        }
    }

    public static async Task GetSubsListChannelAsync(this TelegramService telegramService)
    {
        var fSubsService = telegramService.GetRequiredService<ForceSubsService>();

        var subscriptions = await fSubsService.GetSubsAsync(telegramService.ChatId);

        if (subscriptions.Count == 0)
        {
            await telegramService.AppendTextAsync(
                sendText: "Tidak ada subscription di Grup ini",
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                includeSenderMessage: true,
                preventDuplicateSend: true
            );

            return;
        }

        var htmlMessage = HtmlMessage.Empty
            .BoldBr("Daftar Subscription")
            .Bold("Jumlah: ").TextBr(subscriptions.Count.ToString());

        var inlineKeyboard = new List<IEnumerable<InlineKeyboardButton>>();

        subscriptions.ForEach(
            subscription => {
                inlineKeyboard.Add(
                    new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithUrl(subscription.ChannelTitle, subscription.InviteLink)
                    }
                );
            }
        );

        await telegramService.SendTextMessageAsync(
            sendText: htmlMessage.ToString(),
            replyMarkup: inlineKeyboard.ToButtonMarkup(),
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
            preventDuplicateSend: true,
            includeSenderMessage: true
        );
    }

    public static async Task DeleteForceSubsChannelAsync(this TelegramService telegramService)
    {
        await telegramService.DeleteSenderMessageAsync();

        if (!await telegramService.CheckFromAdminOrAnonymous()) return;

        var fSubsService = telegramService.GetRequiredService<ForceSubsService>();

        var subscriptions = await fSubsService.GetSubsAsync(telegramService.ChatId);

        var inlineKeyboard = new List<IEnumerable<InlineKeyboardButton>>();

        if (subscriptions.Count == 0)
        {
            await telegramService.AppendTextAsync(
                sendText: "Tidak ada subscription di Grup ini",
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1)
            );

            return;
        }

        var htmlMessage = HtmlMessage.Empty
            .BoldBr("Daftar Subscription")
            .TextBr("Pilih Channel untuk dihapus: ");

        subscriptions.ForEach(
            subscription => {
                inlineKeyboard.Add(
                    new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithCallbackData(subscription.ChannelTitle, "fsub delete " + subscription.ChannelId)
                    }
                );
            }
        );

        inlineKeyboard.Add(
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData("❌ Batal", "delete-message current-message")
            }
        );

        await telegramService.AppendTextAsync(
            sendText: htmlMessage.ToString(),
            replyMarkup: inlineKeyboard.ToButtonMarkup(),
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
            includeSenderMessage: true,
            preventDuplicateSend: true
        );
    }

    public static async Task<bool> PreCheckForceSubscriptionAsync(this TelegramService telegramService)
    {
        var chatSettings = await telegramService.GetChatSetting();
        if (!(telegramService.IsSenderChannel &&
              chatSettings.EnableForceSubscription))
        {
            telegramService.IsShouldCheckChannelSubscription = true;
            return true;
        }

        var chatId = telegramService.ChatId;
        var userId = telegramService.FromId;
        var senderChatId = telegramService.SenderChat.Id;

        var userExceptionService = telegramService.GetRequiredService<UserExceptionService>();

        var isExist = await userExceptionService.IsExist(chatId, senderChatId);

        if (isExist)
        {
            telegramService.IsShouldCheckChannelSubscription = false;
            return true;
        }

        var sender = telegramService.SenderChat;
        await telegramService.SendTextMessageAsync(
            enumLang: ChannelSubscription.ShouldUseRealAccount,
            scheduleDeleteAt: DateTime.Now.AddMinutes(1)
        );

        await telegramService.BanSenderChatAsync(sender.Id);

        telegramService.IsShouldCheckChannelSubscription = false;

        return false;
    }
}