using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Целостность героя и быстрый перезапуск забега. Ранняя смерть — это fail-state:
    /// эпизод не завершается, а забег сразу начинается заново с той же стартовой точки.
    /// Финальный сюжетный удар помечает забег завершённым и никогда не запускает restart.
    /// </summary>
    public sealed class OfficeRunController : MonoBehaviour
    {
        [Header("Сцена")]
        [SerializeField] private Transform player;
        [SerializeField] private CharacterController playerCharacter;
        [SerializeField] private OfficePlayerController playerController;
        [SerializeField] private OfficeCarryController carryController;
        [SerializeField] private OfficeEpisodeController episodeController;

        [Header("Правила забега")]
        [SerializeField, Min(1)] private int maxIntegrity = 3;
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 1.2f;
        [SerializeField, Min(0f)] private float downBeatDuration = 1.1f;

        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private float _invulnerableUntil;
        private float _restartAtTime;
        private bool _isDown;
        private bool _storyCompleted;

        public int MaxIntegrity => maxIntegrity;

        public int Integrity { get; private set; }

        /// <summary>Номер текущей попытки; первая попытка — 1.</summary>
        public int Attempt { get; private set; } = 1;

        public bool IsRunActive => !_isDown && !_storyCompleted && Integrity > 0;

        public bool IsStoryCompleted => _storyCompleted;

        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        private void Awake()
        {
            Integrity = maxIntegrity;

            if (player != null)
            {
                _spawnPosition = player.position;
                _spawnRotation = player.rotation;
            }
        }

        private void Start()
        {
            episodeController?.ReportRunState(Integrity, maxIntegrity, Attempt);
        }

        private void Update()
        {
            if (!_isDown || Time.time < _restartAtTime)
            {
                return;
            }

            RestartRun();
        }

        public void Configure(
            Transform playerTransform,
            CharacterController character,
            OfficePlayerController movement,
            OfficeCarryController carry,
            OfficeEpisodeController controller)
        {
            player = playerTransform;
            playerCharacter = character;
            playerController = movement;
            carryController = carry;
            episodeController = controller;
        }

        /// <summary>
        /// Наносит герою один урон. Возвращает <c>true</c>, только если удар засчитан:
        /// противник использует это, чтобы не бить сквозь окно неуязвимости.
        /// </summary>
        public bool TryDamagePlayer(string sourceName)
        {
            if (!IsRunActive || IsInvulnerable)
            {
                return false;
            }

            Integrity = Mathf.Max(0, Integrity - 1);
            _invulnerableUntil = Time.time + invulnerabilityDuration;
            episodeController?.ReportRunState(Integrity, maxIntegrity, Attempt);

            if (Integrity > 0)
            {
                episodeController?.ReportPlayerHit(sourceName, Integrity, maxIntegrity);
                return true;
            }

            BeginDownBeat(sourceName);
            return true;
        }

        private void BeginDownBeat(string sourceName)
        {
            _isDown = true;
            _restartAtTime = Time.time + downBeatDuration;

            if (playerController != null)
            {
                playerController.enabled = false;
            }

            if (carryController != null)
            {
                carryController.enabled = false;
            }

            episodeController?.ReportRunFailed(sourceName, Attempt + 1);
        }

        /// <summary>
        /// Неизбежный удар кольца — успешный сюжетный финал, а не обычная смерть.
        /// Управление блокируется, но таймер restart не запускается.
        /// </summary>
        public void CompleteFromFinalStrike(string sourceName)
        {
            if (_storyCompleted)
            {
                return;
            }

            _storyCompleted = true;
            _isDown = false;
            Integrity = 0;

            playerController?.SetControlLocked(true);
            if (carryController != null)
            {
                carryController.ReleaseHeldItem();
                carryController.SetControlLocked(true);
            }

            episodeController?.ReportRunState(Integrity, maxIntegrity, Attempt);
            episodeController?.ReportStoryBeat(sourceName);
            episodeController?.ReportBossFinalStrike();
        }

        /// <summary>
        /// Возвращает забег в устойчивое начало без перезагрузки сцены: сцена ещё не
        /// добавлена в Build Settings, а мягкий restart укладывается в целевые 5 секунд.
        /// </summary>
        public void RestartRun()
        {
            if (_storyCompleted)
            {
                return;
            }

            _isDown = false;
            Integrity = maxIntegrity;
            _invulnerableUntil = 0f;
            Attempt++;

            if (carryController != null)
            {
                carryController.ReleaseHeldItem();
                carryController.enabled = true;
                carryController.SetControlLocked(false);
            }

            MovePlayerToSpawn();

            if (playerController != null)
            {
                playerController.enabled = true;
                playerController.SetControlLocked(false);
                playerController.ResetMotion();
            }

            OfficeRunReset.ResetAll();

            episodeController?.ResetForRun();
            episodeController?.ReportRunState(Integrity, maxIntegrity, Attempt);
            episodeController?.ReportRunRestarted(Attempt);
        }

        private void MovePlayerToSpawn()
        {
            if (player == null)
            {
                return;
            }

            // CharacterController перезаписывает transform, поэтому его выключаем.
            var hadCharacter = playerCharacter != null && playerCharacter.enabled;
            if (hadCharacter)
            {
                playerCharacter.enabled = false;
            }

            player.SetPositionAndRotation(_spawnPosition, _spawnRotation);

            if (hadCharacter)
            {
                playerCharacter.enabled = true;
            }
        }
    }
}
