using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MongoDB.Entities;
using WinTenDev.Zizi.Models.Entities.MongoDb.Internal.Games;
using WinTenDev.Zizi.Utils.Providers;

namespace WinTenDev.Zizi.DbMigrations.MongoDBEntities;

[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase")]
public class _002_Game_Initialize_Tebak_Kata_Sample : IMigration
{

    public async Task UpgradeAsync()
    {
        var gameName = "Tebak Kata";

        var game = new GameEntity()
        {
            Name = gameName,
            IsRolledOut = false
        };

        await game.ExSaveAsync(entity => entity.Name == gameName);

        var foundGame = await DB.Find<GameEntity>()
            .Match(entity => entity.Name == gameName)
            .ExecuteFirstAsync();

        var tebakKataQuestionEntity = new TebakKataQuestionEntity
        {
            Question = "Apa warna langit?",
            Game = foundGame
        };
        await tebakKataQuestionEntity.ExSaveAsync(x => x.Question == tebakKataQuestionEntity.Question);

        var tebakKataQuestionEntity2 = new TebakKataQuestionEntity
        {
            Question = "Hewan berkaki 4?",
            Game = foundGame
        };
        await tebakKataQuestionEntity2.ExSaveAsync(x => x.Question == tebakKataQuestionEntity2.Question);

        var listAnswer = new List<TebakKataAnswerEntity>
        {
            new TebakKataAnswerEntity()
            {
                Answer = "Biru",
                Game = foundGame
            },
            new TebakKataAnswerEntity()
            {
                Answer = "Merah",
                Game = foundGame
            },
        };

        await DB.SaveAsync(listAnswer);


        var listAnswer2 = new List<TebakKataAnswerEntity>
        {
            new TebakKataAnswerEntity()
            {
                Answer = "Kucing",
                Game = foundGame
            },
            new TebakKataAnswerEntity()
            {
                Answer = "Kambing",
                Game = foundGame
            },
        };

        await DB.SaveAsync(listAnswer2);


    }
}