using CodingSeb.Localization.Loaders;

namespace WinTenDev.Zizi.Utils;

public static class LocalizationLoaderUtil
{
    public static void LoadJsonLocalizationLang()
    {
        LocalizationLoader.Instance.FileLanguageLoaders.Add(new JsonFileLoader());

        LocalizationLoader.Instance.AddDirectory("Storage/Language/Json");
    }
}
