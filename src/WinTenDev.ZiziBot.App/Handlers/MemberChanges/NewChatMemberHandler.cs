using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Enums;
using BotFramework.Utils;
using MoreLinq.Extensions;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.ZiziBot.App.Handlers.Core;

namespace WinTenDev.ZiziBot.App.Handlers.MemberChanges
{
    /// <summary>
    /// Handle new chat Member
    /// </summary>
    public class NewMemberHandler : ZiziEventHandler
    {
        private readonly SettingsService _settingsService;
        private readonly AntiSpamService _antiSpamService;
        private ChatSetting _settings;


        /// <summary>
        ///
        /// </summary>
        /// <param name="settingsService"></param>
        /// <param name="antiSpamService"></param>
        public NewMemberHandler(
            SettingsService settingsService,
            AntiSpamService antiSpamService
        )
        {
            _settingsService = settingsService;
            _antiSpamService = antiSpamService;
        }

        /// <summary>
        /// Handle new Member
        /// </summary>
        [Message(MessageFlag.HasNewChatMembers)]
        public async Task NewMember()
        {
            Log.Debug("New Members on ChatId {Id} arrived: {@NewChatMembers}", Chat.Id, RawUpdate.Message.NewChatMembers);

            _settings = await _settingsService.GetSettingsByGroup(Chat.Id);

            if (!_settings.EnableWelcomeMessage)
            {
                Log.Information("Welcome Message is disabled on ChatId {Id}", Chat.Id);
                return;
            }

            var welcomeButton = _settings.WelcomeButton;

            var newMembers = RawUpdate.Message.NewChatMembers;
            var passedMember = await ScanMembersAsync(Chat.Id, newMembers);

            if (passedMember.Count < 1)
            {
                Log.Information("Welcome Message is disabled because no member passed!");
                return;
            }

            var messageText = await BuildWelcomeMessage(passedMember);
            var replyMarkup = _settings.WelcomeButton.ToReplyMarkup();

            if (_settings.EnableHumanVerification)
            {
                Log.Information("Human verification is enabled!");
                Log.Information("Adding verify button..");

                var userId = newMembers[0].Id;
                // var verifyButton = $"Saya Manusia!|verify {userId}";
                var verifyButton = $"Saya Manusia!|human-verify";

                var withVerifyArr = new[] { welcomeButton, verifyButton };
                var withVerify = string.Join(",", withVerifyArr);

                replyMarkup = withVerify.ToReplyMarkup(2);
            }


            // var htmlMsg = new HtmlBuilder()
            // .TextBr("");

            await SendMessageTextAsync(messageText, replyMarkup: replyMarkup);
        }

        private async Task<HtmlString> BuildWelcomeMessage(List<User> users)
        {
            Log.Debug("Building Welcome Message..");

            var templateMessage = _settings.WelcomeMessage;
            var chatTitle = Chat.Title;
            var newMemberCount = users.Count;
            var htmlString = new HtmlString();

            var greet = TimeUtil.GetTimeGreet();
            var memberCount = await Bot.GetChatMembersCountAsync(Chat.Id);

            htmlString.Text("Hai ");

            // var memberStr = users.Select((user, i) => htmlString.User(user.Id, user.GetFullName()));

            users.ForEach((
                User user,
                int index
            ) => {
                var last = users.Last();
                if (user == last)
                    htmlString.User(user.Id, user.GetFullName()).Br();
                else
                    htmlString.User(user.Id, user.GetFullName()).Text(", ");
            });

            // users.ForEachEx()

            if (templateMessage.IsNullOrEmpty())
            {
                htmlString.TextBr($"Selamat datang di kontrakan {chatTitle}")
                    .Text($"Kamu adalah anggota ke-{memberCount}");
            }

            // var messageText = templateMessage.ResolveVariable(new
            // {
            //     // allNewMember,
            //     // allNoUsername,
            //     // allNewBot,
            //     chatTitle,
            //     greet,
            //     newMemberCount,
            //     memberCount
            // });

            return htmlString;
        }

        private async Task<List<User>> ScanMembersAsync(
            long chatId,
            User[] users
        )
        {
            var members = new List<User>();

            foreach (var user in users)
            {
                var userId = user.Id;
                var checkAntiSpam = await _antiSpamService.CheckSpam(chatId, userId, FuncAntiSpamResult);
                if (!checkAntiSpam.IsAnyBanned)
                {
                    members.Add(user);
                }
            }

            return members;
        }

        private async Task FuncAntiSpamResult(AntiSpamResult result)
        {
            var userId = result.UserId;
            if (result.IsAnyBanned)
            {
                Log.Debug("Kicking User Id '{UserId}' because FBan", userId);
                await Bot.KickChatMemberAsync(Chat.Id, userId);
            }
        }
    }
}