using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodingSeb.ExpressionEvaluator;
using CodingSeb.Localization;
using Humanizer;
using Serilog;
using SerilogTimings;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceSettingsExtension
{
    public static async Task SaveInlineSettingsAsync(this TelegramService telegramService)
    {
        var defaultScheduleDelete = DateTime.UtcNow.AddMinutes(1);

        if (!await telegramService.CheckUserPermission())
        {
            await telegramService.SendTextMessageAsync(
                sendText: "Kamu tidak mempunyai hak akses",
                scheduleDeleteAt: DateTime.UtcNow.AddSeconds(10)
            );

            return;
        }

        var chatId = telegramService.ChatId;
        var command = telegramService.GetCommand();
        var setKey = command.RemoveThisString(
            "/set_",
            "/set"
        );

        var setValues = telegramService.MessageTextParts
            .Where(s => !s.Contains(telegramService.GetCommand()))
            .ToList();

        if (setValues.Count != 2)
        {
            await telegramService.SendTextMessageAsync(
                sendText: "Silakan masukan Key dan Value yang dinginkan",
                scheduleDeleteAt: defaultScheduleDelete
            );

            return;
        }

        var cmdKey = setValues.ElementAtOrDefault(0);
        var cmdValue = setValues.ElementAtOrDefault(1);

        try
        {
            var (verifyKey, verifyValue) = VerifyInlineSettings(cmdKey, cmdValue);

            if (verifyKey == null ||
                verifyValue == null)
            {
                await telegramService.SendTextMessageAsync(
                    sendText: "Key atau Value yang anda masukan tidak valid",
                    scheduleDeleteAt: defaultScheduleDelete,
                    includeSenderMessage: true
                );

                return;
            }

            await telegramService.SettingsService.UpdateCell(
                chatId: chatId,
                key: verifyKey,
                value: verifyValue
            );

            await telegramService.SendTextMessageAsync(
                sendText: "Pengaturan berhasil di perbarui" +
                          $"\nSet <code>{verifyKey.Humanize().Titleize()}</code> to <code>{verifyValue}</code>",
                scheduleDeleteAt: defaultScheduleDelete,
                includeSenderMessage: true
            );
        }
        catch (Exception ex)
        {
            await telegramService.SendTextMessageAsync(
                sendText: "Terjadi kesalahan saat menyimpan pengaturan" +
                          $"\n{ex.Message}",
                scheduleDeleteAt: defaultScheduleDelete,
                includeSenderMessage: true
            );
        }
    }

    public static (string resultKey, string resultValue) VerifyInlineSettings(
        string key,
        string value
    )
    {
        var resultKey = key switch
        {
            "tz" => "timezone_offset",
            "lang" => "language_code",
            _ => null
        };

        var resultValue = resultKey switch
        {
            "timezone_offset" => TimeUtil.FindTimeZoneByOffsetBase(value)?.BaseUtcOffset.ToStringFormat(@"hh\:mm"),
            "language_code" => Loc.Instance.AvailableLanguages.FirstOrDefault(s => s == value),
            _ => null
        };

        return (resultKey, resultValue);
    }

    public static async Task SaveSettingToggleInCommandAsync(this TelegramService telegramService)
    {
        var defaultScheduleDelete = DateTime.UtcNow.AddMinutes(10);

        if (!await telegramService.CheckFromAdminOrAnonymous())
        {
            await telegramService.SendTextMessageAsync(
                sendText: "Hanya admin yang dapat menggunakan perintah ini",
                scheduleDeleteAt: defaultScheduleDelete
            );

            return;
        }

        var chatId = telegramService.ChatId;
        var command = telegramService.GetCommand(true);
        var boolCmd = command.ToBool();
        var param0 = telegramService.GetCommandParam(0);

        if (param0 == null)
        {
            await telegramService.SendTextMessageAsync(
                sendText: "Silakan masukan parameter yang ingin diubah",
                scheduleDeleteAt: defaultScheduleDelete
            );

            return;
        }

        var settingsService = telegramService.GetRequiredService<SettingsService>();

        var columName = new[]
        {
            "afk_status",
            "anti_malfiles",
            "fed_cas_ban",
            "fed_es2_ban",
            "fed_spamwatch",
            "flood_check",
            "fire_check",
            "find_tags",
            "force_subscription",
            "human_verification",
            "check_profile_photo",
            "reply_notification",
            "privacy_mode",
            "spell_check",
            "warn_username",
            "welcome_message",
            "word_filter_global",
            "zizi_mata"
        }.FirstOrDefault(key => key == param0);

        if (columName == null)
        {
            await telegramService.SendTextMessageAsync(
                sendText: "Parameter yang Anda masukan tidak ditemukan",
                scheduleDeleteAt: defaultScheduleDelete
            );

            return;
        }

        var keyFound = $"enable_{columName}";
        var columnValue = boolCmd.ToInt();
        var statusStr = boolCmd ? "mengaktifkan" : "mematikan";

        await settingsService.UpdateCell(
            chatId: chatId,
            key: keyFound,
            value: columnValue
        );

        var htmlMessage = HtmlMessage.Empty
            .Text($"Berhasil {statusStr} ").Bold(columName.Titleize());

        await telegramService.SendTextMessageAsync(
            sendText: htmlMessage.ToString(),
            scheduleDeleteAt: defaultScheduleDelete,
            includeSenderMessage: true
        );

        await settingsService.GetSettingsByGroup(chatId, evictBefore: true);
    }

    public static async Task SaveWelcomeSettingsAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var message = telegramService.MessageOrEdited;
        var command = telegramService.GetCommand();
        var key = command.Remove(0, 5);

        if (telegramService.IsPrivateChat)
        {
            await telegramService.SendTextMessageAsync("Atur pesan Welcome hanya untuk grup saja");
            return;
        }

        if (!await telegramService.CheckFromAdminOrAnonymous()) return;

        var columnTarget = new Dictionary<string, string>()
        {
            { "welcome_btn", "welcome_button" },
            { "welcome_doc", "welcome_document" },
            { "welcome_msg", "welcome_message" }
        }.FirstOrDefault(pair => pair.Key == key).Value;

        var featureName = columnTarget.Titleize();

        await telegramService.SendTextMessageAsync($"Sedang menyimpan {featureName}..");

        if (key == "welcome_doc")
        {
            if (telegramService.ReplyToMessage == null)
            {
                await telegramService.EditMessageTextAsync("Silakan balas file yang ingin di simpan");

                return;
            }
            var mediaFileId = telegramService.ReplyToMessage.GetFileId();
            var mediaType = telegramService.ReplyToMessage.Type;

            await telegramService.SettingsService.UpdateCell(
                chatId: chatId,
                key: "welcome_media",
                value: mediaFileId
            );
            await telegramService.SettingsService.UpdateCell(
                chatId: chatId,
                key: "welcome_media_type",
                value: mediaType
            );
        }
        else
        {
            var welcomeData = message.CloneText(key == "welcome_btn").GetTextWithoutCmd();

            if (welcomeData.IsNullOrEmpty())
            {
                await telegramService.EditMessageTextAsync(
                    new MessageResponseDto()
                    {
                        MessageText = $"Silakan tentukan data untuk {featureName} " +
                                      "atau balas sebuah pesan yang diinginkan.",
                        ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(1),
                        IncludeSenderForDelete = true
                    }
                );
                return;
            }

            await telegramService.SettingsService.UpdateCell(
                chatId: chatId,
                key: columnTarget,
                value: welcomeData
            );
        }

        await telegramService.EditMessageTextAsync(
            new MessageResponseDto()
            {
                MessageText = $"<b>{featureName}</b> berhasil di simpan!" +
                              $"\nKetik <code>/welcome</code> untuk melihat perubahan",
                ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(1),
                IncludeSenderForDelete = true
            }
        );
    }

    public static async Task GetWelcomeSettingsAsync(this TelegramService telegramService)
    {
        var chatId = telegramService.ChatId;
        var chatTitle = telegramService.ChatTitle;

        telegramService.DeleteSenderMessageAsync().InBackground();

        if (!await telegramService.CheckFromAdminOrAnonymous()) return;

        var settings = await telegramService.SettingsService.GetSettingsByGroup(chatId);
        var welcomeMessage = settings.WelcomeMessage;
        var welcomeButton = settings.WelcomeButton;
        var welcomeMedia = settings.WelcomeMedia;
        var welcomeMediaType = settings.WelcomeMediaType;

        var sendText = $"⚙ Konfigurasi Welcome di <b>{chatTitle}</b>\n\n";

        if (welcomeMessage.IsNullOrEmpty())
        {
            var defaultWelcome = "Hai {allNewMember}" +
                                 "\nSelamat datang di kontrakan {chatTitle}" +
                                 "\nKamu adalah anggota ke-{memberCount}";

            sendText += "Tidak ada konfigurasi pesan welcome, pesan default akan di terapkan" +
                        $"\n\n<code>{defaultWelcome}</code>";
        }
        else
        {
            sendText += $"{welcomeMessage}";
        }

        var keyboardMarkup = welcomeButton.ToButtonMarkup();

        sendText += "\n\n<b>Raw Button:</b>" +
                    $"\n<code>{welcomeButton}</code>";

        if (welcomeMediaType > 0)
        {
            await telegramService.SendMediaAsync(
                fileId: welcomeMedia,
                mediaType: welcomeMediaType,
                caption: sendText,
                replyMarkup: keyboardMarkup,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(3),
                preventDuplicateSend: true
            );
        }
        else
        {
            await telegramService.SendTextMessageAsync(
                sendText: sendText,
                replyMarkup: keyboardMarkup,
                replyToMsgId: 0,
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(3),
                preventDuplicateSend: true
            );
        }
    }

    public static async Task SendWelcomeMessageAsync(this TelegramService telegramService)
    {
        var msg = telegramService.Message;
        var chatId = telegramService.ChatId;
        var chatTitle = telegramService.ChatTitle;

        var op = Operation.Begin("New Chat Members on ChatId {ChatId}", chatId);

        var chatSetting = await telegramService.SettingsService.GetSettingsByGroup(chatId);

        if (!chatSetting.EnableWelcomeMessage)
        {
            Log.Information("Welcome message is disabled at ChatId: {ChatId}", chatId);
            return;
        }

        var welcomeMessage = chatSetting.WelcomeMessage;
        var welcomeButton = chatSetting.WelcomeButton;

        var newMembers = msg.NewChatMembers;

        if (newMembers == null) return;

        var isBootAdded = await telegramService.IsAnyMe(newMembers);

        if (isBootAdded)
        {
            var getMe = await telegramService.GetMeAsync();

            var greetMe = $"Hai, perkenalkan saya {getMe.FirstName}" +
                          $"\n\nSaya adalah bot pendebug dan grup manajemen yang dilengkapi dengan alat keamanan. " +
                          $"Agar saya berfungsi penuh, jadikan saya admin dengan level standard. " +
                          $"\n\nUntuk melihat daftar perintah bisa ketikkan /help";

            await telegramService.SendTextMessageAsync(greetMe, replyToMsgId: 0);

            await telegramService.SettingsService.SaveSettingsAsync
            (
                new Dictionary<string, object>()
                {
                    { "chat_id", chatId },
                    { "chat_title", chatTitle }
                }
            );

            if (newMembers.Length == 1) return;
        }

        var parsedNewMember = await telegramService.NewChatMembersService.CheckNewChatMembers(
            chatId,
            newMembers,
            answer =>
                telegramService.CallbackAnswerAsync(answer)
        );

        var allNewMember = parsedNewMember.AllNewChatMembersStr.JoinStr(", ");
        var allNoUsername = parsedNewMember.NewNoUsernameChatMembersStr.JoinStr(", ");
        var allNewBot = parsedNewMember.NewBotChatMembersStr.JoinStr(", ");

        if (allNewMember.Length == 0)
        {
            Log.Information("Welcome Message ignored because User is Global Banned..");
            return;
        }

        var greet = TimeUtil.GetTimeGreet();
        var memberCount = await telegramService.GetMemberCount();
        var newMemberCount = newMembers.Length;

        Log.Information("Preparing send Welcome..");

        if (welcomeMessage.IsNullOrEmpty())
        {
            welcomeMessage = "Hai {AllNewMember}" +
                             "\nSelamat datang di kontrakan {ChatTitle}" +
                             "\nKamu adalah anggota ke-{MemberCount}";
        }

        var listKeyboardButton = welcomeButton.ToInlineKeyboardButton().ToList();
        var enableHumanVerification = chatSetting.EnableHumanVerification;

        if (enableHumanVerification)
        {
            Log.Debug("Adding verify button..");

            listKeyboardButton.Add(
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Saya adalah Manusia!", "verify")
                }
            );
        }

        var evaluator = new ExpressionEvaluator();
        var fixedWelcomeMessage = welcomeMessage.Split("\n")
            .Select(
                part => {
                    part = part.ResolveVariable(
                        new List<(string placeholder, string value)>()
                        {
                            ("enableHumanVerification", enableHumanVerification.ToString().ToLower())
                        }
                    );

                    try
                    {
                        var result = evaluator.Evaluate<string>(part);
                        return result;
                    }
                    catch (Exception e)
                    {
                        Log.Debug("Partial: '{Part}' is not a valid expression", part);
                        return part;
                    }
                }
            )
            .JoinStr("\n")
            .ResolveVariable
            (
                new List<(string placeholder, string value)>()
                {
                    ("AllNewMember", allNewMember),
                    ("AllNoUsername", allNoUsername),
                    ("AllNewBot", allNewBot),
                    ("ChatTitle", chatTitle),
                    ("Greet", greet),
                    ("NewMemberCount", newMemberCount.ToString()),
                    ("MemberCount", memberCount.ToString())
                }
            )
            .Trim();

        var inlineKeyboardMarkup = listKeyboardButton.ToButtonMarkup();

        if (chatSetting.WelcomeMediaType > 0)
        {
            var welcomeMedia = chatSetting.WelcomeMedia;
            var mediaType = chatSetting.WelcomeMediaType;

            await telegramService.SendMediaAsync(
                fileId: welcomeMedia,
                mediaType: mediaType,
                caption: fixedWelcomeMessage,
                replyMarkup: inlineKeyboardMarkup,
                replyToMsgId: 0,
                scheduleDeleteAt: DateTime.UtcNow.AddDays(1),
                preventDuplicateSend: true,
                messageFlag: MessageFlag.NewChatMembers
            );
        }
        else
        {
            await telegramService.SendTextMessageAsync(
                sendText: fixedWelcomeMessage,
                replyMarkup: inlineKeyboardMarkup,
                replyToMsgId: 0,
                scheduleDeleteAt: DateTime.UtcNow.AddDays(1),
                preventDuplicateSend: true,
                messageFlag: MessageFlag.NewChatMembers
            );
        }

        await telegramService.SettingsService.SaveSettingsAsync
        (
            new Dictionary<string, object>()
            {
                { "chat_id", telegramService.ChatId },
                { "chat_title", telegramService.ChatTitle },
                { "chat_type", telegramService.Chat.Type.Humanize() },
                { "members_count", memberCount }
            }
        );

        op.Complete();
    }

}
