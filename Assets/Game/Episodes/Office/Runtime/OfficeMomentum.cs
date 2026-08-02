using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Momentum забега: растёт от разрушений и побед над техникой, падает при простое.
    /// Это не здоровье — шкала влияет только на скорость, силу броска и подачу.
    /// </summary>
    public sealed class OfficeMomentum : MonoBehaviour, IOfficeRunResettable
    {
        [Header("Прирост")]
        [SerializeField, Range(0f, 1f)] private float breakGain = 0.26f;
        [SerializeField, Range(0f, 1f)] private float enemyGain = 0.34f;

        [Header("Падение")]
        [SerializeField, Min(0f)] private float movingDecayPerSecond = 0.055f;
        [SerializeField, Min(0f)] private float idleDecayPerSecond = 0.26f;
        [SerializeField, Min(0f)] private float idleSpeedThreshold = 1.2f;

        [Header("Влияние на героя")]
        [SerializeField, Min(1f)] private float maxSpeedMultiplier = 1.32f;
        [SerializeField, Min(1f)] private float maxThrowMultiplier = 1.4f;

        private float _observedSpeed;

        /// <summary>Нормализованное значение шкалы, 0..1.</summary>
        public float Value { get; private set; }

        public bool IsIdle => _observedSpeed < idleSpeedThreshold;

        public float SpeedMultiplier => Mathf.Lerp(1f, maxSpeedMultiplier, Value);

        public float ThrowMultiplier => Mathf.Lerp(1f, maxThrowMultiplier, Value);

        private void OnEnable()
        {
            OfficeRunReset.Register(this);
        }

        private void OnDisable()
        {
            OfficeRunReset.Unregister(this);
        }

        private void Update()
        {
            var decay = IsIdle ? idleDecayPerSecond : movingDecayPerSecond;
            Value = Mathf.Clamp01(Value - (decay * Time.deltaTime));
        }

        /// <summary>Скорость героя в плоскости XZ; простой считается по ней.</summary>
        public void ReportPlanarSpeed(float speed)
        {
            _observedSpeed = speed;
        }

        public void AddBreak()
        {
            Value = Mathf.Clamp01(Value + breakGain);
        }

        /// <summary>
        /// Разрушение с собственным вкладом объекта. Общая константа рассчитана на
        /// четыре принтера: на всех разрушаемых объектах маршрута шкала заполнялась
        /// бы с первых секунд и перестала быть ресурсом темпа. Неположительный
        /// <paramref name="gain"/> означает «взять значение по умолчанию».
        /// </summary>
        public void AddBreak(float gain)
        {
            Value = Mathf.Clamp01(Value + (gain > 0f ? gain : breakGain));
        }

        public void AddEnemyDefeated()
        {
            Value = Mathf.Clamp01(Value + enemyGain);
        }

        public void ResetForRun()
        {
            Value = 0f;
            _observedSpeed = 0f;
        }
    }
}
