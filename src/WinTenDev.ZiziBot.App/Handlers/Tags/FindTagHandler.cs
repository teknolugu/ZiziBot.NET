using System.Linq;
using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Utils;
using Microsoft.Extensions.Logging;
using MoreLinq;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.ZiziBot.App.Handlers.Core;

namespace WinTenDev.ZiziBot.App.Handlers.Tags
{
    /// <summary>
    /// FindTagHandler
    /// </summary>
    public class FindTagHandler : ZiziEventHandler
    {
        private readonly ILogger<FindTagHandler> _logger;
        private readonly TagsService _tagsService;

        /// <summary>
        ///
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="tagsService"></param>
        public FindTagHandler(
            ILogger<FindTagHandler> logger,
            TagsService tagsService
        )
        {
            _logger = logger;
            _tagsService = tagsService;

        }

        /// <summary>
        /// Find Tag
        /// </summary>
        [Message("#", regex: true)]
        public async Task FindTag()
        {
            _logger.LogInformation("Starting find Tags on ChatId '{Id}'", Chat.Id);

            var tagsStr = RawUpdate.Message.Text.Split(' ', '\n', ',').Where(x => x.Contains("#")).Take(5);

            var tagsData = await _tagsService.GetTagsByGroupAsync(Chat.Id);

            tagsStr.ForEach(async (tag, index) => {
                _logger.LogDebug("Processing Tag on ChatId '{Id}' with tag '{Tag}'", Chat.Id, tag);

                var trimTag = tag.TrimStart('#');

                var tagData = tagsData.FirstOrDefault(cloudTag => cloudTag.Tag == trimTag);

                if (tagData == null)
                {
                    _logger.LogDebug("On ChatId '{Id}' no tag '{Tag}'", Chat.Id, tag);
                    return;
                }

                await PrepareSend(tagData);

            });
        }

        private async Task PrepareSend(CloudTag tag)
        {
            var tagContent = tag.Content;
            var tagSlug = tag.Tag;

            var htmlString = new HtmlString()
                .Text(tagContent);

            var messageId = RawUpdate.Message.MessageId;

            await SendMessageTextAsync(htmlString, replyToMessageId: messageId);
        }
    }
}