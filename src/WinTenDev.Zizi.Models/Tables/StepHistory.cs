using System;
using RepoDb.Attributes;
using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Tables;

[Map("step_histories")]
public class StepHistory
{
    public int Id { get; set; }

    public StepHistoryName Name { get; set; }

    [Map("first_name")]
    public string FirstName { get; set; }

    [Map("last_name")]
    public string LastName { get; set; }

    [Map("reason")]
    public string Reason { get; set; }

    [Map("chat_id")]
    public long ChatId { get; set; }

    [Map("user_id")]
    public long UserId { get; set; }

    [Map("status")]
    public StepHistoryStatus Status { get; set; }

    [Map("hangfire_job_id")]
    public string HangfireJobId { get; set; }

    [Map("last_warn_message_id")]
    public int LastWarnMessageId { get; set; }

    [Map("created_at")]
    public DateTime CreatedAt { get; set; }

    [Map("updated_at")]
    public DateTime UpdatedAt { get; set; }
}