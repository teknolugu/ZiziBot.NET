using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
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

        await telegramService.SendTextMessageAsync("Subtitle sedang diproses, mohon tunggu...");

        await telegramService.SendChatActionAsync(ChatAction.UploadDocument);

        var movieDetail = await subsceneService.GetSubtitleFileAsync(fixedSlug);

        var subsceneUrl = "https://subscene.com" + movieDetail.SubtitleMovieUrl;
        var commentaryUrl = "https://subscene.com" + movieDetail.CommentaryUrl;
        var zipFileName = movieDetail.ReleaseInfos?
                              .OrderBy(s => s.Length).FirstOrDefault(movieDetail.MovieName)?
                              .Replace(".", " ") ??
                          movieDetail.MovieName + ".zip";

        var subtitleInfo = HtmlMessage.Empty
                .Bold("Movie: ").TextBr(movieDetail.MovieName, true)
                .Bold("Language: ").TextBr(movieDetail.Language)
                .Bold("Url: ").Url(subsceneUrl, "Subscene URL").Br()
                .Bold("Author: ").Url(commentaryUrl, movieDetail.CommentaryUser).Br()
                .BoldBr("Release info")
                .TextBr(movieDetail.ReleaseInfo, true)
                .Br()
                .Text(movieDetail.Comment)
            ;

        await telegramService.DeleteSentMessageAsync();

        await telegramService.SendMediaAsync(
            fileId: movieDetail.SubtitleDownloadUrl,
            mediaType: MediaType.Document,
            caption: subtitleInfo.ToString(),
            customFileName: zipFileName
        );

        return response;
    }
}
