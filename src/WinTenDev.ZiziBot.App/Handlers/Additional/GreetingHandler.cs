using System.Threading.Tasks;
using BotFramework.Attributes;
using WinTenDev.ZiziBot.App.Handlers.Core;

namespace WinTenDev.ZiziBot.App.Handlers.Additional
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