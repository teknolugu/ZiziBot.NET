using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using Google.Cloud.Vision.V1;
using WinTenDev.Zizi.Services.Google;
using WinTenDev.ZiziBot.App.Handlers.Core;

namespace WinTenDev.ZiziBot.App.Handlers.Additional
{
    /// <summary>
    /// This class is used to handle about OCR commands
    /// </summary>
    public class OcrHandler : ZiziEventHandler
    {
        private readonly GoogleApiService _googleApiService;

        /// <summary>
        /// Constructor of OcrHandler
        /// </summary>
        /// <param name="googleApiService"></param>
        public OcrHandler(
            GoogleApiService googleApiService
        )
        {
            _googleApiService = googleApiService;
        }

        /// <summary>
        /// Command handler to handle OCR commands
        /// </summary>
        [Command("ocr", CommandParseMode.Both)]
        public async Task CmdOcr()
        {
            if (ReplyToMessage == null)
            {
                await SendMessageTextAsync("Balas sebuah pesan untuk OCR");
                return;
            }

            var fileName = $"{ChatId}/ocr";
            var filePath = await DownloadFileAsync(fileName);

            var imageAnnotatorClient = _googleApiService.CreateImageAnnotatorClient();
            var image = await Image.FromFileAsync(filePath);
            var result = await imageAnnotatorClient.DetectTextAsync(image);

            if (result.Count == 0)
            {
                await SendMessageTextAsync("Tidak terdeteksi adanya teks di gambar ini.");
                return;
            }

            var strOcr = result[0].Description;
            await SendMessageTextAsync(strOcr);
        }
    }
}