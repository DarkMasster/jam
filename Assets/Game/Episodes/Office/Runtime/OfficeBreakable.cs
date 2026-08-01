using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Подготовленный офисный объект с двумя состояниями: <c>Intact</c> и <c>Broken</c>.
    /// Свободной фрагментации нет — меняется только заранее собранная геометрия.
    /// </summary>
    public sealed class OfficeBreakable : MonoBehaviour, IOfficeImpactTarget, IOfficeRunResettable
    {
        [SerializeField] private string displayName = "ПРИНТЕР";
        [SerializeField] private GameObject intactState;
        [SerializeField] private GameObject brokenState;
        [SerializeField] private BoxCollider bodyCollider;
        [SerializeField] private Light impactFlash;
        [SerializeField] private OfficeEpisodeController episodeController;
        [SerializeField] private OfficeMomentum momentum;
        [SerializeField, Min(0f)] private float breakImpactSpeed = 6f;
        [SerializeField, Min(0f)] private float flashIntensity = 7f;
        [SerializeField, Min(0.05f)] private float flashDuration = 0.45f;

        private float _flashTimeLeft;
        private Vector3 _intactColliderSize;
        private Vector3 _intactColliderCenter;

        public bool IsBroken { get; private set; }

        private void Awake()
        {
            if (bodyCollider != null)
            {
                _intactColliderSize = bodyCollider.size;
                _intactColliderCenter = bodyCollider.center;
            }

            ApplyState();

            if (impactFlash != null)
            {
                impactFlash.intensity = 0f;
            }
        }

        private void OnEnable()
        {
            OfficeRunReset.Register(this);
        }

        private void OnDisable()
        {
            OfficeRunReset.Unregister(this);
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

            momentum?.AddBreak();
            episodeController?.RegisterBreakableDestroyed(displayName);
            return true;
        }

        public bool TryTakeImpact(float impactSpeed)
        {
            return TryBreak(impactSpeed);
        }

        public void ResetForRun()
        {
            if (!IsBroken)
            {
                return;
            }

            IsBroken = false;
            _flashTimeLeft = 0f;

            if (impactFlash != null)
            {
                impactFlash.intensity = 0f;
            }

            ApplyState();
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

        public void SetSceneReferences(OfficeEpisodeController controller, OfficeMomentum momentumScale)
        {
            episodeController = controller;
            momentum = momentumScale;
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

            if (bodyCollider == null)
            {
                return;
            }

            if (!IsBroken)
            {
                bodyCollider.size = _intactColliderSize;
                bodyCollider.center = _intactColliderCenter;
                return;
            }

            // Обломки ниже целого объекта: снижаем блокирующий объём, чтобы брошенные
            // предметы перелетали через них.
            bodyCollider.size = new Vector3(
                _intactColliderSize.x,
                _intactColliderSize.y * 0.4f,
                _intactColliderSize.z);
            bodyCollider.center = new Vector3(
                _intactColliderCenter.x,
                _intactColliderCenter.y * 0.4f,
                _intactColliderCenter.z);
        }
    }
}
