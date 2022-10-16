using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

[Collection("ForceSubscription")]
public class ForceSubscriptionEntity : IEntity, ICreatedOn, IModifiedOn
{
    [ObjectId, BsonId]
    public string ID { get; set; }
    public long ChatId { get; set; }
    public long UserId { get; set; }
    public long ChannelId { get; set; }
    public string ChannelTitle { get; set; }
    public string InviteLink { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }

    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();
}