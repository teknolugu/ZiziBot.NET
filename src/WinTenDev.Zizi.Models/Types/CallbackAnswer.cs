using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Types;

public class CallbackAnswer
{
    public CallbackAnswerMode CallbackAnswerMode { get; set; }
    public List<CallbackAnswerMode> CallbackAnswerModes { get; set; } = new();
    public IReplyMarkup CallbackAnswerMarkup { get; set; }
    public InlineKeyboardMarkup CallbackAnswerInlineMarkup { get; set; }
    public string CallbackAnswerText { get; set; }
    public int CallbackDeleteMessageId { get; set; }
    public long TargetUserId { get; set; }
    public TimeSpan MuteMemberTimeSpan { get; set; }
}

public class CallbackResult
{
    public Message UpdatedMessage { get; set; }
}