using System;
using MongoDB.Bson;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal.Games;

[Collection("Game_Session_Chat_TebakKata")]
public class SessionChatTebakKataEntity : IEntity, ICreatedOn, IModifiedOn
{
    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();

    public string ID { get; set; }
    public string GameId { get; set; }

    public long ChatId { get; set; }
    public long UserId { get; set; }

    public One<GameEntity> Game { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}