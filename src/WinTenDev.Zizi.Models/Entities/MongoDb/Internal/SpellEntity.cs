using System;
using MongoDB.Bson;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

[Collection("Spell")]
public class SpellEntity : IEntity, ICreatedOn
{
    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();

    public string ID { get; set; }
    public string Typo { get; set; }
    public string Fix { get; set; }
    public long FromId { get; set; }
    public long ChatId { get; set; }

    public DateTime CreatedOn { get; set; }
}