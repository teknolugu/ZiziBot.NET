using System;
using System.Collections.Generic;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace WinTenDev.Zizi.Models.Dto;

public class MessageResponseDto
{
    public string MessageText { get; set; }
    public InlineKeyboardMarkup ReplyMarkup { get; set; }

    public List<IAlbumInputMedia> ListAlbum { get; set; }

    public bool DisableWebPreview { get; set; } = true;
    public long ReplyToMessageId { get; set; }
    public bool IncludeSenderForDelete { get; set; }
    public DateTime ScheduleDeleteAt { get; set; }
}