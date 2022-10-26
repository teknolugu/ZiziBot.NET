using System;
using System.Threading.Tasks;
using MongoDB.Entities;
using MoreLinq;
using Serilog;
using WinTenDev.Zizi.Models.Entities.MongoDb.Internal.Games;

namespace WinTenDev.Zizi.Services.Extensions;

public static class TelegramServiceGamesExtension
{
    public static async Task RunGameAsync(this TelegramService ts)
    {
        var tebakKataService = ts.GetRequiredService<TebakKataService>();
        var textMessage = ts.MessageOrEditedText;

        if (textMessage.IsNullOrEmpty())
        {
            Log.Information("Message not contains a Text");

            return;
        }
        var checkAnswer = await tebakKataService.CheckAnswerAsync(textMessage);

        if (checkAnswer)
        {
            await ts.SendTextMessageAsync(
                sendText: "Benar",
                scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
                includeSenderMessage: true
            );
        }
    }

    public static async Task StartGameAsync(this TelegramService ts)
    {
        var tebakKataService = ts.GetRequiredService<TebakKataService>();
        var startResult = await tebakKataService.StartGameAsync(new GameStartSessionDto()
        {
            ChatId = ts.ChatId,
            GameName = "Tebak Kata",
            SessionChat = new SessionChatTebakKataEntity()
            {
                ChatId = ts.ChatId,
                UserId = ts.FromId,
                Game = await DB.Find<GameEntity>()
                    .Match(entity => entity.Name == "Tebak Kata")
                    .ExecuteFirstAsync(),
            }
        });

        var tebakKataQuestion = await tebakKataService.GetQuestionAsync();

        var htmlMessage = HtmlMessage.Empty
            .Bold("Tebak Kata").Br()
            .Text(tebakKataQuestion.Question);

        var sessionTebakKataAnswer = await tebakKataService.GetAnswerSessionAsync(tebakKataQuestion.ID);

        sessionTebakKataAnswer.ForEach((entity,
            index) => htmlMessage.Br().Text($"{index + 1}. {entity.Answer.RegexReplace("[a-zA-Z]", "_")}"));

        await ts.SendTextMessageAsync(
            sendText: htmlMessage.ToString(),
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
            includeSenderMessage: true
        );
    }

    public static async Task EndGameAsync(this TelegramService ts)
    {
        var delete = await DB.DeleteAsync<SessionChatTebakKataEntity>(entity => entity.ChatId == ts.ChatId);

        var a = delete.DeletedCount > 0 ? "Game berhasil di akhiri" : "Sepertinya game belum dimulai";

        await ts.SendTextMessageAsync(
            sendText: a,
            scheduleDeleteAt: DateTime.UtcNow.AddMinutes(1),
            includeSenderMessage: true
        );
    }
}