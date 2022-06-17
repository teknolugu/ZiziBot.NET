using BotFramework;
using BotFramework.Attributes;

namespace WinTenDev.ZiziBot.Alpha1.Handlers
{
    public class RoleAttribute : HandlerAttribute
    {

        public RoleAttribute()
        {

        }

        protected override bool CanHandle(HandlerParams param)
        {
            return true;
        }
    }
}