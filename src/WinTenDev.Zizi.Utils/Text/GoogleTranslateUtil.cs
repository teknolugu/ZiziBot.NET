using System.Text;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Serilog;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Utils.Text;

public static class GoogleTranslateUtil
{
    public static async Task<GoogleClients5Translator> GoogleTranslatorCoreAsync(
        this string text,
        string translationLanguage,
        string sourceLanguage = "auto"
    )
    {
        var url = "https://clients5.google.com/translate_a/single";

        Log.Debug(
            "Translating text from '{SL} to '{TL}'",
            sourceLanguage,
            translationLanguage
        );

        var res = await url
            .SetQueryParam("sl", sourceLanguage)
            .SetQueryParam("tl", translationLanguage)
            .SetQueryParam("dj", "1")
            .SetQueryParam(
                "dt",
                new[]
                    { "at", "bd", "ex", "ld", "md", "qca", "rw", "rm", "ss", "t", "sp" }
            )
            .SetQueryParam("client", "dict-chrome-ex")
            .SetQueryParam("q", text)
            .SetQueryParam("ie", "UTF-8")
            .WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4863.0 Safari/537.36")
            .GetJsonAsync<GoogleClients5Translator>();

        return res;
    }

    public static async Task<string> GoogleTranslatorAsync(
        this string text,
        string translateLanguage,
        string sourceLanguage = "auto"
    )
    {
        var translate = await text.GoogleTranslatorCoreAsync(translateLanguage, sourceLanguage);

        Log.Debug("Translate result: {@V}", translate.LdResult);

        var sb = new StringBuilder();

        foreach (var sentence in translate.Sentences)
        {
            sb.Append(sentence.Trans);
        }

        return sb.ToTrimmedString().HtmlEncode();
    }
}
