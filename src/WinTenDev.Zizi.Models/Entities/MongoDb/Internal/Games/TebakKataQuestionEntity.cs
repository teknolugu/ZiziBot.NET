using System;
using MongoDB.Bson;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal.Games;

[Collection("Game_TebakKata_Question")]
public class TebakKataQuestionEntity : IEntity, ICreatedOn, IModifiedOn
{
    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();

    public string ID { get; set; }

    public string Question { get; set; }
    public string GameId { get; set; }

    public One<GameEntity> Game { get; set; }
    public Many<TebakKataAnswerEntity> Answers { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}