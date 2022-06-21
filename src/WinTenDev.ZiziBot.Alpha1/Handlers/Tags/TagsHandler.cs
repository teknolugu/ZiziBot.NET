using System.Linq;
using System.Threading.Tasks;
using BotFramework.Attributes;
using BotFramework.Setup;
using BotFramework.Utils;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.ZiziBot.Alpha1.Handlers.Core;

namespace WinTenDev.ZiziBot.Alpha1.Handlers.Tags
{
    /// <summary>
    ///
    /// </summary>
    public class TagsHandler : ZiziEventHandler
    {
        private readonly TagsService _tagsService;

        /// <summary>
        ///
        /// </summary>
        /// <param name="tagsService"></param>
        public TagsHandler(TagsService tagsService)
        {
            _tagsService = tagsService;
        }

        /// <summary>
        ///
        /// </summary>
        [Command("tags", CommandParseMode.Both)]
        public async Task GetAllTags()
        {
            await SendMessageTextAsync("Sedang mengambil data");

            var tags = (await _tagsService.GetTagsByGroupAsync(Chat.Id)).ToList();
            var tagsCount = tags.Count;

            var htmlString = new HtmlString();

            if (tagsCount > 0)
            {
                await EditMessageTextAsync($"Mendapatkan {tagsCount} tags");
                htmlString.Bold($"#️⃣ {tagsCount} Tags").Br().Br();

                foreach (var tag in tags)
                {
                    htmlString.Text($"#{tag.Tag} ");
                }
            }
            else
            {
                htmlString.Text("Tidak ada tags di obrolan ini");
            }
            await EditMessageTextAsync(htmlString);

        }
    }
}