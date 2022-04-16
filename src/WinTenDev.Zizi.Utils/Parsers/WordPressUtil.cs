using System;
using System.Threading.Tasks;
using Serilog;
using WordPressPCL;

namespace WinTenDev.Zizi.Utils.Parsers;

public static class WordPressUtil
{
    public static async Task<bool> IsWordpress(this string url)
    {
        try
        {
            var uri = url.ParseUrl();
            var client = new WordPressClient(uri.Host);
            var post = await client.Posts.GetAllAsync();

            return true;
        }
        catch (Exception e)
        {
            Log.Warning("Unable to check if {Url} is a wordpress site", url);
            return false;
        }
    }
}
