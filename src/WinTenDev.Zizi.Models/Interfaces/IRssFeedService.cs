using System.Threading.Tasks;

namespace WinTenDev.Zizi.Models.Interfaces;

public interface IRssFeedService
{
    Task RegisterJobAllRssScheduler();
    Task ExecuteUrlAsync(
        long chatId,
        string rssUrl
    );
}