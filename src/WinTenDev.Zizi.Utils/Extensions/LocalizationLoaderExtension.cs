using System;
using CodingSeb.Localization.Loaders;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Exceptions;
using WinTenDev.Zizi.Models.Factories;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class LocalizationLoaderExtension
{
    public static void LoadJsonLocalization(this IApplicationBuilder app)
    {
        var localizationConfig = app.GetRequiredService<IOptionsSnapshot<LocalizationConfig>>().Value;

        LocalizationLoader.Instance.FileLanguageLoaders.Add(new AdvancedJsonLocalizationFileLoaderFactory());
        var jsonLocalizationPath = localizationConfig.LangSourcePath;

        try
        {
            LocalizationLoader.Instance.AddDirectory(jsonLocalizationPath);
        }
        catch (Exception exception)
        {
            throw new InvalidJsonLocalizationException(exception);
        }
    }
}
