using System.Linq;
using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using BotFramework.Utils;
using Microsoft.Extensions.Logging;
using SerilogTimings;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.ZiziBot.App.Handlers.Core;

namespace WinTenDev.ZiziBot.App.Handlers.Group
{
    public class AdminHandler : ZiziEventHandler
    {
        private readonly PrivilegeService _privilegeService;
        private readonly ILogger<AdminHandler> _logger;

        public AdminHandler(
            PrivilegeService privilegeService,
            ILogger<AdminHandler> logger
        )
        {
            _privilegeService = privilegeService;
            _logger = logger;
        }

        [Command(InChat.Public, "adminlist", CommandParseMode.Both)]
        public async Task AdminList()
        {
            using var operation = Operation.Begin("Ping command handler");

            await SendMessageTextAsync("Sedang mengambil data..");

            _logger.LogDebug("Loading chat admin on '{ChatId}'..", ChatId);

            var administrators = await _privilegeService.GetChatAdministratorsAsync(ChatId);
            var creatorGroup = administrators.FirstOrDefault(member => member.Status == ChatMemberStatus.Creator);
            var adminsGroup = administrators.Where(member => member.Status != ChatMemberStatus.Creator).ToList();

            _logger.LogDebug("Parsing admin lists..");
            var htmlMessage = new HtmlString();

            if (creatorGroup != null)
            {
                htmlMessage.Bold("👤 Creator").Br();
                htmlMessage.UserMention(creatorGroup.User).Br().Br();
            }

            htmlMessage.Bold("👥 Administrators").Br();

            var lastItem = adminsGroup.Last();
            foreach (var admin in adminsGroup)
            {
                htmlMessage.UserMention(admin.User);

                if (admin != lastItem) htmlMessage.Br();
            }

            await EditMessageTextAsync(htmlMessage);

            operation.Complete();
        }
    }
}