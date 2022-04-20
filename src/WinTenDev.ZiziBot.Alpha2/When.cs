using System;
using TgBotFramework;

namespace WinTenDev.ZiziBot.Alpha2;

public static class When
{
    public static bool WebHook(UpdateContext context) => false;
    // context.ContainsKey(nameof(HttpContext));

    public static bool SkipCheck(UpdateContext context)
    {
        return context.Update.ChannelPost != null ||
               context.Update.EditedChannelPost != null;
    }

    public static bool NewUpdate(UpdateContext context) =>
        context.Update != null;

    public static bool NewMessage(UpdateContext context) =>
        context.Update.Message != null;

    public static bool EditedMessage(UpdateContext context) =>
        context.Update.EditedMessage != null;

    public static bool NewOrEditedMessage(UpdateContext context) =>
        context.Update.Message != null ||
        context.Update.EditedMessage != null;

    public static bool NewTextMessage(UpdateContext context) =>
        context.Update.Message?.Text != null ||
        context.Update.EditedMessage?.Text != null;

    public static bool NewCommand(UpdateContext context)
    {
        var message = context.Update.Message ?? context.Update.EditedMessage;
        var isNewCommand = message?.Text != null && message.Text.StartsWith("/");

        return isNewCommand;
    }

    public static bool PingReceived(UpdateContext context)
    {
        var botUsername = context.Bot.Username;
        var message = context.Update.Message ?? context.Update.EditedMessage;
        var cmd = message?.Text?.ToLower();

        return cmd is "ping" or "/ping" ||
               string.Equals(
                   cmd,
                   $"/ping@{botUsername}",
                   StringComparison.CurrentCultureIgnoreCase
               );
    }

    public static bool CallTagReceived(UpdateContext context)
    {
        var isTrue = false;
        var message = context.Update.Message ?? context.Update.EditedMessage;

        if (message?.Text != null)
        {
            isTrue = message.Text.Contains("#");
        }

        return isTrue;
    }

    public static bool MembersChanged(UpdateContext context) =>
        context.Update.ChannelPost?.NewChatMembers != null ||
        context.Update.ChannelPost?.LeftChatMember != null;

    public static bool LeftChatMember(UpdateContext context) =>
        context.Update.Message?.LeftChatMember != null;

    public static bool NewChatMembers(UpdateContext context) =>
        context.Update.Message?.NewChatMembers != null;

    public static bool NewPinnedMessage(UpdateContext context) =>
        context.Update.Message?.PinnedMessage != null;

    public static bool LocationMessage(UpdateContext context) =>
        context.Update.Message?.Location != null;

    public static bool StickerMessage(UpdateContext context) =>
        context.Update.Message?.Sticker != null;

    public static bool MediaReceived(UpdateContext context) =>
        context.Update.Message?.Document != null ||
        context.Update.Message?.Photo != null;

    public static bool CallbackQuery(UpdateContext context) =>
        context.Update.CallbackQuery != null;
}
