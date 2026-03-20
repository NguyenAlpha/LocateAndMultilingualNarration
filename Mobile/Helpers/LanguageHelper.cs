using Microsoft.Maui.Storage;
using System.Collections.Generic;

namespace Mobile.Helpers
{
    public static class LanguageHelper
    {
        const string LanguageKey = "app_selected_language";

        static readonly Dictionary<string, string> LanguageNames = new()
        {
            { "vi", "🇻🇳 Tiếng Việt" },
            { "en", "🇺🇸 English" },
            { "ja", "🇯🇵 日本語" },
            { "ko", "🇰🇷 한국어" }
        };

        public static void SetLanguage(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                Preferences.Remove(LanguageKey);
                return;
            }

            Preferences.Set(LanguageKey, code);
        }

        public static string? GetLanguage()
        {
            return Preferences.ContainsKey(LanguageKey) ? Preferences.Get(LanguageKey, string.Empty) : null;
        }

        public static string GetLanguageDisplay()
        {
            var code = GetLanguage();
            if (string.IsNullOrEmpty(code))
                return "Chưa chọn";

            return LanguageNames.TryGetValue(code, out var name) ? name : code;
        }
    }
}
