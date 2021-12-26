using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

public class ExceptionHandler : IUpdateHandler
{
    public ExceptionHandler()
    {
    }

    public async Task HandleAsync(IUpdateContext context, UpdateDelegate next, CancellationToken cancellationToken)
    {
        var contextUpdate = context.Update;

        try
        {
            await next(context, cancellationToken);
        }
        catch (Exception e)
        {
            Log.Error(e.Demystify(), "Exception handler");
            Log.Error("{Message}. {@JsonUpdate}", e.Message, contextUpdate.ToJson(true));
        }
    }
}