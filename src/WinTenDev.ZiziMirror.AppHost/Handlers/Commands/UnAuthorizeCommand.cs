using System;
using System.Threading.Tasks;
using LiteDB.Async;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziMirror.AppHost.Handlers.Commands
{
    public class UnAuthorizeCommand : CommandBase
    {
        private TelegramService _telegramService;
        private readonly AppConfig _appConfig;
        private LiteDatabaseAsync _liteDb;
        private readonly AuthService _authService;

        public UnAuthorizeCommand(
            AppConfig appConfig,
            LiteDatabaseAsync liteDb,
            AuthService authService
        )
        {
            _appConfig = appConfig;
            _authService = authService;
            _liteDb = liteDb;
        }

        public override async Task HandleAsync(IUpdateContext context,
            UpdateDelegate next,
            string[] args)
        {
            await _telegramService.AddUpdateContext(context);

            var fromId = _telegramService.FromId;
            var chatId = _telegramService.ChatId;

            if (!await _authService.IsAuth(chatId))
            {
                await _telegramService.SendTextMessageAsync("Chat hasn't authorized!");
                return;
            }

            if (!_telegramService.IsFromSudo)
            {
                Log.Information("User ID: {0} isn't sudo!", fromId);

                await _telegramService.SendTextMessageAsync("You can't change authorization for this chat!");
                return;
            }

            await _telegramService.SendTextMessageAsync("UnAuthorizing chat..");

            await _authService.SaveAuth(new AuthorizedChat()
            {
                ChatId = chatId,
                AuthorizedBy = fromId,
                IsAuthorized = false,
                CreatedAt = DateTime.Now
            });

            await _telegramService.EditMessageTextAsync("Chat has been UnAuthorized!");
        }
    }
}