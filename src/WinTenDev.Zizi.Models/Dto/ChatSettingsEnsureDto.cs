using System;

namespace WinTenDev.Zizi.Models.Dto;

public class ChatSettingsEnsureDto
{
    public long ChatId { get; set; }
    public string ChatTitle { get; set; }
    public string ChatType { get; set; }
    public long MembersCount { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime UpdatedAt { get; set; }
}