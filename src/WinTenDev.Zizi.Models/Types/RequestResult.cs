using System;
using Telegram.Bot.Types;

namespace WinTenDev.Zizi.Models.Types;

public class RequestResult
{
    public bool IsSuccess { get; set; }
    public int ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
    public Exception ErrorException { get; set; }
    public Message SentMessage { get; set; }
    public Message[] SentMessages { get; set; }
}