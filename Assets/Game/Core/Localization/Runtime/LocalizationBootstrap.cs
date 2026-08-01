using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Jam.Core.Localization
{
    [DefaultExecutionOrder(-900)]
    public sealed class LocalizationBootstrap : MonoBehaviour
    {
        private const string LocalePreferenceKey = "jam.settings.locale";

        private IEnumerator Start()
        {
            yield return LocalizationSettings.InitializationOperation;
            yield return null;
            Loc.SetLocale(PlayerPrefs.GetString(LocalePreferenceKey, "ru"));
        }
    }
}
