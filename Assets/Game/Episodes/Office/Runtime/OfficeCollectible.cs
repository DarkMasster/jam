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
        [SerializeField, Min(0f)] private float rotationSpeed = 55f;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.16f;
        [SerializeField, Min(0f)] private float bobFrequency = 2.2f;

        private Vector3 _basePosition;
        private Collider _trigger;
        private Renderer[] _renderers;
        private bool _collected;
        private bool _missingControllerReported;

        private void Awake()
        {
            _basePosition = transform.position;
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
            if (_collected || other.GetComponentInParent<OfficePlayerController>() == null)
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
            // Объект остаётся включённым, иначе быстрый restart не сможет его вернуть.
            SetPresent(false);
        }

        public void Configure(OfficeEpisodeController controller, OfficeCollectibleType type)
        {
            episodeController = controller;
            collectibleType = type;
        }

        public void ResetForRun()
        {
            if (!_collected)
            {
                return;
            }

            _collected = false;
            transform.position = _basePosition;
            SetPresent(true);
        }

        private void SetPresent(bool value)
        {
            if (_trigger != null)
            {
                _trigger.enabled = value;
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
