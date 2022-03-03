using System;

namespace WinTenDev.Zizi.Models.Tables;

public class Rule
{
    public long Id { get; set; }
    public long FromId { get; set; }
    public long ChatId { get; set; }
    public string RuleText { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}