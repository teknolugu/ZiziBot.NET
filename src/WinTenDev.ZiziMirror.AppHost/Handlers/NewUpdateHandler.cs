using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziMirror.AppHost.Handlers
{
    public class NewUpdateHandler : IUpdateHandler
    {
        private TelegramService _telegramService;

        public NewUpdateHandler()
        {
        }

        public async Task HandleAsync(IUpdateContext context, UpdateDelegate next, CancellationToken cancellationToken)
        {
            await _telegramService.AddUpdateContext(context);

            Log.Debug("New Update..");

            await next(context, cancellationToken);
        }
    }
}