using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb;

public class SubsceneSubtitleItem : IEntity, ICreatedOn, IModifiedOn
{
    [ObjectId, BsonId]
    public string ID { get; set; }
    public string MovieUrl { get; set; }
    public string MovieName { get; set; }
    public string Language { get; set; }
    public string Owner { get; set; }
    public string Comment { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }

    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();
}