using System;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb;

public class SubsceneMovieItem : Entity, ICreatedOn, IModifiedOn
{
    public string MovieUrl { get; set; }
    public string MovieName { get; set; }
    public string Language { get; set; }
    public string Owner { get; set; }
    public string UploadDate { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}
