using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Serilog;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Telegram;

namespace WinTenDev.Zizi.Utils.Telegram;

public static class ChatUtil
{
    public static long ReduceChatId(this long chatId)
    {
        var chatIdStr = chatId.ToString();
        if (!chatIdStr.StartsWith("-100")) return chatId;

        chatIdStr = chatIdStr[4..];

        Log.Verbose(
            "Reduced ChatId from {ChatId} to {Reduced}",
            chatId,
            chatIdStr
        );

        return chatIdStr.ToInt64();
    }

    public static long FixChatId(this long chatId)
    {
        var chatIdStr = chatId.ToString();
        if (chatIdStr.StartsWith("-100")) return chatId;

        chatIdStr = "-100" + chatIdStr;

        Log.Verbose(
            "Fixing ChatId from {Reduced} to {ChatId}",
            chatId,
            chatIdStr
        );

        return chatIdStr.ToInt64();
    }

    public static string GetChatKey(
        this long chatId,
        string prefix
    )
    {
        return $"{prefix}_{chatId.ReduceChatId()}";
    }

    public static string GetUserKey(
        this long userId,
        string prefix
    )
    {
        return $"{prefix}_{userId}";
    }

    public static string GetChatUserKey(
        this long chatId,
        long userId,
        string prefix
    )
    {
        return $"{prefix}_{chatId.ReduceChatId()}_{userId}";
    }

    public static string ToAdminMention(this ChatMember[] chatMembers)
    {
        var adminMention = chatMembers
            .Select(member => member.User.Id.GetMention())
            .JoinStr("");

        return adminMention;
    }

    public static string GetChatLink(this string chatUsername)
    {
        if (chatUsername.IsNullOrEmpty()) return string.Empty;

        var chatLink = $"https://t.me/{chatUsername}";

        Log.Debug("MessageLink: {Link}", chatLink);
        return chatLink;
    }

    public static string GetChatLink(this Chat chat)
    {
        var chatLink = chat.Username.GetChatLink();
        return chatLink;
    }

    public static string GetChatNameLink(
        this string chatUsername,
        string chatTitle
    )
    {
        if (chatUsername.IsNullOrEmpty()) return chatTitle;

        var chatLink = chatUsername.GetChatLink();
        var chatNameLink = $"<a href=\"{chatLink}\">{chatTitle}</a>";

        Log.Debug("ChatNameLink: {Link}", chatNameLink);
        return chatNameLink;
    }

    public static string GetChatNameLink(this Chat chat)
    {
        var chatUsername = chat.Username;
        var chatTitle = chat.Title;

        var chatNameLink = chatUsername.GetChatNameLink(chatTitle);

        Log.Debug("ChatNameLink: {Link}", chatNameLink);
        return chatNameLink;
    }

    public static string GetChatTitle(this Chat chat)
    {
        var chatTitle = chat.Title;

        return chatTitle.IsNullOrEmpty() ? "" : chatTitle;
    }

    public static string ToAdminListStr(this ChatMember[] chatMembers)
    {
        var creatorStr = string.Empty;
        var sbAdmin = new StringBuilder();

        var number = 1;

        foreach (var admin in chatMembers)
        {
            var user = admin.User;
            var nameLink = user.GetNameLink();

            if (admin.Status == ChatMemberStatus.Creator)
            {
                creatorStr = nameLink;
            }
            else
            {
                sbAdmin.Append(number++)
                    .Append(". ")
                    .AppendLine(nameLink);
            }
        }

        var adminList = $"👤 <b>Creator</b>" +
                        $"\n└ {creatorStr}" +
                        $"\n" +
                        $"\n👥️ <b>Administrators</b>" +
                        $"\n{sbAdmin.ToTrimmedString()}";

        return adminList;
    }

    public static string ToAdminListStr(this ChannelParticipants channelParticipants)
    {
        var creator = channelParticipants.ParticipantCreator.users
            .Select(x => x.Value)
            .Select(x => x.GetNameLink())
            .JoinStr("\n");

        var adminList = channelParticipants.ParticipantAdmin.users
            .Select(x => x.Value)
            .OrderBy(
                orderBy => orderBy.GetFullName()
                    .RemoveWhitespace()
            )
            .Select
            (
                (
                    user,
                    index
                ) => {
                    var botStr = user.bot_info_version == 0 ? string.Empty : "🤖";
                    var nameLink = user.GetNameLink();

                    return $"{index + 1}. {nameLink} {botStr}";
                }
            )
            .JoinStr("\n");

        var adminAll = "👤 <b>Creator</b>" +
                       $"\n└ {creator}" +
                       "\n\n👥 <b>Administrators</b>" +
                       $"\n{adminList}";

        return adminAll;
    }

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public static User GetSender(this Update update)
    {
        return update.Type switch
        {
            UpdateType.Unknown => null,
            UpdateType.Message => update.Message.From,
            UpdateType.InlineQuery => update.InlineQuery.From,
            UpdateType.ChosenInlineResult => update.ChosenInlineResult.From,
            UpdateType.CallbackQuery => update.CallbackQuery.From,
            UpdateType.EditedMessage => update.EditedMessage.From,
            UpdateType.ChannelPost => update.ChannelPost.From,
            UpdateType.EditedChannelPost => update.EditedChannelPost.From,
            UpdateType.ShippingQuery => update.ShippingQuery.From,
            UpdateType.PreCheckoutQuery => update.PreCheckoutQuery.From,
            UpdateType.Poll => null,
            UpdateType.PollAnswer => update.PollAnswer.User,
            UpdateType.ChatMember => update.ChatMember.From,
            UpdateType.MyChatMember => update.MyChatMember.From,
            UpdateType.ChatJoinRequest => update.ChatJoinRequest.From,
            _ => default
        };
    }

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public static Chat GetChat(this Update update)
    {
        return update.Type switch
        {
            UpdateType.Message => update.Message.Chat,
            UpdateType.CallbackQuery => update.CallbackQuery.Message.Chat,
            UpdateType.EditedMessage => update.EditedMessage.Chat,
            UpdateType.ChannelPost => update.ChannelPost.Chat,
            UpdateType.EditedChannelPost => update.EditedChannelPost.Chat,
            UpdateType.ChatMember => update.ChatMember.Chat,
            UpdateType.MyChatMember => update.MyChatMember.Chat,
            UpdateType.ChatJoinRequest => update.ChatJoinRequest.Chat,
            UpdateType.Unknown => null,
            UpdateType.InlineQuery => null,
            UpdateType.ChosenInlineResult => null,
            UpdateType.ShippingQuery => null,
            UpdateType.PreCheckoutQuery => null,
            UpdateType.Poll => null,
            UpdateType.PollAnswer => null,
            _ => default
        };
    }

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public static DateTime GetMessageDate(this Update update)
    {
        var date = update.Type switch
        {
            UpdateType.EditedMessage => update.EditedMessage.EditDate.GetValueOrDefault(),
            UpdateType.EditedChannelPost => update.EditedChannelPost.EditDate.GetValueOrDefault(),
            UpdateType.Message => update.Message.Date,
            UpdateType.MyChatMember => update.MyChatMember.Date,
            UpdateType.CallbackQuery => DateTime.UtcNow,
            UpdateType.ChannelPost => update.ChannelPost.Date,
            UpdateType.ChatMember => update.ChatMember.Date,
            UpdateType.ChatJoinRequest => update.ChatJoinRequest.Date,
            _ => DateTime.UtcNow
        };

        return date;
    }

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public static DateTime? GetMessageEditDate(this Update update)
    {
        var date = update.Type switch
        {
            UpdateType.EditedMessage => update.EditedMessage.EditDate,
            UpdateType.EditedChannelPost => update.EditedChannelPost.EditDate,
            _ => DateTime.UtcNow
        };

        return date;
    }
}