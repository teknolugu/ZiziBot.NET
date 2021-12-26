using BotFramework.Attributes;
using BotFramework.Setup;
using WinTenDev.ZiziBot.App.Handlers.Core;

namespace WinTenDev.ZiziBot.App.Handlers.Group
{
    public class PinHandler : ZiziEventHandler
    {
        [Command(InChat.Public, "pin", CommandParseMode.Both)]
        public void Pin()
        {


        }

        [Command(InChat.Public, "unpin", CommandParseMode.Both)]
        public void UnPin()
        {

        }

        [Command(InChat.Public, "unpinall", CommandParseMode.Both)]
        public void UnPinAll()
        {

        }
    }
}