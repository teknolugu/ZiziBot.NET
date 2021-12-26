using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Models.Interfaces;

/// <summary>
/// This is interface for Callback Handler
/// </summary>
public interface ICallbackHandler
{
    /// <summary>
    /// This is function is used to execute Callback Sync
    /// </summary>
    /// <param name="callbackQuery"></param>
    /// <param name="onAnswerCallback"></param>
    /// <returns></returns>
    bool Execute(CallbackQuery callbackQuery, Func<CallbackAnswer, Task> onAnswerCallback);

    /// <summary>
    /// This is function is used to execute Callback Async
    /// </summary>
    /// <param name="callbackQuery"></param>
    /// <param name="onAnswerCallback"></param>
    /// <returns>
    ///   <br />
    /// </returns>
    Task<bool> ExecuteAsync(CallbackQuery callbackQuery, Func<CallbackAnswer, Task> onAnswerCallback);
}