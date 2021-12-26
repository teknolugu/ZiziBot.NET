using System.Threading.Tasks;

namespace WinTenDev.Zizi.Models.Interfaces;

public interface IRssFeedService
{
    Task RegisterScheduler();
    Task<int> ExecuteUrlAsync(long chatId, string rssUrl);
}