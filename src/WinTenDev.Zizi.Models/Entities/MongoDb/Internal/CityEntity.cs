using System;
using MongoDB.Bson;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

[Collection("City")]
public class CityEntity : IEntity, ICreatedOn, IModifiedOn
{
    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();

    public string ID { get; set; }
    public long UserId { get; set; }
    public long ChatId { get; set; }
    public long CityId { get; set; }
    public string CityName { get; set; }
    public bool EnableNotification { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}