using System;
using MongoDB.Bson;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal.Games;

[Collection("Game_Session_Answer_TebakKata")]
public class SessionAnswerTebakKataEntity : IEntity, ICreatedOn, IModifiedOn
{
    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();

    public string ID { get; set; }

    public string Answer { get; set; }
    public long AnsweredBy { get; set; }
    public string GameId { get; set; }

    public One<GameEntity> Game { get; set; }
    public One<TebakKataQuestionEntity> Question { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}