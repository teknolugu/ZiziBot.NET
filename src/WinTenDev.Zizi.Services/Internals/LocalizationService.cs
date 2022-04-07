using System;
using System.Collections.Generic;
using CodingSeb.Localization;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Internals
{
    public class LocalizationService
    {
        public string GetLoc(
            string langCode,
            Enum enumPath,
            IEnumerable<(string placeholder, string value)> placeHolders = null
        )
        {
            Loc.Instance.CurrentLanguage = langCode;
            var path = enumPath.ToNameValue();
            var localized = Loc.Tr(path);

            return placeHolders == null ? localized : localized.ResolveVariable(placeHolders);
        }
    }
}
