using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;

namespace WinTenDev.Zizi.Utils
{
    public static class ValueUtil
    {
        public static bool IsDefault<T>(this T value) where T : struct
        {
            bool isDefault = value.Equals(default(T));

            return isDefault;
        }

        public static bool IsNull<T, TU>(this KeyValuePair<T, TU> pair)
        {
            return pair.Equals(new KeyValuePair<T, TU>());
        }

        public static bool AnyOrNotNull<T>(this IEnumerable<T> source)
        {
            return source?.Any() == true;
        }

        public static int CountOrZero<T>(this List<T> source)
        {
            return source?.Count ?? 0;
        }

        public static List<T> ToListOrEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null) return new List<T>();

            return source.ToList();
        }

        public static InputMedia ToInputMedia(
            this string stringData,
            string fileName
        )
        {
            return new InputMedia(stringData.ToStream(), fileName);
        }
    }
}