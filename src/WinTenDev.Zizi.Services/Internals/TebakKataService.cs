using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;
using WinTenDev.Zizi.Models.Entities.MongoDb.Internal.Games;

namespace WinTenDev.Zizi.Services.Internals;

public class TebakKataService
{
    private readonly ILogger<TebakKataService> _logger;

    public TebakKataService(ILogger<TebakKataService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> CheckAnswerAsync(string answer)
    {
        return await DB.Find<SessionAnswerTebakKataEntity>()
            .Match(x => x.Answer.ToLower() == answer)
            .ExecuteAnyAsync();
    }

    public async Task<TebakKataQuestionEntity> GetQuestionAsync()
    {
        var question = await DB.Find<TebakKataQuestionEntity>()
            .ExecuteFirstAsync();

        return question;
    }

    public async Task<List<SessionAnswerTebakKataEntity>> GetAnswerSessionAsync(string questionId)
    {
        var answers = await DB.Find<SessionAnswerTebakKataEntity>()
            // .Match(entity => entity.Question.ID == questionId)
            .ExecuteAsync();

        return answers
            .DistinctBy(entity => entity.Answer)
            .ToList();
    }

    public async Task<bool> StartGameAsync(GameStartSessionDto dto)
    {
        var checkSession = await DB.Find<SessionChatTebakKataEntity>()
            .Match(entity => entity.ChatId == dto.ChatId)
            .ExecuteAnyAsync();

        if (checkSession)
        {
            _logger.LogInformation("Game Tebak Kata Session already started for ChatId: {ChatId}", dto.ChatId);
            return true;
        }

        _logger.LogInformation("Starting Game Tebak Kata Session for ChatId: {ChatId}", dto.ChatId);
        await DB.SaveAsync(dto.SessionChat);

        // var games = await DB.Find<SessionAnswerTebakKataEntity>()
        //     .ExecuteAsync();


        // var a = await games.SelectAsync(entity => entity.Game.ToEntityAsync());
        // var game = a.FirstOrDefault(entity => entity.Name == dto.GameName);

        // var sess = games.Select(entity => {
        //     entity.Answer =
        // });

        _logger.LogDebug("Creating answer session for Game: {GameName}", dto.GameName);
        var tebakKataAnswer = await DB.Find<TebakKataAnswerEntity>()
            .ExecuteAsync();

        var sessionTebakKataAnswer = tebakKataAnswer
            .DistinctBy(entity => entity.Answer)
            .Select(entity => new SessionAnswerTebakKataEntity()
            {
                Answer = entity.Answer,
                Question = entity.Question,
                Game = entity.Game
            }).ToList();

        await DB.SaveAsync(sessionTebakKataAnswer);

        _logger.LogInformation("Game Tebak Kata Session started for ChatId: {ChatId}", dto.ChatId);
        return false;
    }
}