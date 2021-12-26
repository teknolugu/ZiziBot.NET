using System;
using RepoDb.Attributes;

namespace WinTenDev.Zizi.Models.Types;

[Map("step_histories")]
public class StepHistory
{
    public int Id { get; set; }

    public string Name { get; set; }

    [Map("first_name")]
    public string FirstName { get; set; }

    [Map("last_name")]
    public string LastName { get; set; }

    public string Reason { get; set; }

    [Map("chat_id")]
    public long ChatId { get; set; }

    [Map("user_id")]
    public long UserId { get; set; }

    [Map("step_count")]
    public int StepCount { get; set; }

    [Map("last_warn_message_id")]
    public long LastWarnMessageId { get; set; }

    [Map("created_at")]
    public DateTime CreatedAt { get; set; }

    [Map("updated_at")]
    public DateTime UpdatedAt { get; set; }
}