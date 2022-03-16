using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace WinTenDev.Zizi.Utils.IO;

public static class FileUtil
{
    public static void DeleteFile(this string filePath)
    {
        if (!File.Exists(filePath)) return;

        try
        {
            Log.Information("Deleting {FilePath}", filePath);
            File.Delete(filePath);

            Log.Information("File {FilePath} deleted successfully", filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error Deleting file {FilePath}", filePath);
        }
    }

    public static string ReplaceExt(
        this string fileName,
        string toExt
    )
    {
        return Path.ChangeExtension(fileName, toExt);
    }

    public static async Task WriteTextAsync(
        this string content,
        string filePath
    )
    {
        var cachePath = "Storage/Caches";

        filePath = $"{cachePath}/{filePath}";
        Log.Information("Writing content to {FilePath}", filePath);

        filePath.SanitizeSlash().EnsureDirectory();

        await File.WriteAllTextAsync(filePath, content);

        Log.Information("Writing file success..");
    }

    public static async Task<string> ReadTextAsync(this string filePath)
    {
        var cachePath = "Storage/Caches";

        filePath = $"{cachePath}/{filePath}";
        if (!filePath.IsFileExist()) return string.Empty;

        Log.Debug("Reading content to {FilePath}", filePath);

        var text = await File.ReadAllTextAsync(filePath);
        return text;
    }

    public static string ReadText(this string filePath)
    {
        var cachePath = "Storage/Caches";

        filePath = $"{cachePath}/{filePath}";
        Log.Debug("Reading content to {FilePath}", filePath);

        // ReSharper disable once MethodHasAsyncOverload
        var text = File.ReadAllText(filePath);

        return text;
    }

    public static long FileSize(this string filePath)
    {
        return filePath.FileInfo().Length;
    }

    public static FileInfo FileInfo(this string filePath)
    {
        return new FileInfo(filePath);
    }

    public static bool IsFileExist(this string filePath)
    {
        return File.Exists(filePath);
    }

    public static bool IsDirExist(this string filePath)
    {
        return Directory.Exists(filePath);
    }

    public static IEnumerable<string> EnumerateFiles(
        this string path,
        string searchPattern = "*",
        bool recursive = false
    )
    {
        var files = Directory.EnumerateFiles
        (
            path, searchPattern, new EnumerationOptions
            {
                RecurseSubdirectories = recursive
            }
        );

        return files;
    }
}