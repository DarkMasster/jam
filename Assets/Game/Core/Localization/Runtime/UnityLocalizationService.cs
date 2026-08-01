using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Jam.Core.Localization
{
    public sealed class UnityLocalizationService : ILocalizationService
    {
        private const string LocalePreferenceKey = "jam.settings.locale";
        private const string DefaultLocale = "ru";

        private bool _initialized;
        private bool _subscribed;
        private string _lastNotifiedLocaleCode;

        public event Action LocaleChanged;

        public string CurrentLocaleCode
        {
            get
            {
                EnsureInitialized();
                return LocalizationSettings.SelectedLocale?.Identifier.Code ?? DefaultLocale;
            }
        }

        public string Get(string table, string key, string fallback = null, params object[] arguments)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return fallback ?? string.Empty;
            }

            try
            {
                EnsureInitialized();
                var operation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                    table,
                    key,
                    LocalizationSettings.SelectedLocale,
                    FallbackBehavior.UseProjectSettings,
                    arguments ?? Array.Empty<object>());
                var value = operation.WaitForCompletion();

                return string.IsNullOrWhiteSpace(value) ? fallback ?? key : value;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Localization fallback for '{table}/{key}': {exception.Message}");
                return fallback ?? key;
            }
        }

        public bool SetLocale(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
            {
                return false;
            }

            EnsureInitialized();
            var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(localeCode));
            if (locale == null)
            {
                Debug.LogWarning($"Locale '{localeCode}' is not configured.");
                return false;
            }

            if (LocalizationSettings.SelectedLocale == locale)
            {
                return true;
            }

            LocalizationSettings.SelectedLocale = locale;
            PlayerPrefs.SetString(LocalePreferenceKey, locale.Identifier.Code);
            PlayerPrefs.Save();
            NotifyLocaleReady(locale);
            return true;
        }

        internal void Dispose()
        {
            if (_subscribed)
            {
                LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
                _subscribed = false;
            }

            LocaleChanged = null;
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            var initialization = LocalizationSettings.InitializationOperation;
            if (!initialization.IsDone)
            {
                initialization.WaitForCompletion();
            }

            if (!_subscribed)
            {
                LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
                _subscribed = true;
            }

            var requestedCode = PlayerPrefs.GetString(LocalePreferenceKey, DefaultLocale);
            var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(requestedCode))
                         ?? LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(DefaultLocale));
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
            }

            _initialized = true;
        }

        private void HandleLocaleChanged(Locale locale)
        {
            if (locale != null)
            {
                PlayerPrefs.SetString(LocalePreferenceKey, locale.Identifier.Code);
                PlayerPrefs.Save();
            }
        }

        private void NotifyLocaleReady(Locale locale)
        {
            var localeCode = locale?.Identifier.Code;
            if (!string.IsNullOrWhiteSpace(localeCode) && _lastNotifiedLocaleCode == localeCode)
            {
                return;
            }

            if (locale == null)
            {
                LocaleChanged?.Invoke();
                return;
            }

            var commonTable = LocalizationSettings.StringDatabase.GetTableAsync(
                LocalizationTables.Common,
                locale);
            if (!commonTable.IsDone)
            {
                commonTable.WaitForCompletion();
            }

            _lastNotifiedLocaleCode = localeCode;
            LocaleChanged?.Invoke();
        }
    }
}
