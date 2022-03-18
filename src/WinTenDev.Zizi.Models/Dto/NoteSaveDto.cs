using System;

namespace WinTenDev.Zizi.Models.Dto;

public class NoteSaveDto
{
    public long ChatId { get; set; }
    public long FromId { get; set; }
    public string Tag { get; set; }
    public string Content { get; set; }
    public string BtnData { get; set; }
    public string TypeData { get; set; }
    public string FileId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}