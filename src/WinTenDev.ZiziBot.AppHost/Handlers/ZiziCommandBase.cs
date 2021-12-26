using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers;

public abstract class ZiziCommandBase : CommandBase
{

    private IUpdateContext _updateContext;

    public override Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        _updateContext = context;

        return this.HandleAsync(context, next, ParseCommandArgs(context.Update.Message));
    }


    public void SendMessageText()
    {
        var client = _updateContext.Bot.Client;
    }
}