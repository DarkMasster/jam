using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Аддитивная тряска камеры офисного эпизода. Компонент не ведёт камеру сам:
    /// он только добавляет смещение поверх результата <see cref="OfficeCameraFollow"/>.
    /// Порядок исполнения поднят, поэтому LateUpdate тряски выполняется после
    /// LateUpdate следования и смещение накладывается последним. Снятие прошлого
    /// смещения вынесено в Update: следование интерполирует камеру от её текущей
    /// позиции, и оставленный сдвиг тянул бы кадр за собой.
    /// </summary>
    [DefaultExecutionOrder(120)]
    public sealed class OfficeCameraShake : MonoBehaviour
    {
        [Header("Сцена")]
        [SerializeField] private Camera targetCamera;

        [Header("Пределы")]
        [SerializeField, Min(0f)] private float maxOffset = 0.35f;
        [SerializeField, Min(0.05f)] private float maxDuration = 0.6f;
        [SerializeField, Min(0f)] private float maxRollDegrees = 1.4f;
        [SerializeField, Min(0.1f)] private float noiseFrequency = 26f;

        private Transform _shakeTransform;
        private Vector3 _appliedOffset;
        private Quaternion _appliedRotation = Quaternion.identity;
        private bool _offsetApplied;
        private float _strength;
        private float _duration;
        private float _timeLeft;
        private int _shakeIndex;

        public bool IsShaking => _timeLeft > 0f;

        /// <summary>Остаток амплитуды текущей тряски, 0..1. По нему решается перебивание.</summary>
        public float CurrentStrength => _duration > 0f ? _strength * (_timeLeft / _duration) : 0f;

        private void Awake()
        {
            ResolveShakeTransform();
        }

        private void OnDisable()
        {
            // Компонент могут выключить в середине тряски: камера обязана вернуться
            // в чистую позицию, иначе смещение останется в кадре навсегда.
            RemoveAppliedOffset();
            _timeLeft = 0f;
            _strength = 0f;
            _duration = 0f;
        }

        private void Update()
        {
            RemoveAppliedOffset();
        }

        private void LateUpdate()
        {
            if (_timeLeft <= 0f)
            {
                return;
            }

            _timeLeft = Mathf.Max(0f, _timeLeft - Time.deltaTime);
            if (_timeLeft <= 0f)
            {
                _strength = 0f;
                _duration = 0f;
                return;
            }

            var fade = _timeLeft / _duration;
            // Квадратичное затухание: удар читается как резкий, а не как долгая вибрация.
            var amplitude = maxOffset * _strength * fade * fade;
            var elapsed = _duration - _timeLeft;
            var sample = (_shakeIndex * 13.37f) + (elapsed * noiseFrequency);

            var offset = new Vector3(
                SignedNoise(sample, 0.13f) * amplitude,
                SignedNoise(0.57f, sample) * amplitude * 0.6f,
                SignedNoise(sample, 4.19f) * amplitude * 0.45f);

            var roll = SignedNoise(sample, 8.31f) * maxRollDegrees * _strength * fade * fade;
            ApplyOffset(offset, Quaternion.AngleAxis(roll, Vector3.forward));
        }

        /// <summary>Editor-side wiring: камера, которой владеет тряска.</summary>
        public void Configure(Camera target)
        {
            RemoveAppliedOffset();
            targetCamera = target;
            _shakeTransform = null;
            ResolveShakeTransform();
        }

        /// <summary>
        /// Просит тряску силой <paramref name="strength"/> (0..1 от <c>maxOffset</c>)
        /// длительностью <paramref name="duration"/>. Более сильный запрос перебивает
        /// текущий, слабый — игнорируется: серия мелких попаданий не должна гасить
        /// удар босса, а совпавшие события не складываются в рывок камеры.
        /// </summary>
        public void Shake(float strength, float duration)
        {
            var clampedStrength = Mathf.Clamp01(strength);
            var clampedDuration = Mathf.Clamp(duration, 0f, maxDuration);
            if (clampedStrength <= 0f || clampedDuration <= 0f)
            {
                return;
            }

            if (_timeLeft > 0f && clampedStrength <= CurrentStrength)
            {
                return;
            }

            _strength = clampedStrength;
            _duration = clampedDuration;
            _timeLeft = clampedDuration;

            // Индекс сдвигает окно шума: соседние тряски не повторяют один рисунок,
            // но остаются детерминированными. Счётчик закольцован, чтобы аргумент
            // PerlinNoise не уходил в область, где теряется точность float.
            _shakeIndex = (_shakeIndex + 1) % 64;
        }

        /// <summary>Мгновенно снимает тряску; используется быстрым restart забега.</summary>
        public void StopShake()
        {
            RemoveAppliedOffset();
            _timeLeft = 0f;
            _strength = 0f;
            _duration = 0f;
        }

        private void ApplyOffset(Vector3 offset, Quaternion rotationDelta)
        {
            var shakeTransform = ResolveShakeTransform();
            if (shakeTransform == null)
            {
                return;
            }

            shakeTransform.position += offset;
            shakeTransform.rotation *= rotationDelta;

            _appliedOffset = offset;
            _appliedRotation = rotationDelta;
            _offsetApplied = true;
        }

        private void RemoveAppliedOffset()
        {
            if (!_offsetApplied)
            {
                return;
            }

            var shakeTransform = ResolveShakeTransform();
            if (shakeTransform != null)
            {
                shakeTransform.position -= _appliedOffset;
                shakeTransform.rotation *= Quaternion.Inverse(_appliedRotation);
            }

            _appliedOffset = Vector3.zero;
            _appliedRotation = Quaternion.identity;
            _offsetApplied = false;
        }

        private Transform ResolveShakeTransform()
        {
            if (_shakeTransform == null)
            {
                _shakeTransform = targetCamera != null ? targetCamera.transform : transform;
            }

            return _shakeTransform;
        }

        /// <summary>Perlin-шум в диапазоне -1..1; Random не используется намеренно.</summary>
        private static float SignedNoise(float x, float y)
        {
            return (Mathf.PerlinNoise(x, y) - 0.5f) * 2f;
        }
    }
}
