using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WinTenDev.WebHook.AppHost.Models.Github
{
    public partial class Commit
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("tree_id")]
        public string TreeId { get; set; }

        [JsonProperty("distinct")]
        public bool Distinct { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("author")]
        public Author Author { get; set; }

        [JsonProperty("committer")]
        public Author Committer { get; set; }

        [JsonProperty("added")]
        public List<string> Added { get; set; }

        [JsonProperty("removed")]
        public List<object> Removed { get; set; }

        [JsonProperty("modified")]
        public List<string> Modified { get; set; }
    }
}