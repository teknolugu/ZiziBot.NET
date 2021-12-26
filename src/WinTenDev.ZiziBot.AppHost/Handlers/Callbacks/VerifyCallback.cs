using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Callbacks;

public class VerifyCallback
{
    private readonly TelegramService _telegramService;
    private readonly CheckUsernameService _checkUsernameService;
    private readonly CallbackQuery _callbackQuery;

    public VerifyCallback(
        TelegramService telegramService,
        CheckUsernameService checkUsernameService
    )
    {
        _telegramService = telegramService;
        _checkUsernameService = checkUsernameService;
        _callbackQuery = telegramService.Context.Update.CallbackQuery;

        Log.Information("Receiving Verify Callback");
    }

    public async Task<bool> ExecuteVerifyAsync()
    {
        Log.Information("Executing Verify Callback");

        var callbackData = _callbackQuery.Data;
        var fromId = _callbackQuery.From.Id;
        var chatId = _callbackQuery.Message.Chat.Id;

        Log.Debug("CallbackData: {CallbackData} from {FromId}", callbackData, fromId);

        var partCallbackData = callbackData.Split(" ");
        var callBackParam1 = partCallbackData.ValueOfIndex(1);
        var answer = "Tombol ini bukan untukmu Bep!";

        Log.Debug("Verify Param1: {0}", callBackParam1);

        if (callBackParam1 == "username")
        {
            Log.Debug("Checking username");
            if (_telegramService.IsNoUsername)
            {
                answer = "Sepertinya Anda belum memasang Username, silakan di periksa kembali.";
            }
            else
            {
                // await _telegramService.RemoveOldWarnUsernameQueue(chatId);

                // var warnJson = await WarnUsernameUtil.GetWarnUsernameCollectionAsync();
                // var listWarns = warnJson.AsQueryable().ToList();
                var listWarns = new List<WarnUsernameHistory>();

                Parallel.ForEach(listWarns, async (warn) =>
                    // foreach (var warn in listWarns)
                {
                    var userId = warn.FromId;
                    var isExist = listWarns.Any(x => x.FromId == userId);
                    Log.Debug("Is UserId: {0} exist? => {1}", userId, isExist);

                    try
                    {
                        var chatMember = await _telegramService.Client.GetChatMemberAsync(chatId, userId);

                        if (_telegramService.IsNoUsername)
                        {
                            Log.Debug("User {0} does not set an Username", userId);
                        }
                        else
                        {
                            await _telegramService.RestrictMemberAsync(fromId, true);


                            if (isExist)
                            {
                                // var delete = await warnJson.DeleteManyAsync(x =>
                                //         x.FromId == userId && x.ChatId == chatId);
                                // Log.Debug("Deleting {0} result {1}", userId, delete);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error Occured.");

                        if (ex.Message.Contains("user not found"))
                        {
                            Log.Debug("Maybe UserID '{0}' has leave from ChatID '{1}' ", userId, chatId);
                            if (isExist)
                            {
                                // var delete = await warnJson.DeleteManyAsync(x =>
                                //         x.FromId == userId && x.ChatId == chatId);
                                // Log.Debug("Deleting {0} result {1}", userId, delete);
                            }
                        }
                    }

                });

                answer = "Terima kasih sudah verifikasi Username!";
            }

            // await _telegramService.UpdateWarnMessageAsync();
            Log.Debug("Verify Username finish");
        }
        else if (fromId == callBackParam1.ToInt64())
        {
            await _telegramService.RestrictMemberAsync(callBackParam1.ToInt(), true);
            answer = "Terima kasih sudah verifikasi!";
        }

        await _telegramService.AnswerCallbackQueryAsync(answer);
        return true;
    }
}