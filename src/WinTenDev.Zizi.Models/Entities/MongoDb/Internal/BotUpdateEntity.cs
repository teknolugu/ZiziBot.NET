using System;
using MongoDB.Bson;
using MongoDB.Entities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

[Collection("BotUpdate")]
public class BotUpdateEntity : IEntity, ICreatedOn
{
    public string BotName { get; set; }
    public long UpdateId { get; set; }
    public UpdateType UpdateType { get; set; }
    public long ChatId { get; set; }
    public long UserId { get; set; }
    public Update Update { get; set; }

    public string ID { get; set; }
    public DateTime CreatedOn { get; set; }

    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();
}