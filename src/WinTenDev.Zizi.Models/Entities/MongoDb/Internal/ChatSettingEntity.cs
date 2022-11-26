using System;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

[Collection("ChatSetting")]
public class ChatSettingEntity : IEntity, ICreatedOn, IModifiedOn
{
    public string ID { get; set; }
    public long ChatId { get; set; }
    public long MemberCount { get; set; }

    public long TopicWelcome { get; set; }
    public long TopicRss { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }

    public string GenerateNewID()
    {
        return ChatId.ToString();
    }
}