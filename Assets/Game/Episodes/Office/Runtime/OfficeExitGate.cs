using UnityEngine;

namespace Jam.Episodes.Office
{
    [RequireComponent(typeof(Collider))]
    public sealed class OfficeExitGate : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private OfficeEpisodeController episodeController;
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Color lockedColor = new(0.43f, 0.08f, 0.07f, 1f);
        [SerializeField] private Color readyColor = new(0.85f, 0.14f, 0.11f, 1f);

        private MaterialPropertyBlock _propertyBlock;
        private bool _isReady;
        private float _contactPulseUntil;
        private bool _missingControllerReported;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (indicatorRenderer == null)
            {
                return;
            }

            var pulse = _isReady
                ? 0.75f + (Mathf.Sin(Time.time * 7f) * 0.25f)
                : Time.time < _contactPulseUntil
                    ? 1f
                    : 0.45f;

            var color = (_isReady ? readyColor : lockedColor) * pulse;
            color.a = 1f;
            _propertyBlock ??= new MaterialPropertyBlock();
            indicatorRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(EmissionColorId, color * 2.2f);
            indicatorRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<OfficePlayerController>() == null)
            {
                return;
            }

            _contactPulseUntil = Time.time + 0.8f;
            if (episodeController == null)
            {
                if (!_missingControllerReported)
                {
                    Debug.LogError($"{nameof(OfficeExitGate)} on '{name}' has no episode controller assigned.", this);
                    _missingControllerReported = true;
                }

                return;
            }

            episodeController.HandleExitAttempt();
        }

        public void Configure(OfficeEpisodeController controller, Renderer indicator)
        {
            episodeController = controller;
            indicatorRenderer = indicator;
        }

        public void SetReady(bool value)
        {
            _isReady = value;
        }
    }
}
