using System;

namespace WinTenDev.Zizi.Models.Dto;

public class SubsceneSubtitleDto
{
    public string ID { get; set; }
    public string MovieUrl { get; set; }
    public string MovieName { get; set; }
    public string Language { get; set; }
    public string Owner { get; set; }
    public string Comment { get; set; }
    public DateTime CreatedOn { get; set; }
}