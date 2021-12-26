using System;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Types;

public class CallbackAnswer
{
    public CallbackAnswerMode CallbackAnswerMode { get; set; }
    public CallbackAnswerMode[] CallbackAnswerModes { get; set; }
    public IReplyMarkup CallbackAnswerMarkup { get; set; }
    public InlineKeyboardMarkup CallbackAnswerInlineMarkup { get; set; }
    public string CallbackAnswerText { get; set; }
    public TimeSpan MuteMemberTimeSpan { get; set; }
}