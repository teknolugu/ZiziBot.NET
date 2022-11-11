using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Dto;

public class StepHistoryDto
{
    public long ChatId { get; set; }
    public long UserId { get; set; }
    public StepHistoryName Name { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Reason { get; set; }
    public StepHistoryStatus Status { get; set; }
    public string HangfireJobId { get; set; }
    public int WarnMessageId { get; set; }
}