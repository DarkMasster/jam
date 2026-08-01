using System.Collections;
using Jam.Core.Cutscenes;
using Jam.Core.Flow;
using Jam.Core.Localization;
using Jam.Core.Save;
using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Сюжетная рамка офисного эпизода: короткий Setup сна, пробуждение после
    /// неизбежного удара, устойчивый checkpoint и один <see cref="EpisodeResult"/>
    /// для общего flow. Эпизод никогда не грузит чужую сцену сам.
    /// </summary>
    public sealed class OfficeStoryDirector : MonoBehaviour, IGameModeSaveProvider
    {
        [Header("Сцена")]
        [SerializeField] private OfficeRunController runController;
        [SerializeField] private OfficeEpisodeController episodeController;
        [SerializeField] private OfficePlayerController playerController;
        [SerializeField] private OfficeCarryController carryController;

        [Header("Катсцены")]
        [SerializeField] private string setupCutsceneId = "office.prologue.setup";
        [SerializeField] private string awakeningCutsceneId = "office.prologue.awakening";

        [Header("Темп постановки")]
        [SerializeField, Min(0f)] private float finalStrikeHold = 2.2f;
        [SerializeField, Min(0.4f)] private float fallbackBeatDuration = 2.6f;

        private OfficeCharacterSaveData _saveData;
        private OfficeEpisodePhase _phase = OfficeEpisodePhase.Setup;
        private CutsceneDirector _director;
        private string _pendingCutsceneId;
        private Coroutine _fallbackRoutine;
        private bool _episodeHandedOff;

        public OfficeEpisodePhase Phase => _phase;

        /// <summary>Ручное сохранение доступно только внутри управляемого забега.</summary>
        public bool CanSave => isActiveAndEnabled && _phase == OfficeEpisodePhase.Run && !_episodeHandedOff;

        public string ModeName => Loc.Get(LocalizationTables.Office, "mode.name", "Офисный кошмар");

        private void Awake()
        {
            if (runController == null || episodeController == null)
            {
                Debug.LogError($"{nameof(OfficeStoryDirector)} on '{name}' is missing a scene reference.", this);
            }

            _saveData = LoadProgress();
            _phase = OfficeCheckpointAdapter.ResolveResumePhase(_saveData);

            // Повторный заход в уже пройденный сон начинается с чистого счёта.
            if (_phase == OfficeEpisodePhase.Setup)
            {
                _saveData.prologue = new OfficePrologueProgress();
            }
        }

        private void OnEnable()
        {
            if (runController != null)
            {
                runController.StoryCompleted += HandleStoryCompleted;
            }
        }

        private void OnDisable()
        {
            if (runController != null)
            {
                runController.StoryCompleted -= HandleStoryCompleted;
            }

            UnsubscribeFromDirector();
        }

        private void Start()
        {
            if (_phase == OfficeEpisodePhase.Setup)
            {
                BeginSetup();
                return;
            }

            BeginRun(false);
        }

        // ------------------------------------------------------------------ Setup

        private void BeginSetup()
        {
            _phase = OfficeEpisodePhase.Setup;
            SetControlLocked(true);
            episodeController?.ReportStoryBeat(
                Loc.Get(LocalizationTables.Office, "status.setup", "СОН НАЧИНАЕТСЯ"));

            if (!TryPlayCutscene(setupCutsceneId))
            {
                // Fallback остаётся коротким и не блокирует управление, поэтому
                // Setup нигде не превращается в длинную непрерываемую cutscene.
                SetControlLocked(false);
                _fallbackRoutine = StartCoroutine(PlayFallbackBeats(
                    new[] { "setup.fallback.01", "setup.fallback.02", "setup.fallback.03" },
                    new[]
                    {
                        "ОЧЕРЕДЬ НА ГРАНИЦЕ • ГЕРОЙ ЗАСЫПАЕТ В МАШИНЕ",
                        "СОН НАЧИНАЕТСЯ • ОФИС, КОТОРЫЙ ОН ТОЛЬКО ЧТО ПОТЕРЯЛ",
                        "СОБЕРИ ЛИЧНЫЕ ВЕЩИ И ДОБЕРИСЬ ДО EXIT"
                    },
                    () => BeginRun(true)));
            }
        }

        private void BeginRun(bool saveCheckpoint)
        {
            _phase = OfficeEpisodePhase.Run;
            _saveData.prologue.setupSeen = true;
            SetControlLocked(false);

            if (saveCheckpoint)
            {
                SaveCheckpoint(OfficeCheckpointAdapter.RunCheckpoint);
            }
        }

        // -------------------------------------------------------------- Awakening

        private void HandleStoryCompleted()
        {
            if (_phase == OfficeEpisodePhase.Awakening || _episodeHandedOff)
            {
                return;
            }

            _phase = OfficeEpisodePhase.Awakening;
            CaptureRunState();
            StartCoroutine(PlayAwakening());
        }

        private IEnumerator PlayAwakening()
        {
            // Финальный удар и его overlay должны дочитаться до пробуждения.
            yield return new WaitForSeconds(finalStrikeHold);

            episodeController?.ReportStoryBeat(
                Loc.Get(LocalizationTables.Office, "status.awakening", "ПРОБУЖДЕНИЕ • ОЧЕРЕДЬ ПОШЛА"));

            if (TryPlayCutscene(awakeningCutsceneId))
            {
                yield break;
            }

            _fallbackRoutine = StartCoroutine(PlayFallbackBeats(
                new[] { "awakening.fallback" },
                new[] { "ПРОБУЖДЕНИЕ В МАШИНЕ • ОЧЕРЕДЬ ПОШЛА" },
                CompleteEpisode));
        }

        private void CompleteEpisode()
        {
            if (_episodeHandedOff)
            {
                return;
            }

            _episodeHandedOff = true;
            _phase = OfficeEpisodePhase.Arrival;
            CaptureRunState();

            var progress = _saveData.prologue;
            progress.completed = true;
            progress.phase = OfficeEpisodePhase.Arrival;

            var result = new EpisodeResult
            {
                characterId = CharacterId.Office,
                sceneName = gameObject.scene.name,
                checkpointId = OfficeCheckpointAdapter.ArrivalCheckpoint,
                payloadJson = OfficeCheckpointAdapter.Serialize(
                    _saveData,
                    OfficeCheckpointAdapter.ArrivalCheckpoint),
                episodeCompleted = true,
                arrivalTable = LocalizationTables.Office,
                arrivalKey = "arrival.body",
                arrivalFallback =
                    "Он просыпается в очереди на границе. Офис остался во сне, ноутбук и кружка — на коленях."
            };

            var yes = Loc.Get(LocalizationTables.Office, "result.yes", "ЕСТЬ");
            var no = Loc.Get(LocalizationTables.Office, "result.no", "НЕТ");

            result
                .AddLine(LocalizationTables.Office, "result.retries", "ПЕРЕЗАПУСКОВ ЗАБЕГА", progress.retries.ToString())
                .AddLine(LocalizationTables.Office, "result.laptop", "НОУТБУК", progress.hasLaptop ? yes : no)
                .AddLine(LocalizationTables.Office, "result.mug", "КРУЖКА", progress.hasMug ? yes : no)
                .AddLine(LocalizationTables.Office, "result.destroyed", "РАЗРУШЕНО ТЕХНИКИ", progress.destroyedEquipment.ToString())
                .AddLine(LocalizationTables.Office, "result.wrecked", "СПИСАНО КРЕСЕЛ", progress.wreckedChasers.ToString());

            GameFlowService.CompleteEpisode(result);
        }

        // ---------------------------------------------------------------- Помощь

        private bool TryPlayCutscene(string cutsceneId)
        {
            if (string.IsNullOrWhiteSpace(cutsceneId))
            {
                return false;
            }

            _director = CutsceneDirector.Instance;
            if (_director == null)
            {
                return false;
            }

            var context = new CutsceneContext
            {
                characterId = CharacterId.Office.ToString(),
                startCheckpointId = OfficeCheckpointAdapter.CheckpointFromPhase(_phase),
                completionCheckpointId = _phase == OfficeEpisodePhase.Setup
                    ? OfficeCheckpointAdapter.RunCheckpoint
                    : OfficeCheckpointAdapter.ArrivalCheckpoint
            };

            _pendingCutsceneId = cutsceneId;
            _director.Finished += HandleCutsceneFinished;

            if (_director.TryPlay(cutsceneId, context, out var error))
            {
                return true;
            }

            Debug.LogWarning($"Office cutscene fallback: {error}");
            UnsubscribeFromDirector();
            return false;
        }

        private void HandleCutsceneFinished(CutsceneResult result)
        {
            if (result.CutsceneId != _pendingCutsceneId)
            {
                return;
            }

            UnsubscribeFromDirector();

            if (_phase == OfficeEpisodePhase.Setup)
            {
                BeginRun(true);
                return;
            }

            if (_phase == OfficeEpisodePhase.Awakening)
            {
                CompleteEpisode();
            }
        }

        private void UnsubscribeFromDirector()
        {
            if (_director != null)
            {
                _director.Finished -= HandleCutsceneFinished;
            }

            _director = null;
            _pendingCutsceneId = null;
        }

        private IEnumerator PlayFallbackBeats(string[] keys, string[] fallbacks, System.Action onFinished)
        {
            for (var index = 0; index < keys.Length; index++)
            {
                episodeController?.ReportStoryBeat(
                    Loc.Get(LocalizationTables.Office, keys[index], fallbacks[index]));
                yield return new WaitForSeconds(fallbackBeatDuration);
            }

            _fallbackRoutine = null;
            onFinished?.Invoke();
        }

        private void SetControlLocked(bool value)
        {
            playerController?.SetControlLocked(value);
            carryController?.SetControlLocked(value);
        }

        /// <summary>Снимает текущее состояние забега в episode-owned payload.</summary>
        private void CaptureRunState()
        {
            var progress = _saveData.prologue;
            progress.setupSeen = true;

            if (runController != null)
            {
                progress.retries = Mathf.Max(0, runController.Attempt - 1);
            }

            if (episodeController != null)
            {
                progress.hasLaptop = episodeController.HasLaptop;
                progress.hasMug = episodeController.HasMug;
                progress.destroyedEquipment = episodeController.BreakablesDestroyed;
                progress.wreckedChasers = episodeController.ChasersWrecked;
            }
        }

        private void SaveCheckpoint(string checkpointId)
        {
            CaptureRunState();
            _saveData.prologue.phase = _phase;

            GameSaveService.SaveCharacterCheckpoint(
                CharacterId.Office,
                gameObject.scene.name,
                checkpointId,
                OfficeCheckpointAdapter.Serialize(_saveData, checkpointId));
        }

        private static OfficeCharacterSaveData LoadProgress()
        {
            return GameSaveService.TryGetCharacterCheckpoint(CharacterId.Office, out var checkpoint)
                   && OfficeCheckpointAdapter.TryLoad(checkpoint, out var stored)
                ? stored
                : OfficeCheckpointAdapter.CreateNew();
        }

        public void Configure(
            OfficeRunController run,
            OfficeEpisodeController episode,
            OfficePlayerController movement,
            OfficeCarryController carry,
            string setupId,
            string awakeningId)
        {
            runController = run;
            episodeController = episode;
            playerController = movement;
            carryController = carry;
            setupCutsceneId = setupId;
            awakeningCutsceneId = awakeningId;
        }

        public bool TrySave(out string message)
        {
            if (!CanSave)
            {
                message = Loc.Get(LocalizationTables.Office, "save.blocked", "Сейчас сохранение недоступно.");
                return false;
            }

            SaveCheckpoint(OfficeCheckpointAdapter.RunCheckpoint);
            episodeController?.ReportStoryBeat(Loc.Get(
                LocalizationTables.Office,
                "status.saved",
                "ПРОГРЕСС СОХРАНЁН • ПОПЫТКА {0}",
                runController != null ? runController.Attempt : 1));

            message = Loc.Get(LocalizationTables.Office, "save.ok", "Офисный забег сохранён.");
            return true;
        }
    }
}
