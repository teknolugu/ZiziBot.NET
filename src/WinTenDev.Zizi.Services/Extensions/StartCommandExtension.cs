using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Dto;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.IO;

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

        Task.Run(
            async () => {
                await telegramService.SendTextMessageAsync("Subtitle sedang diproses, mohon tunggu...");
                await telegramService.SendChatActionAsync(ChatAction.UploadDocument);

                var movieDetail = await subsceneService.GetSubtitleFileAsync(fixedSlug);

                var subsceneUrl = "https://subscene.com" + movieDetail.SubtitleMovieUrl;
                var commentaryUrl = "https://subscene.com" + movieDetail.CommentaryUrl;
                var serverFileName = await movieDetail.SubtitleDownloadUrl.GetServerFileName();
                var fileName = movieDetail.ReleaseInfos?
                                   .OrderBy(s => s.Length).FirstOrDefault(movieDetail.MovieName)?
                                   .Replace(".", " ") ??
                               movieDetail.MovieName;
                var fileNameWithExt = fileName + serverFileName.GetFileExtension();

                var subtitleInfo = HtmlMessage.Empty
                    .Bold("Movie: ").TextBr(movieDetail.MovieName, true)
                    .Bold("Language: ").TextBr(movieDetail.Language)
                    .Bold("Author: ").Url(commentaryUrl, movieDetail.CommentaryUser).Br();

                if (movieDetail.ReleaseInfo.IsNotNullOrEmpty())
                {
                    subtitleInfo.BoldBr("Release info")
                        .TextBr(movieDetail.ReleaseInfo, true);
                }

                if (movieDetail.Comment.IsNotNullOrEmpty())
                {
                    subtitleInfo.Br()
                        .Text(movieDetail.Comment);
                }

                var buttonMarkup = new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("Tautan Subscene", subsceneUrl),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Pencarian baru", "subscene ")
                    }
                };

                var replyMarkup = new InlineKeyboardMarkup(buttonMarkup);

                await telegramService.DeleteSentMessageAsync();

                await telegramService.SendMediaAsync(
                    fileId: movieDetail.SubtitleDownloadUrl,
                    mediaType: MediaType.Document,
                    caption: subtitleInfo.ToString(),
                    customFileName: fileNameWithExt,
                    replyMarkup: replyMarkup
                );
            }
        ).InBackground();

        return response;
    }
}
