using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Jam.Core.Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedTextBinding : MonoBehaviour
    {
        [SerializeField] private string table = LocalizationTables.Common;
        [SerializeField] private string key;
        [SerializeField, TextArea] private string fallback;

        private TMP_Text _target;

        public static LocalizedTextBinding Attach(
            TMP_Text target,
            string tableName,
            string entryKey,
            string fallbackText)
        {
            var binding = target.GetComponent<LocalizedTextBinding>();
            if (binding == null)
            {
                binding = target.gameObject.AddComponent<LocalizedTextBinding>();
            }

            binding.Configure(tableName, entryKey, fallbackText);
            return binding;
        }

        public void Configure(string tableName, string entryKey, string fallbackText)
        {
            table = tableName;
            key = entryKey;
            fallback = fallbackText;
            _target ??= GetComponent<TMP_Text>();
            Refresh();
        }

        private void OnEnable()
        {
            _target ??= GetComponent<TMP_Text>();
            Loc.LocaleChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Loc.LocaleChanged -= Refresh;
        }

        private IEnumerator Start()
        {
            yield return LocalizationSettings.InitializationOperation;
            yield return null;
            Refresh();
        }

        public void Refresh()
        {
            if (_target != null && !string.IsNullOrWhiteSpace(key))
            {
                _target.text = Loc.Get(table, key, fallback);
            }
        }
    }
}
