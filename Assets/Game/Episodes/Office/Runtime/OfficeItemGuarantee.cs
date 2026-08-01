using Jam.Core.Localization;
using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Гарантия личных вещей перед финальной зоной: если игрок дошёл до последнего
    /// порога без ноутбука или кружки, забытая вещь переносится на обязательный
    /// подход к `EXIT`. Пропустить её после этого невозможно.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class OfficeItemGuarantee : MonoBehaviour, IOfficeRunResettable
    {
        [Header("Сцена")]
        [SerializeField] private OfficeCollectible laptop;
        [SerializeField] private OfficeCollectible mug;
        [SerializeField] private Transform laptopFallback;
        [SerializeField] private Transform mugFallback;
        [SerializeField] private GameObject fallbackMarker;
        [SerializeField] private OfficeEpisodeController episodeController;
        [SerializeField] private OfficeCoach coach;

        [Header("Текст")]
        [SerializeField, Min(0.5f)] private float beatMessageDuration = 5f;

        private bool _triggered;

        private void Awake()
        {
            SetMarkerVisible(false);
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
            if (!_triggered || fallbackMarker == null || !fallbackMarker.activeSelf)
            {
                return;
            }

            var laptopReady = laptop == null || laptop.IsCollected;
            var mugReady = mug == null || mug.IsCollected;
            if (laptopReady && mugReady)
            {
                SetMarkerVisible(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered || other.GetComponentInParent<OfficePlayerController>() == null)
            {
                return;
            }

            _triggered = true;

            var moved = string.Empty;
            if (TryMove(laptop, laptopFallback))
            {
                moved = Loc.Get(LocalizationTables.Office, "item.laptop", "НОУТБУК");
            }

            if (TryMove(mug, mugFallback))
            {
                var mugName = Loc.Get(LocalizationTables.Office, "item.mug", "КРУЖКА");
                moved = string.IsNullOrEmpty(moved)
                    ? mugName
                    : Loc.Get(LocalizationTables.Office, "item.list_pair", "{0} И {1}", moved, mugName);
            }

            if (string.IsNullOrEmpty(moved))
            {
                return;
            }

            SetMarkerVisible(true);
            var message = Loc.Get(LocalizationTables.Office, "coach.fallback_items", "ЗАБЫТОЕ ЖДЁТ У EXIT: {0}", moved);
            episodeController?.ReportStoryBeat(message);
            coach?.ShowBeat(message, beatMessageDuration);
        }

        public void Configure(
            OfficeCollectible laptopItem,
            OfficeCollectible mugItem,
            Transform laptopAnchor,
            Transform mugAnchor,
            GameObject marker,
            OfficeEpisodeController controller,
            OfficeCoach routeCoach)
        {
            laptop = laptopItem;
            mug = mugItem;
            laptopFallback = laptopAnchor;
            mugFallback = mugAnchor;
            fallbackMarker = marker;
            episodeController = controller;
            coach = routeCoach;
        }

        public void ResetForRun()
        {
            _triggered = false;
            SetMarkerVisible(false);
        }

        private static bool TryMove(OfficeCollectible item, Transform anchor)
        {
            if (item == null || anchor == null || item.IsCollected)
            {
                return false;
            }

            item.MoveTo(anchor.position);
            return true;
        }

        private void SetMarkerVisible(bool value)
        {
            if (fallbackMarker != null)
            {
                fallbackMarker.SetActive(value);
            }
        }
    }
}
