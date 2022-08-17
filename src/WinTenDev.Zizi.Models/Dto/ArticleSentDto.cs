using System;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

public class ArticleSentDto
{
    public long ChatId { get; set; }
    public string RssSource { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public DateTime PublishDate { get; set; }
    public string Author { get; set; }
}