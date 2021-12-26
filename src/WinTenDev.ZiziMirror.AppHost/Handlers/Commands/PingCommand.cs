using System;
using System.Threading.Tasks;
using EasyCaching.Core;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziMirror.AppHost.Handlers.Commands
{
    public class PingCommand : CommandBase
    {
        private TelegramService _telegramService;
        private readonly IEasyCachingProvider _easyCachingProvider;

        public PingCommand(IEasyCachingProvider easyCachingProvider)
        {
            _easyCachingProvider = easyCachingProvider;
        }

        public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
        {
            await _telegramService.AddUpdateContext(context);
            var message = _telegramService.Message;

            await _easyCachingProvider.SetAsync($"message{message.MessageId}", message, TimeSpan.FromMinutes(10));

            await _telegramService.SendTextMessageAsync("Pong!");
        }
    }
}