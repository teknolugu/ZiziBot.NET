using System;
using MongoDB.Bson;
using MongoDB.Entities;
using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

[Collection("StepHistory")]
public class StepHistoryEntity : IEntity, ICreatedOn, IModifiedOn
{
    public string GenerateNewID() => ObjectId.GenerateNewId().ToString();

    public string ID { get; set; }
    public StepHistoryName Name { get; set; }
    public long ChatId { get; set; }
    public long UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Reason { get; set; }
    public StepHistoryStatus Status { get; set; }
    public string HangfireJobId { get; set; }
    public int WarnMessageId { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}