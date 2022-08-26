using System;
using MongoDB.Bson;
using MongoDB.Entities;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

[Collection("ChatAdmin")]
public class GroupAdmin : IEntity, ICreatedOn
{
    public string ID { get; set; }
    public long ChatId { get; set; }
    public long UserId { get; set; }
    public ChatMemberStatus Role { get; set; }
    public DateTime CreatedOn { get; set; }

    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();
}