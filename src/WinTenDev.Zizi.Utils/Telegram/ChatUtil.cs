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

        Log.Debug("Reduced ChatId from {0} to {1}", chatId, chatIdStr);

        return chatIdStr.ToInt64();
    }

    public static long FixChatId(this long chatId)
    {
        var chatIdStr = chatId.ToString();
        if (chatIdStr.StartsWith("-100")) return chatId;

        chatIdStr = "-100" + chatIdStr;

        Log.Debug("Fixing ChatId from {0} to {1}", chatId, chatIdStr);

        return chatIdStr.ToInt64();
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