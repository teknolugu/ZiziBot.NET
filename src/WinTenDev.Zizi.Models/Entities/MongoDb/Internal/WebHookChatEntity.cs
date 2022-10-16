using System;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

[Collection("WebHookChat")]
public class WebHookChatEntity : IEntity, ICreatedOn, IModifiedOn
{
    public string ID { get; set; }
    public long ChatId { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }

    public string GenerateNewID() => Guid.NewGuid().ToString("N").Substring(0, 12);
}