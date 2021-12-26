using System.Collections.Generic;
using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.App.Handlers.Core
{
    public class CommandHandler : ZiziEventHandler
    {
        private readonly ILogger<CommandHandler> _logger;
        private readonly PrivilegeService _privilegeService;

        public CommandHandler(
            ILogger<CommandHandler> logger,
            PrivilegeService privilegeService
        )
        {
            _logger = logger;
            _privilegeService = privilegeService;
        }

        [Command("reloadcmd", CommandParseMode.Both)]
        public async Task ReloadCommand()
        {
            await DeleteMessageAsync();
            if (!_privilegeService.IsFromSudo(FromId))
            {
                _logger.LogInformation("This command is for Sudo only");
                return;
            }

            await SendMessageTextAsync("Sedang mendaftarkan perintah.");
            await Bot.SetMyCommandsAsync(new List<BotCommand>()
            {
                new()
                {
                    Command = "ping",
                    Description = "Mengecek kesehatan Zizi"
                },
                new BotCommand()
                {
                    Command = "start",
                    Description = "Memulai menggunakan Zizi"
                }
            });

            await EditMessageTextAsync("Perintah berhasil di daftarkan");
            await DeleteMessageAsync(delaySecond: 3);
        }

        [Command("deletecmd", CommandParseMode.Both)]
        public async Task DeleteCommand()
        {
            await DeleteMessageAsync();
            if (!_privilegeService.IsFromSudo(FromId))
            {
                _logger.LogInformation("This command is for Sudo only");
                return;
            }

            await SendMessageTextAsync("Sedang menghapus perintah.");

            await Bot.DeleteMyCommandsAsync();

            await EditMessageTextAsync("Perintah berhasil di hapus");
            await DeleteMessageAsync(delaySecond: 3);
        }
    }
}