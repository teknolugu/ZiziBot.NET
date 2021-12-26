using System.Linq;
using System.Text;
using Newtonsoft.Json;
using WinTenDev.WebHook.AppHost.Models.Github;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.WebHook.AppHost.Services
{
    public class GitHubService
    {
        public string ExecuteAsync(string json)
        {
            var msgSb = new StringBuilder();

            var githubRoot = JsonConvert.DeserializeObject<GithubRoot>(json);

            if (githubRoot == null) return null;

            if (githubRoot.Action.IsNotNullOrEmpty())
            {
                msgSb = ParseStargazers(githubRoot);
            }
            else if (githubRoot.Zen.IsNotNullOrEmpty())
            {
                var zen = githubRoot.Zen;

                msgSb.AppendLine(zen);
            }
            else if (githubRoot.Pusher != null)
            {
                msgSb = ParseCommit(githubRoot);
            }
            else
            {
                msgSb.AppendLine("Undetected Hook");
            }

            var msgText = msgSb.ToTrimmedString();

            return msgText;
        }

        private StringBuilder ParseStargazers(GithubRoot githubRoot)
        {
            var msgSb = new StringBuilder();

            var actionStr = githubRoot.Action;

            switch (actionStr)
            {
                case "created":
                case "started":
                case "deleted":
                    msgSb.AppendLine($"Someone {actionStr} repo");
                    break;
            }

            return msgSb;
        }

        private StringBuilder ParseCommit(GithubRoot githubRoot)
        {
            var msgSb = new StringBuilder();
            var commitStr = githubRoot.Commits.Select((commit, i) => {

                var commitStr = commit.Message;
                var commitUrl = commit.Url;
                var commitHref = $"<a href='{commitUrl}'>{commitStr}</a>";

                return commitHref;
            }).JoinStr("\n\n");

            msgSb.AppendLine("Someone push");
            msgSb.AppendLine(commitStr);

            return msgSb;
        }
    }
}