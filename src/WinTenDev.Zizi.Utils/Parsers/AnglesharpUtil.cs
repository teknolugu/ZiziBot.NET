using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;

namespace WinTenDev.Zizi.Utils.Parsers;

public static class AnglesharpUtil
{
    public static IBrowsingContext DefaultContext
    {
        get
        {
            var config = Configuration.Default.WithDefaultLoader().WithJs().WithCss();
            var context = BrowsingContext.New(config);

            return context;
        }
    }

    public static async Task<IDocument> OpenDocumentAsync(this string url)
    {
        var document = await DefaultContext.OpenAsync(url);

        return document;
    }
}