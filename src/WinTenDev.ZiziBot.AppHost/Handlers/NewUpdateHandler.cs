using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SerilogTimings;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Services.Telegram.Extensions;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

public class NewUpdateHandler : IUpdateHandler
{
    private readonly ILogger<NewUpdateHandler> _logger;
    private readonly TelegramService _telegramService;

    public NewUpdateHandler(
        ILogger<NewUpdateHandler> logger,
        AfkService afkService,
        AntiSpamService antiSpamService,
        MataService mataService,
        SettingsService settingsService,
        TelegramService telegramService,
        WordFilterService wordFilterService
    )
    {
        _logger = logger;
        _telegramService = telegramService;
    }

    public async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        CancellationToken cancellationToken
    )
    {
        await _telegramService.AddUpdateContext(context);

        if (_telegramService.IsUpdateTooOld()) return;

        var floodCheck = await _telegramService.FloodCheckAsync();
        if (floodCheck.IsFlood) return;

        _logger.LogTrace("NewUpdate: {@V}", _telegramService.Update);

        // Pre-Task is should be awaited.
        var preTaskResult = await RunPreTasks();

        // Last, do additional task which bot may do
        RunPostTasks();

        if (!preTaskResult)
        {
            _logger.LogDebug("Next handler is ignored because pre-task is not success");
            return;
        }

        _logger.LogDebug("Continue to next Handler");

        await next(context, cancellationToken);
    }

    private async Task<bool> RunPreTasks()
    {
        var op = Operation.Begin("Run PreTask for ChatId: {ChatId}", _telegramService.ChatId);

        var hasRestricted = await _telegramService.CheckChatRestriction();

        if (hasRestricted)
        {
            return false;
        }

        await _telegramService.FireAnalyzer();

        var shouldDelete = await _telegramService.ScanMessageAsync();

        var hasSpam = await _telegramService.AntiSpamCheckAsync();

        if (hasSpam.IsAnyBanned)
        {
            return false;
        }

        var hasUsername = await _telegramService.RunCheckUserUsername();

        if (!hasUsername)
        {
            return false;
        }

        var hasPhotoProfile = await _telegramService.RunCheckUserProfilePhoto();

        if (!hasPhotoProfile)
        {
            return false;
        }

        if (shouldDelete)
        {
            return false;
        }

        op.Complete();

        return true;
    }

    private void RunPostTasks()
    {
        var op = Operation.Begin("Run PostTask");

        var nonAwaitTasks = new List<Task>
        {
            _telegramService.EnsureChatSettingsAsync(),
            _telegramService.AfkCheckAsync(),
            _telegramService.CheckNameChangesAsync()
        };

        nonAwaitTasks.InBackgroundAll();

        op.Complete();
    }
}