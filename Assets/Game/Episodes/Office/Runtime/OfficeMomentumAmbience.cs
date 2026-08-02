using System;
using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Свет окружения, ведомый шкалой <see cref="OfficeMomentum"/>: красные акценты
    /// разгораются и слегка «нагреваются» от <c>#D8241D</c> к <c>#FF5A3C</c>, а холодный
    /// ключевой свет только набирает яркость. Компонент не трогает RenderSettings:
    /// ambient задан билдером сцены и является нижней границей читаемости.
    /// </summary>
    public sealed class OfficeMomentumAmbience : MonoBehaviour
    {
        [Header("Сцена")]
        [SerializeField] private OfficeMomentum momentum;
        [SerializeField] private Light[] accentLights = Array.Empty<Light>();
        [SerializeField] private Light keyLight;

        [Header("Красные акценты")]
        // Красный только ДОБАВЛЯЕТ: интенсивность идёт от базы вверх, поэтому телеграфы
        // и путь к выходу остаются акцентом, а не заливкой кадра.
        [SerializeField, Min(0f)] private float accentMinIntensity = 1.2f;
        [SerializeField, Min(0f)] private float accentMaxIntensity = 2.6f;
        [SerializeField] private Color accentBaseColor = new Color32(0xD8, 0x24, 0x1D, 0xFF);
        [SerializeField] private Color accentHotColor = new Color32(0xFF, 0x5A, 0x3C, 0xFF);
        [SerializeField, Range(0f, 1f)] private float accentHotBlend = 0.65f;

        [Header("Ключевой свет")]
        // Читаемость важнее эффекта: пол и красные полосы телеграфа должны различаться
        // на любом значении шкалы. Поэтому ключевой свет никогда не опускается ниже
        // базовой яркости — Momentum умеет только прибавить к ней разницу max - min.
        [SerializeField, Min(0f)] private float keyMinIntensity = 0.5f;
        [SerializeField, Min(0f)] private float keyMaxIntensity = 0.72f;

        [Header("Сглаживание")]
        [SerializeField, Min(0.1f)] private float smoothingSpeed = 3.2f;

        private float[] _accentBaseIntensity = Array.Empty<float>();
        private float _keyBaseIntensity;
        private float _smoothedValue;
        private float _appliedValue = -1f;
        private bool _baseCaptured;

        /// <summary>Сглаженное значение шкалы, которым сейчас ведётся свет.</summary>
        public float SmoothedValue => _smoothedValue;

        private void Awake()
        {
            CaptureBaseIntensities();
            _smoothedValue = momentum != null ? momentum.Value : 0f;
            ApplyLighting(_smoothedValue);
        }

        private void Update()
        {
            var target = momentum != null ? momentum.Value : 0f;

            // Кадронезависимое сглаживание тем же приёмом, что и следование камеры:
            // шкала прыгает от разрушений, свет не должен мигать вместе с ней.
            var blend = 1f - Mathf.Exp(-smoothingSpeed * Time.deltaTime);
            _smoothedValue = Mathf.Lerp(_smoothedValue, target, blend);
            ApplyLighting(_smoothedValue);
        }

        /// <summary>Editor-side wiring: шкала, красные акценты и ключевой свет сцены.</summary>
        public void Configure(OfficeMomentum momentumScale, Light[] accents, Light key)
        {
            momentum = momentumScale;
            accentLights = accents ?? Array.Empty<Light>();
            keyLight = key;
            _baseCaptured = false;
            CaptureBaseIntensities();
            _appliedValue = -1f;
        }

        /// <summary>
        /// База снимается один раз и не ниже настроенного минимума: повторный захват
        /// уже после того, как компонент начал вести свет, поднимал бы минимум с каждым
        /// вызовом и постепенно засвечивал сцену.
        /// </summary>
        private void CaptureBaseIntensities()
        {
            if (_baseCaptured)
            {
                return;
            }

            _baseCaptured = true;
            accentLights ??= Array.Empty<Light>();
            _keyBaseIntensity = keyLight != null
                ? Mathf.Max(keyMinIntensity, keyLight.intensity)
                : keyMinIntensity;

            _accentBaseIntensity = new float[accentLights.Length];
            for (var i = 0; i < accentLights.Length; i++)
            {
                _accentBaseIntensity[i] = accentLights[i] != null
                    ? Mathf.Max(accentMinIntensity, accentLights[i].intensity)
                    : accentMinIntensity;
            }
        }

        private void ApplyLighting(float value)
        {
            if (Mathf.Abs(value - _appliedValue) < 0.001f)
            {
                return;
            }

            _appliedValue = value;
            var normalized = Mathf.Clamp01(value);

            var accentHeadroom = Mathf.Max(0f, accentMaxIntensity - accentMinIntensity);
            var accentColor = Color.Lerp(accentBaseColor, accentHotColor, normalized * accentHotBlend);

            var count = Mathf.Min(accentLights.Length, _accentBaseIntensity.Length);
            for (var i = 0; i < count; i++)
            {
                var accent = accentLights[i];
                if (accent == null)
                {
                    continue;
                }

                accent.intensity = _accentBaseIntensity[i] + (accentHeadroom * normalized);
                accent.color = accentColor;
            }

            if (keyLight != null)
            {
                var keyHeadroom = Mathf.Max(0f, keyMaxIntensity - keyMinIntensity);
                keyLight.intensity = _keyBaseIntensity + (keyHeadroom * normalized);
            }
        }
    }
}
