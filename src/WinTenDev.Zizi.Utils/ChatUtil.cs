using Serilog;

namespace WinTenDev.Zizi.Utils;

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
}