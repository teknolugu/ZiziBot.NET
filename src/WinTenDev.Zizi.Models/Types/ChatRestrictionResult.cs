using System;
using BotFramework.Utils;

namespace WinTenDev.Zizi.Models.Types;

/// <summary>
/// Result of Chat Restriction
/// </summary>
public class ChatRestrictionResult
{
    public bool IsSuccessful { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsRestricted { get; set; }
    public bool DoLeaveChat { get; set; }
    public long ChatId { get; set; }
    public HtmlString HtmlMessage { get; set; }
    public DateTime CompletionTime { get; set; }
}