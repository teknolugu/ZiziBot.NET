using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;

namespace WinTenDev.Zizi.Services.Externals;

public class KbbiService
{
    // private string baseUrl = "https://kbbi.kemdikbud.go.id/entri";
    private string baseUrl = "https://kbbi.web.id";

    public async Task<KbbiSearch> SearchWord(string word)
    {
        var url = $"{baseUrl}/{word}";
        var document = await url.OpenDocumentAsync();
        var contentElement = document.All
            .OfType<IHtmlDivElement>()
            .FirstOrDefault(element => element.Id == "d1");

        var text = contentElement.TextContent.Trim();

        var kbbiSearch = new KbbiSearch()
        {
            Url = url,
            Content = text
        };

        return kbbiSearch;
    }
}