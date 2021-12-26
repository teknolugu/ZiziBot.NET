using System.Text;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Serilog;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Utils.Text;

/// <summary>
/// The translator util.
/// </summary>
public static class TranslatorUtil
{
    /// <summary>
    /// Translate text using Google Translate Clients5 (core).
    /// </summary>
    /// <param name="text">text is string for translate to</param>
    /// <param name="tl">tl is translation language</param>
    /// <param name="sl">sl is source language</param>
    /// <returns>A Task.</returns>
    public static async Task<GoogleClients5Translator> GoogleTranslatorCoreAsync(this string text, string tl, string sl = "auto")
    {
        var url = "https://clients5.google.com/translate_a/t";

        Log.Debug("Translating text from '{SL} to '{TL}'", sl, tl);
        var res = await url.SetQueryParam("sl", sl)
            .SetQueryParam("tl", tl)
            .SetQueryParam("client", "dict-chrome-ex")
            .SetQueryParam("q", text)
            .SetQueryParam("ie", "UTF-8")
            .GetJsonAsync<GoogleClients5Translator>();

        return res;
    }

    /// <summary>
    ///     Googles the translator using the specified text
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="tl">The tl</param>
    /// <param name="sl">The sl</param>
    /// <returns>A task containing the string</returns>
    public static async Task<string> GoogleTranslatorAsync(this string text, string tl, string sl = "auto")
    {
        var translate = await text.GoogleTranslatorCoreAsync(tl, sl);

        Log.Debug("Translate result: {@V}", translate.LdResult);

        var sb = new StringBuilder();
        foreach (var sentence in translate.Sentences)
        {
            sb.Append(sentence.Trans);
        }

        return sb.ToTrimmedString();
    }
}