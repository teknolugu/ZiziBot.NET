using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Serilog;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Utils;

public static class StringUtil
{
    public static List<string> SplitText(
        this string text,
        string delimiter
    )
    {
        return text?.Split(delimiter).ToList();
    }

    public static string JoinStr(
        this IEnumerable<string> source,
        string separator
    )
    {
        return string.Join(separator, source);
    }

    public static string ResolveVariable(
        this string input,
        object parameters
    )
    {
        Log.Debug("Resolving variable..");
        var type = parameters.GetType();
        var regex = new Regex("\\{(.*?)\\}");
        var sb = new StringBuilder();
        var pos = 0;

        if (input == null) return null;

        foreach (Match toReplace in regex.Matches(input))
        {
            var capture = toReplace.Groups[0];
            var paramName = toReplace.Groups[toReplace.Groups.Count - 1].Value;
            var property = type.GetProperty(paramName);
            if (property == null) continue;
            sb.Append(input.Substring(pos, capture.Index - pos));
            sb.Append(property.GetValue(parameters, null));
            pos = capture.Index + capture.Length;
        }

        if (input.Length > pos + 1) sb.Append(input.Substring(pos));

        return sb.ToString();
    }

    public static string ResolveVariable(
        this string input,
        IEnumerable<(string placeholder, string value)> placeHolders
    )
    {
        return placeHolders.Aggregate(
            input,
            (
                current,
                ph
            ) => current.Replace(
                $"{{{ph.placeholder}}}",
                ph.value,
                StringComparison.CurrentCultureIgnoreCase
            )
        );
    }

    public static async Task ToFile(
        this string content,
        string path
    )
    {
        Log.Debug("Writing file to {0}", path);
        await File.WriteAllTextAsync(path, content);
    }

    public static string SqlEscape(this string str)
    {
        if (str.IsNullOrEmpty()) return str;

        var escaped = Regex.Replace(
            str,
            @"[\x00'""\b\n\r\t\cZ\\%_]",
            delegate(Match match) {
                var v = match.Value;

                switch (v)
                {
                    case "\x00":// ASCII NUL (0x00) character
                        return "\\0";

                    case "\b":// BACKSPACE character
                        return "\\b";

                    case "\n":// NEWLINE (linefeed) character
                        return "\\n";

                    case "\r":// CARRIAGE RETURN character
                        return "\\r";

                    case "\t":// TAB
                        return "\\t";

                    case "\u001A":// Ctrl-Z
                        return "\\Z";

                    default:
                        return "\\" + v;
                }
            }
        );
        escaped = escaped.Replace("'", "\'");

        return escaped;
    }

    public static string NewSqlEscape(this object obj)
    {
        return obj.ToString().Replace("'", "''");
    }

    public static bool CheckUrlValid(this string source)
    {
        return Uri.TryCreate(
                   source,
                   UriKind.Absolute,
                   out var uriResult
               ) &&
               (uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeHttp);
    }

    public static string GetBaseUrl(this string url)
    {
        var uri = new Uri(url);
        return uri.Host;
    }

    public static string NumSeparator(this object number)
    {
        return $"{number:#,#.00}";
    }

    public static string MkUrl(
        this string text,
        string url
    )
    {
        return $"<a href ='{url}'>{text}</a>";
    }

    public static string MkJoin(
        this ICollection<string> obj,
        string delim
    )
    {
        return string.Join(delim, obj.ToArray());
    }

    public static string CleanExceptAlphaNumeric(this string str)
    {
        var arr = str.Where
        (
            c => char.IsLetterOrDigit(c) ||
                 char.IsWhiteSpace(c) ||
                 c == '-'
        ).ToArray();

        return new string(arr);
    }

    public static string RemoveThisChar(
        this string str,
        string chars
    )
    {
        return str.IsNullOrEmpty()
            ? str
            : chars.Aggregate(
                str,
                (
                        current,
                        c
                    ) =>
                    current.Replace($"{c}", "")
            );
    }

    public static string RemoveThisString(
        this string str,
        params string[] forRemoves
    )
    {
        foreach (var remove in forRemoves) str = str.Replace(remove, "").Trim();

        return str;
    }

    public static string RemoveLastLines(
        this string str,
        int lines = 1
    )
    {
        return str.Trim().Split("\n").SkipLast(lines).JoinStr("\n").Trim();
    }

    public static string StripMargin(this string s)
    {
        return Regex.Replace(
            s,
            @"[ \t]+\|",
            string.Empty
        );
    }

    public static string StripLeadingWhitespace(this string s)
    {
        var r = new Regex(@"^\s+", RegexOptions.Multiline);
        return r.Replace(s, string.Empty);
    }

    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }

    public static bool IsNotNullOrEmpty(this string str)
    {
        return !str.IsNullOrEmpty();
    }

    public static bool IsContains(
        this string str,
        string filter
    )
    {
        return str.Contains(filter);
    }

    public static bool NotContains(
        this string str,
        [NotNull] string value
    )
    {
        if (value.IsNullOrEmpty()) return false;

        return !str.Contains(value);
    }

    public static bool ContainsListStr(
        this string str,
        string[] listStr,
        StringComparison stringComparison
    )
    {
        return listStr.Any(s => str.Contains(s, stringComparison));
    }

    public static bool ContainsListStr(
        this string str,
        params string[] listStr
    )
    {
        return listStr.Any(s => str.Contains(s, StringComparison.CurrentCultureIgnoreCase));
    }

    public static string ToLowerCase(this string str)
    {
        return str.ToLower(CultureInfo.CurrentCulture);
    }

    public static string ToUpperCase(this string str)
    {
        return str.IsNullOrEmpty() ? str : str.ToUpper(CultureInfo.CurrentCulture);
    }

    public static string ToTitleCase(this string text)
    {
        var textInfo = new CultureInfo("en-US", false).TextInfo;
        return textInfo.ToTitleCase(text.ToLower());
    }


    public static string RemoveStrAfterFirst(
        this string str,
        string after
    )
    {
        return str.Substring(0, str.IndexOf(after, StringComparison.Ordinal) + 1);
    }

    public static string DistinctChar(this string str)
    {
        return new(str.ToCharArray().Distinct().ToArray());
    }

    public static string GenerateUniqueId(int lengthId = 11)
    {
        var builder = new StringBuilder();

        Enumerable
            .Range(65, 26)
            .Select(e => ((char) e).ToString())
            .Concat(Enumerable.Range(97, 26).Select(e => ((char) e).ToString()))
            .Concat(Enumerable.Range(0, 10).Select(e => e.ToString()))
            .OrderBy(_ => Guid.NewGuid())
            .Take(lengthId)
            .ToList()
            .ForEach(e => builder.Append(e));

        var id = builder.ToString();

        return id;
    }

    public static string ToTrimmedString(this StringBuilder sb)
    {
        return sb.ToString().Trim();
    }

    public static string RemoveWhitespace(this string input)
    {
        if (input == null) return string.Empty;

        return new string
        (
            input.ToCharArray()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray()
        );
    }

    public static int WordsCount(this string line)
    {
        var wordCount = 0;

        for (var i = 0; i < line.Length; i++)
            if (line[i] == ' ' ||
                i == line.Length - 1)
                wordCount++;
        return wordCount;
    }

    public static IEnumerable<string> SplitInParts(
        this string s,
        int partLength
    )
    {
        if (s == null)
            throw new ArgumentNullException(nameof(s));

        if (partLength <= 0)
            throw new ArgumentException("Part length has to be positive.", nameof(partLength));

        for (var i = 0; i < s.Length; i += partLength)
            yield return s.Substring(i, Math.Min(partLength, s.Length - i));
    }

    public static StringAnalyzer AnalyzeString(this string text)
    {
        var allStrCount = text.Length;
        var upperStrCount = text.Count(c => char.IsUpper(c));
        var lowerStrCount = text.Count(c => char.IsLower(c));
        var alphaNumStrCount = text.CleanExceptAlphaNumeric().Length;

        var alphaNumNoSpaceStrCount = text.CleanExceptAlphaNumeric()
            .Replace(" ", "").Length;

        var result = new StringAnalyzer()
        {
            WordsCount = text.WordsCount(),
            AllStrCount = allStrCount,
            AlphaNumStrCount = alphaNumStrCount,
            AlphaNumStrNoSpaceCount = alphaNumNoSpaceStrCount,
            LowerStrCount = lowerStrCount,
            UpperStrCount = upperStrCount,
            // ReSharper disable once RedundantCast
            FireRatio = (float) upperStrCount / (float) alphaNumNoSpaceStrCount
        };

        Log.Debug("String analyzer result: {@Result}", result);

        return result;
    }

    public static CallbackDataParse ParseCallback(this string callbackData)
    {
        var callbackDataSplit = callbackData.SplitText(" ");
        var callbackCmd = callbackDataSplit.ElementAt(0);
        var callbackArgStr = callbackDataSplit.ElementAtOrDefault(1) ?? "";
        var callbackArgs = callbackArgStr.SplitText("_");

        var parseCallback = new CallbackDataParse()
        {
            CallbackData = callbackData,
            CallbackDataSplit = callbackDataSplit,
            CallbackDataCmd = callbackCmd,
            CallbackArgStr = callbackArgStr,
            CallbackArgs = callbackArgs
        };

        return parseCallback;
    }

    public static string NewGuid()
    {
        return Guid.NewGuid().ToString();
    }

    public static string RegexReplace(
        this string input,
        string pattern,
        string replacement
    )
    {
        return Regex.Replace(
            input: input,
            pattern: pattern,
            replacement: replacement
        );
    }
}
