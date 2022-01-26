using System.Linq;
using Serilog;
using Telegram.Bot.Types;

namespace WinTenDev.Zizi.Utils.Telegram;

public static class ChatUtil
{
    public static long ReduceChatId(this long chatId)
    {
        var chatIdStr = chatId.ToString();
        if (!chatIdStr.StartsWith("-100")) return chatId;

        chatIdStr = chatIdStr[4..];

        Log.Verbose("Reduced ChatId from {ChatId} to {Reduced}", chatId, chatIdStr);

        return chatIdStr.ToInt64();
    }

    public static long FixChatId(this long chatId)
    {
        var chatIdStr = chatId.ToString();
        if (chatIdStr.StartsWith("-100")) return chatId;

        chatIdStr = "-100" + chatIdStr;

        Log.Verbose("Fixing ChatId from {Reduced} to {ChatId}", chatId, chatIdStr);

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

    public static string GetChatLink(
        this string chatUsername
    )
    {
        if (chatUsername.IsNullOrEmpty()) return string.Empty;

        var chatLink = $"https://t.me/{chatUsername}";

        Log.Debug("MessageLink: {Link}", chatLink);
        return chatLink;
    }

    public static string GetChatNameLink(
        this string chatUsername,
        string chatTitle
    )
    {
        if (chatUsername.IsNullOrEmpty()) return chatTitle;

        var chatNameLink = $"<a href=\"{chatUsername.GetChatLink()}\">{chatTitle}</a>";

        Log.Debug("ChatNameLink: {Link}", chatNameLink);
        return chatNameLink;
    }
}