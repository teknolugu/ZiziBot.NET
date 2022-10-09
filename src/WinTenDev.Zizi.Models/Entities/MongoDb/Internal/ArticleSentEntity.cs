using System;
using MongoDB.Bson;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

[Collection("ArticleSent")]
public class ArticleSentEntity : IEntity, ICreatedOn
{
    public string ID { get; set; }
    public long ChatId { get; set; }
    public string RssSource { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public DateTime PublishDate { get; set; }
    public string Author { get; set; }
    public DateTime CreatedOn { get; set; }

    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();
}