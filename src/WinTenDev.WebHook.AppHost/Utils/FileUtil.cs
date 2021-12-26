using System.IO;

namespace WinTenDev.WebHook.AppHost.Utils
{
    public static class FileUtil
    {
        public static void WriteText(this string content, string fileName)
        {
            File.WriteAllText(fileName, content);
        }
    }
}