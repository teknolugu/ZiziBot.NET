using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using BotFramework.Utils;
using TL;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.ZiziBot.Alpha1.Handlers.Core
{
    /// <summary>
    /// Handle Debug
    /// </summary>
    public class DebugHandler : ZiziEventHandler
    {
        private readonly WTelegramApiService _iWTelegramApiService;

        public DebugHandler(WTelegramApiService iWTelegramApiService)
        {
            _iWTelegramApiService = iWTelegramApiService;
        }

        /// <summary>
        /// Handle Debug command
        /// Usage: <code>/dbg</code>
        /// </summary>
        [Command("dbg", CommandParseMode.Both)]
        public async Task Debug()
        {
            var update = RawUpdate.ToJson(indented: true);

            var htmlStr = new HtmlString()
                .Bold("Debug Message").Br()
                .Code(update).Br();

            await SendMessageTextAsync(htmlStr);
        }

        [Command("all", CommandParseMode.Both)]
        public async Task CmdAllMember()
        {
            var update = RawUpdate.ToJson(indented: true);

            // var client = await _iWTelegramApiService.CreateClient();
            // client.CollectAccessHash = true;
            // var accessHash = client.GetAccessHashFor<Channel>(ChatId);
            // var chats = await client.Messages_GetAllChats(null);
            // var chat = chats.chats.Values.First(x => x.ID == cid);
            // var channel = (Channel) chats.chats[cid];
            // var channels = await client.Channels_GetParticipants(channel, null, 0, 1000, 0);
            // var cid = ChatId.ReduceChatId();
            // var channels = await client.Channels_GetParticipants(new InputChannel()
            // {
            // access_hash =  client.GetAccessHashFor<Channel>(cid),
            // channel_id = cid
            // }, null, 0, 1000, 0);

            // var fullChat = await client.Messages_GetFullChat(chat.ID);
            // var fullChat = await client.Channels_GetFullChannel(new InputChannel()
            // {
            // access_hash = client.GetAccessHashFor<Channel>(cid),
            // channel_id = cid
            // });

            // var users = fullChat.users.Take(200);

            var channels = await _iWTelegramApiService.GetAllParticipants(ChatId);

            var htmlStr = new HtmlString();
            foreach (var (id, userBase) in channels.users)
                // foreach (var user in channels.users)
            {
                var user = (User) userBase;
                // htmlStr.Text(user.ID.ToString()).Text(" - ").Text(u.first_name).Br();
                // var u = (User)user;
                htmlStr.Text(user.id.ToString()).Text(" - ").Text(user.first_name).Br();
            }

            htmlStr.Br();

            await SendMessageTextAsync(htmlStr);
        }

        [Command("id", CommandParseMode.Both)]
        public async Task Id()
        {
            var htmlMsg = new HtmlString()
                .Bold($"👥 {Chat.Title}").Br()
                .Bold("ID: ").Code(Chat.Id.ToString()).Br()
                .Bold("Type: ").Code(Chat.Type.ToString()).Br().Br()
                .Bold($"👤 {From.GetFullName()}").Br()
                .Bold("ID: ").Code(From.Id.ToString()).Br()
                .Bold($"Lang: ").Code(From.LanguageCode.ToUpperCase());

            if (ReplyToMessage != null)
            {
                var message = ReplyToMessage;
                htmlMsg.Br().Br()
                    .Bold($"👤 {message.From.GetFullName()}").Br()
                    .Bold("ID: ").Code(message.From.Id.ToString()).Br()
                    .Bold($"Lang: ").Code(message.From.LanguageCode.ToUpperCase());
            }

            await SendMessageTextAsync(htmlMsg);
        }
    }
}