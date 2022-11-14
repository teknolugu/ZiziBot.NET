using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziMirror.AppHost.Handlers.Commands
{
    public class PingCommand : CommandBase
    {
        private TelegramService _telegramService;

        public PingCommand()
        {
        }

        public override async Task HandleAsync(IUpdateContext context,
            UpdateDelegate next,
            string[] args)
        {
            await _telegramService.AddUpdateContext(context);
            var message = _telegramService.Message;

            await _telegramService.SendTextMessageAsync("Pong!");
        }
    }
}