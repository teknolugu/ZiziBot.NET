namespace WinTenDev.Zizi.Utils.Parsers;

public static class GithubUtil
{
    public static bool IsGithubReleaseUrl(this string url)
    {
        return url.Contains("github.com") && url.Contains("releases");
    }

    public static bool IsGithubCommitsUrl(this string url)
    {
        return url.Contains("github.com") && url.Contains("commits");
    }
}
