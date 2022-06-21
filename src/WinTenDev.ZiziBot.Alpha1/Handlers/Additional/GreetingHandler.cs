using System.Threading.Tasks;
using BotFramework.Attributes;
using WinTenDev.ZiziBot.Alpha1.Handlers.Core;

namespace WinTenDev.ZiziBot.Alpha1.Handlers.Additional
{
    public class GreetingHandler : ZiziEventHandler
    {
        [Message("hontoni")]
        public async Task Greetings()
        {
            await SendMessageTextAsync("Yokatta");
        }
    }
}