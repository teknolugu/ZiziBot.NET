using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using WinTenDev.ZiziBot.App.Handlers.Core;

namespace WinTenDev.ZiziBot.App.Handlers.Group
{
    public class PromoteHandler : ZiziEventHandler
    {
        public PromoteHandler()
        {
        }

        [Command(InChat.Public, "promote", CommandParseMode.Both)]
        public async Task PromoteAsync()
        {
            if (ReplyToMessage == null)
            {
                await SendMessageTextAsync("Balasa seseorang yang mau di promosikan");
                return;
            }

            var userId = ReplyToMessage.From.Id;

            await SendMessageTextAsync($"Sedang mempromosikan {userId}..");

            var result = await PromoteMember(userId, true);
            if (result.IsSuccess)
            {
                await EditMessageTextAsync($"{userId} berhasil di promosikan.");
            }
            else
            {
                await EditMessageTextAsync(result.Exception.Message);
            }


        }

        [Command(InChat.Public, "demote", CommandParseMode.Both)]
        public async Task DemoteAsync()
        {
            if (ReplyToMessage == null)
            {
                await SendMessageTextAsync("Balasa seseorang yang mau di promosikan");
                return;
            }

            var userId = ReplyToMessage.From.Id;

            await SendMessageTextAsync($"Sedang mendemosikan {userId}..");

            var result = await PromoteMember(userId, false);
            if (result.IsSuccess)
            {
                await EditMessageTextAsync($"{userId} berhasil di demosikan.");
            }
            else
            {
                await EditMessageTextAsync(result.Exception.Message);
            }
        }
    }
}