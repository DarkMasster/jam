using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Ожившее офисное кресло — единственный тип противника джемового среза.
    /// Одно поведение: красный телеграф на полу, затем быстрый рывок по прямой.
    /// Промах по герою заканчивается ударом о мебель и долгим окном для броска.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class OfficeChaser : MonoBehaviour, IOfficeImpactTarget, IOfficeRunResettable
    {
        private enum ChaserState
        {
            Dormant,
            Telegraph,
            Dash,
            Recover,
            Wrecked
        }

        [SerializeField] private string displayName = "ОФИСНОЕ КРЕСЛО";

        [Header("Сцена")]
        [SerializeField] private Transform player;
        [SerializeField] private OfficeRunController runController;
        [SerializeField] private OfficeMomentum momentum;
        [SerializeField] private OfficeEpisodeController episodeController;

        [Header("Состояния")]
        [SerializeField] private GameObject intactState;
        [SerializeField] private GameObject wreckedState;
        [SerializeField] private Transform telegraphVisual;
        [SerializeField] private Light warningLight;

        [Header("Поведение")]
        [SerializeField, Min(1f)] private float activationRadius = 13f;
        [SerializeField, Min(0.1f)] private float telegraphDuration = 0.8f;
        [SerializeField, Min(0.1f)] private float dashSpeed = 14f;
        [SerializeField, Min(0.1f)] private float dashDuration = 0.55f;
        [SerializeField, Min(0.1f)] private float recoverDuration = 1.1f;
        [SerializeField, Min(0.1f)] private float wallRecoverDuration = 2.1f;
        [SerializeField, Min(1f)] private float aimRotationSpeed = 260f;
        [SerializeField, Min(0.1f)] private float contactRadius = 1.15f;
        [SerializeField, Min(0.05f)] private float bodyRadius = 0.55f;
        [SerializeField, Min(0.1f)] private float sightHeight = 1.2f;
        [SerializeField, Min(0f)] private float breakImpactSpeed = 6f;
        [SerializeField, Min(0f)] private float warningIntensity = 4.5f;

        private ChaserState _state = ChaserState.Dormant;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private Vector3 _dashDirection;
        private float _stateTimeLeft;
        private bool _registeredWithEpisode;

        public string DisplayName => displayName;

        public bool IsWrecked => _state == ChaserState.Wrecked;

        /// <summary>Телеграф виден игроку; используется проверками среза.</summary>
        public bool IsTelegraphing => _state == ChaserState.Telegraph;

        public bool IsDashing => _state == ChaserState.Dash;

        private float DashLength => dashSpeed * dashDuration;

        private void Awake()
        {
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;

            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
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
            if (!_registeredWithEpisode)
            {
                episodeController?.RegisterChaser();
                _registeredWithEpisode = true;
            }

            ApplyState();
        }

        private void Update()
        {
            if (_state == ChaserState.Wrecked)
            {
                return;
            }

            var delta = Time.deltaTime;
            switch (_state)
            {
                case ChaserState.Dormant:
                    TickDormant();
                    break;
                case ChaserState.Telegraph:
                    TickTelegraph(delta);
                    break;
                case ChaserState.Dash:
                    TickDash(delta);
                    break;
                case ChaserState.Recover:
                    TickRecover(delta);
                    break;
            }
        }

        public void Configure(
            Transform playerTransform,
            OfficeRunController run,
            OfficeMomentum momentumScale,
            OfficeEpisodeController controller,
            GameObject intact,
            GameObject wrecked,
            Transform telegraph,
            Light warning)
        {
            player = playerTransform;
            runController = run;
            momentum = momentumScale;
            episodeController = controller;
            intactState = intact;
            wreckedState = wrecked;
            telegraphVisual = telegraph;
            warningLight = warning;
        }

        public void SetSceneReferences(
            Transform playerTransform,
            OfficeRunController run,
            OfficeMomentum momentumScale,
            OfficeEpisodeController controller)
        {
            player = playerTransform;
            runController = run;
            momentum = momentumScale;
            episodeController = controller;
        }

        public bool TryTakeImpact(float impactSpeed)
        {
            if (_state == ChaserState.Wrecked || impactSpeed < breakImpactSpeed)
            {
                return false;
            }

            _state = ChaserState.Wrecked;
            ApplyState();
            momentum?.AddEnemyDefeated();
            episodeController?.RegisterChaserWrecked(displayName);
            return true;
        }

        public void ResetForRun()
        {
            _state = ChaserState.Dormant;
            _stateTimeLeft = 0f;
            _dashDirection = Vector3.zero;
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            ApplyState();
        }

        private void TickDormant()
        {
            if (!CanSeePlayer(out var toPlayer))
            {
                return;
            }

            _state = ChaserState.Telegraph;
            _stateTimeLeft = telegraphDuration;
            FaceInstantly(toPlayer);
            ApplyState();
        }

        private void TickTelegraph(float delta)
        {
            if (CanSeePlayer(out var toPlayer))
            {
                // Кресло доводит прицел до конца телеграфа, но не преследует героя.
                var target = Quaternion.LookRotation(toPlayer, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    target,
                    aimRotationSpeed * delta);
            }

            _stateTimeLeft -= delta;
            UpdateTelegraphVisual();

            if (_stateTimeLeft > 0f)
            {
                return;
            }

            _dashDirection = Flatten(transform.forward).normalized;
            _state = ChaserState.Dash;
            _stateTimeLeft = dashDuration;
            ApplyState();
        }

        private void TickDash(float delta)
        {
            var step = dashSpeed * delta;

            if (HitsObstacle(step, out var allowedStep))
            {
                transform.position += _dashDirection * allowedStep;
                EnterRecover(wallRecoverDuration);
                episodeController?.ReportChaserCrash(displayName);
                return;
            }

            transform.position += _dashDirection * step;

            if (TouchesPlayer())
            {
                if (runController != null && runController.TryDamagePlayer(displayName))
                {
                    EnterRecover(recoverDuration);
                    return;
                }
            }

            _stateTimeLeft -= delta;
            if (_stateTimeLeft <= 0f)
            {
                EnterRecover(recoverDuration);
            }
        }

        private void TickRecover(float delta)
        {
            _stateTimeLeft -= delta;
            if (_stateTimeLeft > 0f)
            {
                return;
            }

            _state = ChaserState.Dormant;
            ApplyState();
        }

        private void EnterRecover(float duration)
        {
            _state = ChaserState.Recover;
            _stateTimeLeft = duration;
            ApplyState();
        }

        private bool CanSeePlayer(out Vector3 toPlayer)
        {
            toPlayer = Vector3.zero;
            if (player == null || (runController != null && !runController.IsRunActive))
            {
                return false;
            }

            var offset = Flatten(player.position - transform.position);
            if (offset.sqrMagnitude > activationRadius * activationRadius || offset.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            // Стена не должна будить кресло: иначе оно телеграфирует и бьётся о неё,
            // пока герой ещё в другой зоне маршрута.
            if (IsLineOfSightBlocked(offset))
            {
                return false;
            }

            toPlayer = offset;
            return true;
        }

        private bool IsLineOfSightBlocked(Vector3 offset)
        {
            // Луч идёт выше столов и принтеров: кресло «видит» через низкую мебель,
            // но не через стены, стекло, стойки и стойку рецепции.
            var origin = transform.position + (Vector3.up * sightHeight);
            var distance = offset.magnitude;
            var hits = Physics.RaycastAll(
                origin,
                offset / distance,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < hits.Length; i++)
            {
                var collider = hits[i].collider;
                if (collider == null || collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (collider.GetComponentInParent<OfficePlayerController>() != null
                    || collider.GetComponentInParent<OfficeCarryable>() != null)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private bool TouchesPlayer()
        {
            if (player == null)
            {
                return false;
            }

            return Flatten(player.position - transform.position).sqrMagnitude <= contactRadius * contactRadius;
        }

        private bool HitsObstacle(float step, out float allowedStep)
        {
            allowedStep = step;
            var origin = transform.position + (Vector3.up * 0.6f);
            var hits = Physics.SphereCastAll(
                origin,
                bodyRadius,
                _dashDirection,
                step,
                ~0,
                QueryTriggerInteraction.Ignore);

            var closest = float.MaxValue;
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                // Герой не останавливает рывок: попадание по нему обрабатывается отдельно.
                if (hit.collider.GetComponentInParent<OfficePlayerController>() != null)
                {
                    continue;
                }

                // Брошенные и лежащие предметы кресло просто расталкивает.
                if (hit.collider.GetComponentInParent<OfficeCarryable>() != null)
                {
                    continue;
                }

                if (hit.distance < closest)
                {
                    closest = hit.distance;
                }
            }

            if (closest >= float.MaxValue)
            {
                return false;
            }

            allowedStep = Mathf.Max(0f, closest - 0.02f);
            return true;
        }

        private void FaceInstantly(Vector3 direction)
        {
            transform.rotation = Quaternion.LookRotation(Flatten(direction).normalized, Vector3.up);
        }

        private void ApplyState()
        {
            var wrecked = _state == ChaserState.Wrecked;

            if (intactState != null)
            {
                intactState.SetActive(!wrecked);
            }

            if (wreckedState != null)
            {
                wreckedState.SetActive(wrecked);
            }

            if (telegraphVisual != null)
            {
                telegraphVisual.gameObject.SetActive(_state == ChaserState.Telegraph);
                if (_state == ChaserState.Telegraph)
                {
                    SetTelegraphFill(0f);
                }
            }

            if (warningLight != null)
            {
                warningLight.intensity = _state == ChaserState.Telegraph ? warningIntensity : 0f;
            }
        }

        private void UpdateTelegraphVisual()
        {
            if (telegraphVisual == null)
            {
                return;
            }

            var fill = Mathf.Clamp01(1f - (_stateTimeLeft / telegraphDuration));
            SetTelegraphFill(fill);

            if (warningLight != null)
            {
                warningLight.intensity = warningIntensity * (0.55f + (0.45f * Mathf.Sin(Time.time * 26f)));
            }
        }

        /// <summary>Полоса на полу растёт вперёд и показывает длину будущего рывка.</summary>
        private void SetTelegraphFill(float fill)
        {
            var length = Mathf.Max(0.01f, DashLength * fill);
            var scale = telegraphVisual.localScale;
            telegraphVisual.localScale = new Vector3(scale.x, scale.y, length);
            telegraphVisual.localPosition = new Vector3(0f, 0.03f, (length * 0.5f) + bodyRadius);
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
