using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Минимальный Reflection beat у стеклянной переговорной: за стеклом с задержкой
    /// идёт силуэт героя и один раз за забег произносится строка. Полноценное
    /// призрачное эхо в джемовый scope не входит и сокращается первым.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class OfficeReflectionBeat : MonoBehaviour, IOfficeRunResettable
    {
        [Header("Сцена")]
        [SerializeField] private Transform echo;
        [SerializeField] private Transform player;
        [SerializeField] private OfficeEpisodeController episodeController;
        [SerializeField] private OfficeCoach coach;

        [Header("Отражение")]
        [SerializeField] private float echoPlaneX = 8.7f;
        [SerializeField] private float roomMinZ = -5.2f;
        [SerializeField] private float roomMaxZ = 6.2f;
        [SerializeField, Min(0.1f)] private float followLag = 3.2f;
        [SerializeField, Min(0.5f)] private float holdDuration = 7f;

        [Header("Текст")]
        [SerializeField] private string beatMessage = "ОТРАЖЕНИЕ ИДЁТ РЯДОМ • ЭТО ВСЁ ЕЩЁ ТЫ";
        [SerializeField, Min(0.5f)] private float beatMessageDuration = 4.5f;

        private Renderer[] _echoRenderers;
        private bool _played;
        private float _visibleUntil;

        private void Awake()
        {
            _echoRenderers = echo != null ? echo.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            SetEchoVisible(false);
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
            if (echo == null || player == null)
            {
                return;
            }

            if (Time.time >= _visibleUntil)
            {
                SetEchoVisible(false);
                return;
            }

            var target = new Vector3(echoPlaneX, player.position.y, Mathf.Clamp(player.position.z, roomMinZ, roomMaxZ));
            echo.position = Vector3.Lerp(echo.position, target, 1f - Mathf.Exp(-followLag * Time.deltaTime));

            // Силуэт смотрит по зеркальному направлению героя, поэтому эхо повторяет
            // его повороты, а не следит за ним.
            var forward = player.forward;
            forward.x = -forward.x;
            if (forward.sqrMagnitude > 0.001f)
            {
                echo.rotation = Quaternion.Slerp(
                    echo.rotation,
                    Quaternion.LookRotation(forward, Vector3.up),
                    1f - Mathf.Exp(-followLag * Time.deltaTime));
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_played || other.GetComponentInParent<OfficePlayerController>() == null)
            {
                return;
            }

            _played = true;
            _visibleUntil = Time.time + holdDuration;

            if (echo != null && player != null)
            {
                echo.position = new Vector3(echoPlaneX, player.position.y, Mathf.Clamp(player.position.z, roomMinZ, roomMaxZ));
            }

            SetEchoVisible(true);
            episodeController?.ReportStoryBeat(beatMessage);
            coach?.ShowBeat(beatMessage, beatMessageDuration);
        }

        public void Configure(
            Transform echoTransform,
            Transform playerTransform,
            OfficeEpisodeController controller,
            OfficeCoach routeCoach)
        {
            echo = echoTransform;
            player = playerTransform;
            episodeController = controller;
            coach = routeCoach;
        }

        public void ResetForRun()
        {
            _played = false;
            _visibleUntil = 0f;
            SetEchoVisible(false);
        }

        private void SetEchoVisible(bool value)
        {
            if (_echoRenderers == null)
            {
                return;
            }

            for (var i = 0; i < _echoRenderers.Length; i++)
            {
                if (_echoRenderers[i] != null)
                {
                    _echoRenderers[i].enabled = value;
                }
            }
        }
    }
}
