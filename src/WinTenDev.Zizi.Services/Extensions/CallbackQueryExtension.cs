using System.Threading.Tasks;
using Serilog;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.Zizi.Services.Extensions;

public static class CallbackQueryExtension
{
    public static async Task<bool> OnCallbackPingAsync(this TelegramService telegramService)
    {
        Log.Information("Receiving Ping callback");
        var callbackQuery = telegramService.CallbackQuery;

        var callbackData = callbackQuery.Data;
        Log.Debug("CallbackData: {CallbackData}", callbackData);

        var answerCallback = $"Callback: {callbackData}";

        await telegramService.AnswerCallbackQueryAsync(answerCallback, showAlert: true);

        return true;
    }
}
