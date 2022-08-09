using System;
using MongoDB.Bson;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

public class UserInfo : IEntity, ICreatedOn
{
    public string ID { get; set; }
    public long UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string LangCode { get; set; }
    public DateTime CreatedOn { get; set; }

    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();
}