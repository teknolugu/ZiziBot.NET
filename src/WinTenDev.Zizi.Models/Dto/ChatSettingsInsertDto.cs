using System;

namespace WinTenDev.Zizi.Models.Dto;

public class ChatSettingsInsertDto
{
    public long ChatId { get; set; }
    public string ChatTitle { get; set; }
    public string ChatType { get; set; }
    public long MembersCount { get; set; }
    public long EventLogChatId { get; set; }
    public bool IsAdmin { get; set; }
    public string WelcomeMessage { get; set; }
    public string WelcomeButton { get; set; }
    public string WelcomeMedia { get; set; }
    public int WelcomeMediaType { get; set; }
    public string RulesText { get; set; }
    public long LastTagsMessageId { get; set; }
    public long LastWarnUsernameMessageId { get; set; }
    public long LastWelcomeMessageId { get; set; }
    public bool EnableAfkStatus { get; set; } = true;
    public bool EnableHumanVerification { get; set; } = true;
    public bool EnableFedCasBan { get; set; } = true;
    public bool EnableFedEs2Ban { get; set; } = true;
    public bool EnableFedSpamwatch { get; set; } = true;
    public bool EnableFindNotes { get; set; }
    public bool EnableFindTags { get; set; } = true;
    public bool EnableFireCheck { get; set; }
    public bool EnableWordFilterGroup { get; set; } = true;
    public bool EnableWordFilterGlobal { get; set; } = true;
    public bool EnableWarnUsername { get; set; } = true;
    public bool EnableCheckProfilePhoto { get; set; } = true;
    public bool EnableWelcomeMessage { get; set; } = true;
    public bool EnableZiziMata { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}