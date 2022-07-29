using System;

namespace WinTenDev.Zizi.Models.Dto;

public class SubsceneTitleDto
{
    public string ID { get; set; }
    public string MovieSlug { get; set; }
    public string MovieUrl { get; set; }
    public string MovieName { get; set; }
    public string SubtitleCount { get; set; }
    public DateTime CreatedOn { get; set; }
}