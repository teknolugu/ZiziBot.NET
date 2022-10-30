using System;
using MongoDB.Bson;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

[Collection("GlobalBan_User")]
public class GlobalBanUserEntity : IEntity, ICreatedOn, IModifiedOn
{
    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();

    public string ID { get; set; }

    public long ChatId { get; set; }
    public long UserId { get; set; }
    public long BannedUserId { get; set; }
    public string Reason { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}