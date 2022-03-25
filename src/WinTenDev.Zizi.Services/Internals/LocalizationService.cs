using System;
using CodingSeb.Localization;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Internals
{
    public class LocalizationService
    {
        public string GetLoc(
            string langCode,
            Enum enumPath
        )
        {
            Loc.Instance.CurrentLanguage = langCode;
            var path = enumPath.ToNameValue();
            var localized = Loc.Tr(path);

            return localized;
        }
    }
}
