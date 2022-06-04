using System.Threading.Tasks;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.Zizi.Services.Extensions;

internal static class StartCommandExtension
{
    public static async Task<MessageResponseDto> OnStartSubsceneDownloadAsync(
        this TelegramService telegramService,
        string subtitleSlug
    )
    {
        var response = new MessageResponseDto();

        var subsceneService = telegramService.GetRequiredService<SubsceneService>();
        var fixedSlug = subtitleSlug.Replace("=", "/");

        await telegramService.SendTextMessageAsync(
            "Subtitle sedang didownload." +
            "\nSilahkan tunggu beberapa saat.."
        );

        var subtitleFileAsync = await subsceneService.GetSubtitleFileAsync(fixedSlug);
        var fromId = telegramService.FromId;

        await telegramService.DeleteSentMessageAsync();

        await telegramService.SendMediaAsync(
            fileId: subtitleFileAsync,
            mediaType: MediaType.LocalDocument,
            caption: "Subtitle"
        );

        return response;
    }
}
