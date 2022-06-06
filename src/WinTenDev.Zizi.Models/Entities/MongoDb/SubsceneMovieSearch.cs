using System;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb;

[Collection("SubsceneSubtitleSearch")]
public class SubsceneMovieSearch : Entity, ICreatedOn, IModifiedOn
{
    public string MovieUrl { get; set; }
    public string MovieName { get; set; }
    public string SubtitleCount { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}
