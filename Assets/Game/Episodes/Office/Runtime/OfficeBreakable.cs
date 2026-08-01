using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Подготовленный офисный объект с двумя состояниями: <c>Intact</c> и <c>Broken</c>.
    /// Свободной фрагментации нет — меняется только заранее собранная геометрия.
    /// </summary>
    public sealed class OfficeBreakable : MonoBehaviour
    {
        [SerializeField] private string displayName = "ПРИНТЕР";
        [SerializeField] private GameObject intactState;
        [SerializeField] private GameObject brokenState;
        [SerializeField] private BoxCollider bodyCollider;
        [SerializeField] private Light impactFlash;
        [SerializeField] private OfficeEpisodeController episodeController;
        [SerializeField, Min(0f)] private float breakImpactSpeed = 6f;
        [SerializeField, Min(0f)] private float flashIntensity = 7f;
        [SerializeField, Min(0.05f)] private float flashDuration = 0.45f;

        private float _flashTimeLeft;

        public bool IsBroken { get; private set; }

        private void Awake()
        {
            ApplyState();

            if (impactFlash != null)
            {
                impactFlash.intensity = 0f;
            }
        }

        private void Start()
        {
            episodeController?.RegisterBreakableTarget();
        }

        private void Update()
        {
            if (impactFlash == null || _flashTimeLeft <= 0f)
            {
                return;
            }

            _flashTimeLeft -= Time.deltaTime;
            impactFlash.intensity = _flashTimeLeft <= 0f
                ? 0f
                : flashIntensity * (_flashTimeLeft / flashDuration);
        }

        /// <summary>
        /// Разрушает объект, если удар достаточно сильный. Возвращает <c>true</c>,
        /// только когда состояние действительно сменилось.
        /// </summary>
        public bool TryBreak(float impactSpeed)
        {
            if (IsBroken || impactSpeed < breakImpactSpeed)
            {
                return false;
            }

            IsBroken = true;
            ApplyState();
            _flashTimeLeft = flashDuration;

            if (impactFlash != null)
            {
                impactFlash.intensity = flashIntensity;
            }

            episodeController?.RegisterBreakableDestroyed(displayName);
            return true;
        }

        public void Configure(
            string itemName,
            GameObject intact,
            GameObject broken,
            BoxCollider body,
            Light flash,
            OfficeEpisodeController controller)
        {
            displayName = itemName;
            intactState = intact;
            brokenState = broken;
            bodyCollider = body;
            impactFlash = flash;
            episodeController = controller;
        }

        public void SetEpisodeController(OfficeEpisodeController controller)
        {
            episodeController = controller;
        }

        private void ApplyState()
        {
            if (intactState != null)
            {
                intactState.SetActive(!IsBroken);
            }

            if (brokenState != null)
            {
                brokenState.SetActive(IsBroken);
            }

            if (bodyCollider == null || !IsBroken)
            {
                return;
            }

            // Обломки ниже целого объекта: снижаем блокирующий объём, чтобы брошенные
            // предметы перелетали через них.
            var size = bodyCollider.size;
            var center = bodyCollider.center;
            bodyCollider.size = new Vector3(size.x, size.y * 0.4f, size.z);
            bodyCollider.center = new Vector3(center.x, center.y * 0.4f, center.z);
        }
    }
}
