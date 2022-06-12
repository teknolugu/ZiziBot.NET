using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb;

[Collection("SubsceneSubtitleSearch")]
public class SubsceneMovieSearch : IEntity, ICreatedOn, IModifiedOn
{
    [ObjectId, BsonId]
    public string ID { get; set; }
    public string MovieUrl { get; set; }
    public string MovieName { get; set; }
    public string SubtitleCount { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }

    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();
}