using System.Threading.Tasks;

namespace WinTenDev.Zizi.Models.Interfaces;

public interface IChatService
{
    Task RegisterChatHealth();
    // Task ChatCleanUp();
    // Task AdminCheckerJobAsync(long chatId);
}