using System;
using System.IO;
using CodingSeb.Localization.Loaders;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using WinTenDev.Zizi.Models.Configs;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class LocalizationLoaderExtension
{
    public static void LoadJsonLocalization(this IApplicationBuilder app)
    {
        var localizationConfig = app.GetRequiredService<IOptionsSnapshot<LocalizationConfig>>().Value;

        LocalizationLoader.Instance.FileLanguageLoaders.Add(new JsonFileLoader());
        var jsonLocalizationPath = localizationConfig.LangSourcePath;

        try
        {
            LocalizationLoader.Instance.AddDirectory(jsonLocalizationPath);
        }
        catch (Exception e)
        {
            // Log.Warning(e, "Error loading localization files");
            throw new InvalidDataException("Error loading localization files", e);
        }
    }
}