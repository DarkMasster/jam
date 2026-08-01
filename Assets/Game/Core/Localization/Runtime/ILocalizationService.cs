using System;

namespace Jam.Core.Localization
{
    public interface ILocalizationService
    {
        event Action LocaleChanged;

        string CurrentLocaleCode { get; }
        string Get(string table, string key, string fallback = null, params object[] arguments);
        bool SetLocale(string localeCode);
    }
}
