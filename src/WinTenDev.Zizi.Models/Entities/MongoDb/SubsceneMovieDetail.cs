using System;
using System.Collections.Generic;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Models.Entities.MongoDb;

public class SubsceneMovieDetail : Entity, ICreatedOn, IModifiedOn
{
    public string SubtitleMovieUrl { get; set; }
    public string SubtitleDownloadUrl { get; set; }
    public string MovieName { get; set; }
    public string Language { get; set; }
    public string ReleaseInfo { get; set; } = string.Empty;
    public List<string> ReleaseInfos { get; set; }
    public string Description { get; set; }
    public string Comment { get; set; }
    public string CommentaryUrl { get; set; }
    public string CommentaryUser { get; set; }
    public string PosterUrl { get; set; }
    public string ImdbUrl { get; set; }
    public string LocalFilePath { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}
