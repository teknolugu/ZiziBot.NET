using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class TranslateCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public TranslateCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var message = _telegramService.Message;
        var userLang = message.From.LanguageCode;

        if (message.ReplyToMessage == null)
        {
            var hint = await "Balas pesan yang ingin anda terjemahkan".GoogleTranslatorAsync(userLang);
            await _telegramService.SendTextMessageAsync(hint);

            return;
        }

        var param = message.Text.SplitText(" ").ToArray();
        var param1 = param.ValueOfIndex(1) ?? "";

        if (param1.IsNullOrEmpty())
        {
            param1 = message.From.LanguageCode;
        }

        var forTranslate = message.ReplyToMessage.Text ?? message.ReplyToMessage.Caption;

        Log.Information("Param: {0}", param.ToJson(true));

        await _telegramService.SendTextMessageAsync("🔄 Translating into Your language..");
        try
        {
            var translate = await forTranslate.GoogleTranslatorAsync(param1);

            // var translate = await forTranslate.TranslateAsync(param1);

            // var translate = forTranslate.TranslateTo(param1);

            // var translateResult = new StringBuilder();
            // foreach (var translation in translate.Result.Translations)
            // {
            // translateResult.AppendLine(translation._Translation);
            // }

            // var translateResult = translate.MergedTranslation;

            await _telegramService.EditMessageTextAsync(translate);
        }
        catch (Exception ex)
        {
            Log.Error(ex.Demystify(), "Error translation");

            var messageError = "Error translation" +
                               $"\nMessage: {ex.Message}";
            await _telegramService.EditMessageTextAsync(messageError);
        }
    }
}