using System;
using System.Threading.Tasks;
using EasyCaching.Core;
using LiteDB.Async;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziMirror.AppHost.Handlers.Commands
{
    public class AuthorizeCommand : CommandBase
    {
        private TelegramService _telegramService;
        private readonly LiteDatabaseAsync _liteDb;
        private readonly IEasyCachingProvider _easyCachingProvider;
        private readonly AuthService _authService;

        public AuthorizeCommand(
            LiteDatabaseAsync liteDb,
            IEasyCachingProvider easyCachingProvider,
            AuthService authService
        )
        {
            _easyCachingProvider = easyCachingProvider;
            _authService = authService;
            _liteDb = liteDb;
        }

        public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
        {
            await _telegramService.AddUpdateContext(context);

            var fromId = _telegramService.FromId;
            var chatId = _telegramService.ChatId;

            if (await _authService.IsAuth(chatId))
            {
                await _telegramService.SendTextMessageAsync("Chat has been authorized!");
                return;
            }

            if (!_telegramService.IsFromSudo)
            {
                Log.Information("User ID: {0} isn't sudo!", fromId);

                await _telegramService.SendTextMessageAsync("You can't authorize this chat!");
                return;
            }

            await _telegramService.SendTextMessageAsync("Authorizing chat..");

            await _authService.SaveAuth(new AuthorizedChat()
            {
                ChatId = chatId,
                AuthorizedBy = fromId,
                IsAuthorized = true,
                CreatedAt = DateTime.Now
            });

            await _telegramService.EditMessageTextAsync("Chat has been authorized!");
        }
    }
}