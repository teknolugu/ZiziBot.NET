using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Enums;
using BotFramework.Setup;
using BotFramework.Utils;
using Humanizer;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.ZiziBot.App.Handlers.Core;
using WinTenDev.ZiziBot.App.Handlers.Modules;

namespace WinTenDev.ZiziBot.App.Handlers
{
    /// <summary>
    /// Handle update
    /// </summary>
    public class UpdateHandler : ZiziEventHandler
    {
        /// <summary>
        /// The chat setting
        /// </summary>
        private ChatSetting _chatSetting;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The chat restriction module
        /// </summary>
        private readonly ChatRestrictionModule _chatRestrictionModule;

        /// <summary>
        /// The anti spam service
        /// </summary>
        private readonly AntiSpamService _antiSpamService;

        /// <summary>
        /// The settings service
        /// </summary>
        private readonly SettingsService _settingsService;

        /// <summary>
        /// The word filter service
        /// </summary>
        private readonly WordFilterService _wordFilterService;

        /// <summary>
        /// Instantiate UpdateHandler
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="chatRestrictionModule"></param>
        /// <param name="antiSpamService"></param>
        /// <param name="settingsService"></param>
        /// <param name="wordFilterService"></param>
        public UpdateHandler(
            ILogger<UpdateHandler> logger,
            ChatRestrictionModule chatRestrictionModule,
            AntiSpamService antiSpamService,
            SettingsService settingsService,
            WordFilterService wordFilterService
        )
        {
            _logger = logger;
            _chatRestrictionModule = chatRestrictionModule;
            _antiSpamService = antiSpamService;
            _settingsService = settingsService;
            _wordFilterService = wordFilterService;
        }

        /// <summary>
        /// Handle on all update and all chat
        /// </summary>
        /// <returns></returns>
        [Update(InChat.All, UpdateFlag.All)]
        [Priority(10)]
        public async Task<bool> OnUpdate()
        {
            var sw = Stopwatch.StartNew();

            _logger.LogDebug("New Update: {@RawUpdate}", RawUpdate);

            _chatSetting = await _settingsService.GetSettingsByGroup(ChatId);

            var checkAntiSpamTask = CheckAntiSpam();
            var checkScanMessageTask = CheckScanMessageAsync();
            var checkMentionTask = CheckMention();
            var checkRestrictionTask = CheckChatRestricted();
            var fireMessageTask = CheckFireMessage();

            await Task.WhenAll(checkAntiSpamTask,
                checkScanMessageTask,
                fireMessageTask,
                checkRestrictionTask);

            var checkAntiSpam = await checkAntiSpamTask;
            var checkScanMessage = await checkScanMessageTask;
            var checkMention = await checkMentionTask;
            var checkRestriction = await checkRestrictionTask;
            var fireMessageCheck = await fireMessageTask;

            _logger.LogDebug("User {@From} has No Username? {NoUsername}", From.ToString(), NoUsername);

            var shouldNext = !checkAntiSpam.IsAnyBanned && !checkRestriction && HasUsername;
            _logger.LogDebug("Should to Next handler? {ShouldNext}. Elapsed. {Elapsed}", shouldNext, sw.Elapsed);
            sw.Stop();

            return shouldNext;
        }

        /// <summary>
        /// Check the anti spam
        /// </summary>
        /// <returns>The check anti spam</returns>
        private async Task<AntiSpamResult> CheckAntiSpam()
        {
            var checkAntiSpam = await _antiSpamService.CheckSpam(Chat.Id, From.Id, async result => {
                var userId = result.UserId;

                if (result.IsAnyBanned)
                {
                    _logger.LogDebug("Kicking User Id '{UserId}' because FBan", userId);
                    await Bot.KickChatMemberAsync(Chat.Id, userId);
                }
            });

            return checkAntiSpam;
        }

        /// <summary>
        /// Check the scan message text/caption media
        /// </summary>
        /// <returns>A task containing the bool</returns>
        private async Task<bool> CheckScanMessageAsync()
        {
            try
            {
                var checkScanMessage = false;
                if (CallbackQuery != null)
                {
                    _logger.LogWarning("Look this message is callbackQuery!");
                    return false;
                }

                if (Message == null)
                {
                    _logger.LogInformation("This Message don't contain any Message");
                    return false;
                }

                if (!_chatSetting.EnableWordFilterGroupWide)
                {
                    _logger.LogDebug("Word Filter on {ChatId} is disabled!", ChatId);
                    return false;
                }

                var text = Message.Text ?? Message.Caption;
                if (text.IsNullOrEmpty())
                {
                    _logger.LogInformation("No message Text for scan..");
                }
                else
                {
                    var result = await _wordFilterService.IsMustDelete(text);
                    var isMustDelete = result.IsSuccess;

                    if (isMustDelete) _logger.LogInformation("Starting scan image if available..");

                    _logger.LogInformation("Message {MsgId} IsMustDelete: {IsMustDelete}", Message.MessageId, isMustDelete);

                    if (isMustDelete)
                    {
                        _logger.LogDebug("Scan Result: {@V}", result);
                        // var note = "Pesan di Obrolan di hapus karena terdeteksi filter Kata.\n" + result.Notes;
                        // await _telegramService.SendEventAsync(note);

                        await DeleteMessageAsync();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured when run {V}", nameof(CheckScanMessageAsync).Humanize());
                return false;
            }

            return false;
        }

        /// <summary>
        /// Checks the chat restricted
        /// </summary>
        /// <returns>The check restriction</returns>
        private async Task<bool> CheckChatRestricted()
        {
            if (IsPrivateChat())
            {
                return false;
            }

            var checkRestriction = await _chatRestrictionModule.CheckRestriction(ChatId, async result => {
                if (result.DoLeaveChat)
                {
                    await SendMessageTextAsync(result.HtmlMessage, chatId: result.ChatId);
                    await Bot.LeaveChatAsync(result.ChatId);
                }
            });

            return checkRestriction;
        }

        /// <summary>
        /// Check the mention
        /// </summary>
        /// <returns>The any mention</returns>
        private async Task<bool> CheckMention()
        {
            var anyMention = false;

            var messageEntities = MessageOrEdited.Entities;
            if (messageEntities != null)
            {
                // .Where(entity => entity.Type is MessageEntityType.Mention or MessageEntityType.TextMention);
                var messageMention = MessageOrEdited.Text
                    .Split(" ")
                    .Where((
                        s,
                        i
                    ) => s.Contains("@"));

                if (!messageEntities.Any())
                {
                    anyMention = false;
                }
                else
                {
                    anyMention = true;
                    messageMention.ForEach(async (
                        target,
                        i
                    ) => {
                        // await SendMessageTextAsync(chatId: target, messageStr: "Lorem ipsum dolor sit amet");
                    });
                }
            }

            if (ReplyToMessage != null)
            {
                var messageUrl = Message.GetMessageLink();

                var repFromId = ReplyToMessage.From.Id;
                var fromUsername = "@" + Message.From.Username;
                var chatTitle = Message.Chat.Title;

                var htmlMessage = new HtmlString()
                    .Bold("Anda telah di Tag").Br()
                    .Bold("Dari: ").Text(fromUsername).Br()
                    .Bold("Grup: ").Text(chatTitle);
                // .Br()
                // .Br()
                // .Url(messageUrl, "Ke Pesan");

                var keyboardMarkup = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("➡️ Ke Pesan", messageUrl)
                    }
                });

                await SendMessageTextAsync(chatId: repFromId,
                    message: htmlMessage,
                    replyMarkup: keyboardMarkup,
                    disableWebPreview: true);
            }

            return anyMention;
        }

        /// <summary>
        /// Check the fire message
        /// </summary>
        /// <returns>A task containing the bool</returns>
        private async Task<bool> CheckFireMessage()
        {
            var message = RawUpdate.Message;

            if (message == null)
            {
                var messageId = CallbackQueryMessage.MessageId;
                _logger.LogInformation("MessageId '{MessageId}' on ChatID '{ChatId}' is type {Type}",
                    messageId, ChatId, RawUpdate.Type);
                return false;
            }

            var messageText = message.Text;
            var captionText = message.Caption;

            var text = messageText ?? captionText;

            if (text == null)
            {
                _logger.LogDebug("No Message Text/Caption on ChatId '{ChatId}' for MessageId '{MessageId}'",
                    ChatId, message.MessageId);

                return true;
            }

            var result = text.AnalyzeString();
            var fireRatio = result.FireRatio;
            var wordCount = result.WordsCount;

            if (wordCount < 3)
            {
                _logger.LogDebug("String analyzer stop, because Words count is less than 3");
                return false;
            }

            var aaa = fireRatio switch
            {
                >= 1 => "Tolong matikan CAPS LOCK sebelum mengetik pesan.",
                >= 0.6 => "Tolong kurangi penggunaan huruf kapital yang berlebihan.",
                _ => ""
            };

            await SendMessageTextAsync(aaa, replyToMessageId: message.MessageId);

            return true;
        }
    }
}