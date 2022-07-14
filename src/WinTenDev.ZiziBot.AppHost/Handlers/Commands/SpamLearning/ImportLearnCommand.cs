using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.SpamLearning;

public class ImportLearnCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public ImportLearnCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var message = _telegramService.Message;
        var msgText = message.Text.Split(' ');
        var param1 = "\t";
        // var param1 = msgText.ValueOfIndex(1).Trim() ?? ",";

        if (!_telegramService.IsFromSudo)
        {
            Log.Information("This user is not sudoer");
            return;
        }

        if (message.ReplyToMessage != null)
        {
            await _telegramService.SendTextMessageAsync("Sedang mengimport dataset");

            var repMessage = message.ReplyToMessage;
            if (repMessage.Document != null)
            {
                var document = repMessage.Document;
                var chatId = message.Chat.Id.ToString().TrimStart('-');
                var msgId = repMessage.MessageId;
                var fileName = document.FileName;
                var filePath = $"learn-dataset-{chatId}-{msgId}-{fileName}";
                var savedFile = await _telegramService.DownloadFileAsync(filePath);

                // await _telegramService.ImportCsv(savedFile, param1);

                await _telegramService.EditMessageTextAsync("Sedang mempelajari dataset");
                await MachineLearning.SetupEngineAsync();

                await _telegramService.EditMessageTextAsync("Import selesai");
            }
            else
            {
                var typeHint = "File yang mau di import harus berupa dokumen bertipe csv, tsv atau sejenis";
                await _telegramService.SendTextMessageAsync(typeHint);
            }
        }
        else
        {
            await _telegramService.SendTextMessageAsync("Balas file yang mau di import");
        }
    }
}
