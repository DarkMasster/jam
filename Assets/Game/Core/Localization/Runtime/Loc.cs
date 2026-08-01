using System;
using UnityEngine;

namespace Jam.Core.Localization
{
    public static class Loc
    {
        private static readonly ILocalizationService _service = new UnityLocalizationService();

        public static event Action LocaleChanged
        {
            add => _service.LocaleChanged += value;
            remove => _service.LocaleChanged -= value;
        }

        public static string CurrentLocaleCode => _service.CurrentLocaleCode;

        public static void InitializeFromPreferences()
        {
            _ = _service.CurrentLocaleCode;
            _service.SetLocale(PlayerPrefs.GetString("jam.settings.locale", "ru"));
        }

        public static string Get(string table, string key, string fallback = null, params object[] arguments)
        {
            return _service.Get(table, key, fallback, arguments);
        }

        public static bool SetLocale(string localeCode)
        {
            return _service.SetLocale(localeCode);
        }

        public static bool ToggleRussianEnglish()
        {
            return SetLocale(CurrentLocaleCode == "ru" ? "en" : "ru");
        }
    }
}
