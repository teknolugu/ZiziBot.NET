using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class RandomCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public RandomCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);
        var parts = _telegramService.MessageTextParts.Skip(1).ToList();

        if (!parts.Any())
        {
            await _telegramService.SendTextMessageAsync(
                "\nRandom angka dari X sampai Y sebanyak Z kali\n" +
                "\nPola: <code>/ran X Y [*X]</code>" +
                "\nContoh: <code>/ran 1 20 *5</code>"
            );

            return;
        }

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("<b>Random Angka</b>").AppendLine();

        var range1 = parts.ElementAtOrDefault(0).ToInt();
        int range2;
        int multiplier;

        if (parts.ElementAtOrDefault(1)?.Contains("*") ?? false)
        {
            range1 = 0;
            range2 = parts.ElementAtOrDefault(0).ToInt();
            multiplier = parts.ElementAtOrDefault(1)?.Remove(0, 1).ToInt() ?? 0;
        }
        else
        {
            range1 = parts.ElementAtOrDefault(0).ToInt();
            range2 = parts.ElementAtOrDefault(1).ToInt();
            multiplier = parts.ElementAtOrDefault(2)?.Remove(0, 1).ToInt() ?? 0;
        }

        if (range2 == 0)
        {
            range2 = range1;
            range1 = 1;
        }

        if (multiplier > 0)
        {
            Enumerable.Range(1, multiplier).ForEach(
                number => messageBuilder.AppendLine($"Angka {number}: " + NumberUtil.RandomInt(range1, range2))
            );
        }
        else
        {
            messageBuilder.Append("Hasil: " + NumberUtil.RandomInt(range1, range2));
        }

        await _telegramService.SendTextMessageAsync(messageBuilder.ToTrimmedString());
    }
}