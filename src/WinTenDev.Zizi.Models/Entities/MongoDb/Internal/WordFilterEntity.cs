using System;
using MongoDB.Bson;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

[Collection("WordFilter")]
public class WordFilterEntity : IEntity, ICreatedOn, IModifiedOn
{
    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();

    public string ID { get; set; }
    public long ChatId { get; set; }
    public long UserId { get; set; }
    public string Word { get; set; }
    public bool IsGlobal { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}