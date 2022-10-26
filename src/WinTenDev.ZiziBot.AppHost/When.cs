using System;
using Microsoft.AspNetCore.Http;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost;

public static class When
{
    public static bool WebHook(IUpdateContext context) =>
        context.Items.ContainsKey(nameof(HttpContext));

    public static bool SkipCheck(IUpdateContext context)
    {
        return context.Update.ChannelPost != null ||
               context.Update.EditedChannelPost != null;
    }

    public static bool NewUpdate(IUpdateContext context) =>
        context.Update != null;

    public static bool NewMessage(IUpdateContext context) =>
        context.Update.Message != null;

    public static bool EditedMessage(IUpdateContext context) =>
        context.Update.EditedMessage != null;

    public static bool NewOrEditedMessage(IUpdateContext context) =>
        context.Update.Message != null ||
        context.Update.EditedMessage != null;

    public static bool NewTextMessage(IUpdateContext context) =>
        context.Update.Message?.Text != null ||
        context.Update.EditedMessage?.Text != null;

    public static bool NewCommand(IUpdateContext context)
    {
        var message = context.Update.Message ?? context.Update.EditedMessage;
        var isNewCommand = message?.Text != null && message.Text.StartsWith("/");

        return isNewCommand;
    }

    public static bool PingReceived(IUpdateContext context)
    {
        var botUsername = context.Bot.Username;
        var message = context.Update.Message ?? context.Update.EditedMessage;
        var cmd = message?.Text?.ToLower();

        return cmd is "ping" or "/ping" ||
               string.Equals(cmd, $"/ping@{botUsername}", StringComparison.CurrentCultureIgnoreCase);
    }

    public static bool CallTagReceived(IUpdateContext context)
    {
        var isTrue = false;
        var message = context.Update.Message ?? context.Update.EditedMessage;

        if (message?.Text != null)
        {
            isTrue = message.Text.Contains("#");
        }

        return isTrue;
    }

    public static bool MembersChanged(IUpdateContext context) =>
        context.Update.ChannelPost?.NewChatMembers != null ||
        context.Update.ChannelPost?.LeftChatMember != null;

    public static bool LeftChatMember(IUpdateContext context) =>
        context.Update.Message?.LeftChatMember != null;

    public static bool NewChatMembers(IUpdateContext context) =>
        context.Update.Message?.NewChatMembers != null;

    public static bool NewPinnedMessage(IUpdateContext context) =>
        context.Update.Message?.PinnedMessage != null;

    public static bool LocationMessage(IUpdateContext context) =>
        context.Update.Message?.Location != null;

    public static bool StickerMessage(IUpdateContext context) =>
        context.Update.Message?.Sticker != null;

    public static bool MediaReceived(IUpdateContext context) =>
        context.Update.Message?.Document != null ||
        context.Update.Message?.Photo != null;

    public static bool CallbackQuery(IUpdateContext context) =>
        context.Update.CallbackQuery != null;

    public static bool InlineQuery(IUpdateContext context) =>
        context.Update.InlineQuery != null;

    public static bool ChatJoinRequest(IUpdateContext context) =>
        context.Update.ChatJoinRequest != null;
}
