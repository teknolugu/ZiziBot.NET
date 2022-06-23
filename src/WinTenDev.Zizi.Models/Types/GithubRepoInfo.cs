using System;
using System.Collections.Generic;
using Octokit;

namespace WinTenDev.Zizi.Models.Types;

public class GithubRepoInfo
{
    public string Owner { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public string Homepage { get; set; }
    public string Language { get; set; }
    public int ForksCount { get; set; }
    public int StargazersCount { get; set; }
    public int WatchersCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PushedAt { get; set; }
    public LicenseMetadata License { get; set; }
    public IReadOnlyList<string> Topics { get; set; }
    public IReadOnlyList<Release> Releases { get; set; }
    public IReadOnlyList<GitHubCommit> Commits { get; set; }
    public IReadOnlyList<Issue> Issues { get; set; }
    public IReadOnlyList<PullRequest> PullRequests { get; set; }
}
