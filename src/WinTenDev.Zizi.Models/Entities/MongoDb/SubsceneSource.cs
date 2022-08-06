using System;
using MongoDB.Bson;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb;

public class SubsceneSource : IEntity, ICreatedOn
{
    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();

    public string ID { get; set; }
    public string SearchTitleUrl { get; set; }
    public string SearchSubtitleUrl { get; set; }
    public bool IsActive { get; set; }
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public DateTime CreatedOn { get; set; }
}