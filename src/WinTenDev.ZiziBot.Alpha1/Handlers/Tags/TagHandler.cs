using System;
using System.Linq;
using System.Threading.Tasks;
using BotFramework;
using BotFramework.Attributes;
using BotFramework.Setup;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Models.Validators;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.ZiziBot.Alpha1.Handlers.Core;

namespace WinTenDev.ZiziBot.Alpha1.Handlers.Tags
{
    /// <summary>
    /// Tag Handler for creation tag
    /// </summary>
    public class TagHandler : ZiziEventHandler
    {
        private readonly TagsService _tagsService;

        /// <summary>
        /// Instantiate TagHandler
        /// </summary>
        /// <param name="tagsService"></param>
        public TagHandler(TagsService tagsService)
        {
            _tagsService = tagsService;
        }

        /// <summary>
        /// Create tag without remove previous tag
        /// </summary>
        /// <param name="param"></param>
        [ParametrizedCommand("tag", CommandParseMode.Both)]
        public async Task Tag(CloudTag param)
        {
            await ProcessingSave(param, false);
        }

        /// <summary>
        /// Create tag with remove previous tag
        /// </summary>
        /// <param name="param"></param>
        [ParametrizedCommand("retag", CommandParseMode.Both)]
        public async Task ReTag(CloudTag param)
        {
            await ProcessingSave(param, true);
        }

        [ParametrizedCommand("untag", CommandParseMode.Both)]
        public async Task UnTag(CloudTag param)
        {
            var validator = await new TagParamValidator().ValidateAsync(param);

            if (!validator.IsValid)
            {
                var validatorMessage = validator.Errors.Select(failure => failure.ErrorMessage).JoinStr("\n");
                await SendMessageTextAsync(validatorMessage);
                return;
            }

            await SendMessageTextAsync($"Sedang menghapus tag '{param.Tag}'..");
            var deleteTag = await _tagsService.DeleteTag(Chat.Id, param.Tag);

            if (deleteTag)
            {
                await EditMessageTextAsync($"Menghapus tag '{param.Tag}' berhasil. Gunakan /tags untuk menampilkan perubahan.");
                await _tagsService.UpdateCacheAsync(ChatId);
            }
            else
            {
                await EditMessageTextAsync($"Gagal menghapus tag '{param.Tag}', silakan di coba kembali nanti.");
            }
        }


        private async Task ProcessingSave(
            CloudTag param,
            bool reTag
        )
        {
            if (reTag)
            {
                await _tagsService.DeleteTag(Chat.Id, param.Tag);
            }

            var validator = await new TagParamValidator().ValidateAsync(param);

            if (!validator.IsValid)
            {
                var validatorMessage = validator.Errors.Select(failure => failure.ErrorMessage).JoinStr("\n");
                await SendMessageTextAsync(validatorMessage);
                return;
            }

            if (reTag)
            {
                await _tagsService.DeleteTag(Chat.Id, param.Tag);
            }

            if (await _tagsService.IsExist(Chat.Id, param.Tag))
            {
                await SendMessageTextAsync(
                    $"Sepertinya tag '{param.Tag}' sudah ada. " +
                    $"silakan gunakan nama lain nya atau gunakan /retag."
                );
                return;
            }

            await SendMessageTextAsync($"Sedang menyimpan tag '{param.Tag}'..");

            var saveTag = await _tagsService.SaveTagAsync(param);
            if (saveTag == 1)
            {
                await EditMessageTextAsync($"Sedang memperbarui cache..");
                await _tagsService.UpdateCacheAsync(Chat.Id);

                await EditMessageTextAsync($"Tag berhasil disimpan, gunakan #{param.Tag} untuk mengambil kembali.");
            }
            else
            {
                await EditMessageTextAsync($"Tag '{param.Tag}' gagal disimpan");
            }

        }
    }

    /// <summary>
    /// Parse parameters from update
    /// </summary>
    public class TagParser : IRawParameterParser<CloudTag>
    {
        /// <summary>
        /// DefaultInstance
        /// </summary>
        /// <returns></returns>
        public CloudTag DefaultInstance()
        {
            return new CloudTag();
        }

        /// <summary>
        /// Parse parameters from update
        /// </summary>
        /// <param name="update"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetValueByRawUpdate(
            Update update,
            out CloudTag result
        )
        {
            var param = new CloudTag();

            var anyMessage = update.EditedMessage ?? update.Message;
            var replyMessage = anyMessage.ReplyToMessage;

            param.ChatId = anyMessage.Chat.Id;
            param.FromId = anyMessage.From.Id;

            var messageText = anyMessage.Text;

            var messageTexts = messageText.Split(" ");

            var cmd = messageTexts.ElementAtOrDefault(0) ?? string.Empty;
            var slug = messageTexts.ElementAtOrDefault(1) ?? string.Empty;
            var cmdSlug = $"{cmd} {slug}".Trim();

            param.Tag = slug;
            param.BtnData = messageText.Substring(cmdSlug.Length);
            param.CreatedAt = DateTime.Now;
            param.UpdatedAt = DateTime.Now;

            if (replyMessage != null)
            {
                var replyMessageText = replyMessage.Text;
                var replyMessageTexts = replyMessageText.Split(" ");
                param.Content = replyMessage.Text ?? replyMessage.Caption;
                param.FileId = replyMessage.GetFileId();
            }

            result = param;

            return true;
        }
    }
}