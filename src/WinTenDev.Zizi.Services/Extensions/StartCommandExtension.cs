using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Services.Google;
using YoutubeExplode.Videos.Streams;

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

                var movieDetail = await subsceneService.GetSubtitleFileAsync(fixedSlug);

                if (movieDetail.SubtitleDownloadUrl == null)
                {
                    var htmlMessage = HtmlMessage.Empty
                        .BoldBr("Pengunduhan Subtitle")
                        .Bold("Pesan: ").TextBr("Terjadi kesalahan ketika mengunduh Subtitle, silakan hubungi Administrator")
                        .Bold("Url: ").TextBr(movieDetail.SubtitleMovieUrl);

                    await telegramService.EditMessageTextAsync(
                        sendText: htmlMessage.ToString(),
                        scheduleDeleteAt: DateTime.UtcNow.AddMinutes(10),
                        includeSenderMessage: true
                    );

                    return;
                }

                await telegramService.SendChatActionAsync(ChatAction.UploadDocument);

                var subsceneUrl = "https://subscene.com" + movieDetail.SubtitleMovieUrl;
                var commentaryUrl = "https://subscene.com" + movieDetail.CommentaryUrl;
                var subtitleDownloadUrl = movieDetail.SubtitleDownloadUrl;
                var serverFileName = await subtitleDownloadUrl.GetServerFileName();
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
                    fileId: subtitleDownloadUrl,
                    mediaType: MediaType.Document,
                    caption: subtitleInfo.ToString(),
                    customFileName: fileNameWithExt,
                    replyMarkup: replyMarkup
                );
            }
        ).InBackground();

        return response;
    }

    public static async Task<MessageResponseDto> OnStartYoutubeDownloadAsync(
        this TelegramService telegramService
    )
    {
        var responseDto = new MessageResponseDto();

        var youtubeService = telegramService.GetRequiredService<YoutubeService>();
        var googleApiService = telegramService.GetRequiredService<GoogleApiService>();

        var startCmdParse = telegramService.GetStartCommand();
        var videoUrl = startCmdParse.StartArgs.LastOrDefault();

        var htmlMessage = HtmlMessage.Empty
            .Bold("Youtube Downloader").Br()
            .Bold("URL: ").Text(videoUrl);

        await telegramService.AppendTextAsync(htmlMessage.ToString());

        await telegramService.AppendTextAsync("Mendapatkan informasi video...");
        var streamManifest = await youtubeService.GetStreamManifestAsync(videoUrl);
        var videoManifest = await youtubeService.GetVideoManifestAsync(videoUrl);

        var streamInfo = streamManifest
            .GetAudioOnlyStreams()
            .GetWithHighestBitrate();

        var ext = streamInfo.Container.Name;

        await telegramService.AppendTextAsync("Sedang mengunduh berkas..");

        var fileName = videoManifest.Title + "." + streamInfo.Container;
        var filePath = await youtubeService.DownloadStreamAsync(streamInfo, fileName);
        var locationOnDrive = "public/youtube";

        await telegramService.AppendTextAsync("Sedang mengunggah ke Drive..");

        var pathOnDrive = await googleApiService.UploadFileToDrive(
            parentId: "default",
            sourceFile: filePath,
            locationPath: locationOnDrive,
            preventDuplicate: true
        );

        await telegramService.AppendTextAsync("Selesai..");

        await telegramService.SendChatActionAsync(ChatAction.UploadDocument);
        await telegramService.SendMediaAsync(filePath, MediaType.LocalDocument, htmlMessage.ToString());

        return responseDto;
    }
}