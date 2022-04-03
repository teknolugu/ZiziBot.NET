using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Services.Extensions;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class EpicFreeGamesCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly EpicGamesService _epicGamesService;

    public EpicFreeGamesCommand(
        TelegramService telegramService,
        EpicGamesService epicGamesService
    )
    {
        _telegramService = telegramService;
        _epicGamesService = epicGamesService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var offeredGameList = await _epicGamesService.GetFreeGamesParsed();

        var listAlbum = offeredGameList.Select(
                item =>
                    new InputMediaPhoto(item.Images.ToString())
                    {
                        Caption = item.Detail,
                        ParseMode = ParseMode.Html
                    }
            )
            .Cast<IAlbumInputMedia>()
            .ToList();

        await _telegramService.SendMediaGroupAsync(
            new MessageResponseDto()
            {
                ListAlbum = listAlbum,
                ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(1),
                IncludeSenderForDelete = true
            }
        );

        var listGames = offeredGameList.Select(parsed => parsed.Detail).JoinStr("\n\n");

        await _telegramService.SendMessageTextAsync(
            new MessageResponseDto()
            {
                MessageText = $"EGS Free\n\n" +
                              $"{listGames}" +
                              $"\n",
                DisableWebPreview = true,
                ScheduleDeleteAt = DateTime.UtcNow.AddMinutes(1),
                IncludeSenderForDelete = true
            }
        );
    }
}