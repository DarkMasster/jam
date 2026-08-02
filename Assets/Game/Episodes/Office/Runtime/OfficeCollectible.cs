using UnityEngine;

namespace Jam.Episodes.Office
{
    public enum OfficeCollectibleType
    {
        Laptop,
        Mug
    }

    [RequireComponent(typeof(Collider))]
    public sealed class OfficeCollectible : MonoBehaviour, IOfficeRunResettable
    {
        [SerializeField] private OfficeCollectibleType collectibleType;
        [SerializeField] private OfficeEpisodeController episodeController;
        [SerializeField] private Renderer pickupMarker;
        [SerializeField, Min(0f)] private float rotationSpeed = 55f;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.16f;
        [SerializeField, Min(0f)] private float bobFrequency = 2.2f;

        private Vector3 _spawnPosition;
        private Vector3 _basePosition;
        private Vector3 _markerSpawnPosition;
        private Collider _trigger;
        private Renderer[] _renderers;
        private bool _collected;
        private bool _missingControllerReported;

        public OfficeCollectibleType CollectibleType => collectibleType;

        public bool IsCollected => _collected;

        private void Awake()
        {
            _spawnPosition = transform.position;
            _basePosition = _spawnPosition;
            if (pickupMarker != null)
            {
                _markerSpawnPosition = pickupMarker.transform.position;
            }

            _trigger = GetComponent<Collider>();
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

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
            if (_collected)
            {
                return;
            }

            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            var position = _basePosition;
            position.y += Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.position = position;
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<OfficePlayerController>();
            if (_collected || player == null)
            {
                return;
            }

            if (episodeController == null)
            {
                if (!_missingControllerReported)
                {
                    Debug.LogError($"{nameof(OfficeCollectible)} on '{name}' has no episode controller assigned.", this);
                    _missingControllerReported = true;
                }

                return;
            }

            _collected = true;
            episodeController.RegisterCollectible(collectibleType);
            OfficeFeedback.Instance?.ReportCollectiblePickup(
                player.transform,
                collectibleType,
                episodeController.BossEncounterReady);
            // Объект остаётся включённым, иначе быстрый restart не сможет его вернуть.
            SetPresent(false);
        }

        public void Configure(
            OfficeEpisodeController controller,
            OfficeCollectibleType type,
            Renderer marker = null)
        {
            episodeController = controller;
            collectibleType = type;
            if (marker != null)
            {
                pickupMarker = marker;
            }
        }

        /// <summary>
        /// Переносит несобранную вещь на гарантированную точку маршрута.
        /// Стартовая позиция сохраняется и возвращается при перезапуске забега.
        /// </summary>
        public void MoveTo(Vector3 position)
        {
            var offset = position - _basePosition;
            _basePosition = position;
            transform.position = position;

            if (pickupMarker != null)
            {
                pickupMarker.transform.position += offset;
            }
        }

        public void ResetForRun()
        {
            _basePosition = _spawnPosition;
            transform.position = _basePosition;
            if (pickupMarker != null)
            {
                pickupMarker.transform.position = _markerSpawnPosition;
                pickupMarker.enabled = true;
            }

            if (!_collected)
            {
                return;
            }

            _collected = false;
            SetPresent(true);
        }

        private void SetPresent(bool value)
        {
            if (_trigger != null)
            {
                _trigger.enabled = value;
            }

            if (pickupMarker != null)
            {
                pickupMarker.enabled = value;
            }

            if (_renderers == null)
            {
                return;
            }

            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].enabled = value;
                }
            }
        }
    }
}
