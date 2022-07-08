using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.ShalatTime
{
    public class ShalatTimeCommand : CommandBase
    {
        private readonly TelegramService _telegramService;

        public ShalatTimeCommand(TelegramService telegramService)
        {
            _telegramService = telegramService;
        }

        public override async Task HandleAsync(
            IUpdateContext context,
            UpdateDelegate next,
            string[] args
        )
        {
            await _telegramService.AddUpdateContext(context);

            await _telegramService.GetShalatTimeAsync();
        }
    }
}
