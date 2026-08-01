using UnityEngine;

namespace Jam.Episodes.Office
{
    [RequireComponent(typeof(Collider))]
    public sealed class OfficeZoneTrigger : MonoBehaviour
    {
        [SerializeField] private OfficeEpisodeController episodeController;
        [SerializeField] private string zoneName;

        private bool _missingControllerReported;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<OfficePlayerController>() != null)
            {
                if (episodeController == null)
                {
                    if (!_missingControllerReported)
                    {
                        Debug.LogError($"{nameof(OfficeZoneTrigger)} on '{name}' has no episode controller assigned.", this);
                        _missingControllerReported = true;
                    }

                    return;
                }

                episodeController.EnterZone(zoneName);
            }
        }

        public void Configure(OfficeEpisodeController controller, string displayName)
        {
            episodeController = controller;
            zoneName = displayName;
        }
    }
}
