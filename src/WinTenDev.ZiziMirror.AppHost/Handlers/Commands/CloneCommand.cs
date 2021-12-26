using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziMirror.AppHost.Handlers.Commands
{
    public class CloneCommand : CommandBase
    {
        private TelegramService _telegramService;
        private readonly DriveService _driveService;

        public CloneCommand(DriveService driveService)
        {
            _driveService = driveService;
        }

        public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
        {
            await _telegramService.AddUpdateContext(context);

            var texts = _telegramService.MessageTextParts;
            var url = texts.ValueOfIndex(1);

            if (url == null)
            {
                await _telegramService.SendTextMessageAsync("Masukkan URL yang akan di clone!");
                return;
            }

            await _telegramService.SendTextMessageAsync("Cloning..");

            // await _telegramService.CloneLink(_driveService, url);
        }
    }
}