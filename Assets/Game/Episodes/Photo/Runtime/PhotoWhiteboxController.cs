using System;
using System.Linq;
using Jam.Core.Audio;
using Jam.Core.Cutscenes;
using Jam.Core.Flow;
using Jam.Core.Localization;
using Jam.Core.Save;
using NodeCanvas.Framework;
using NodeCanvas.DialogueTrees;
using NodeCanvas.StateMachines;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jam.Episodes.Photo
{
    public enum PhotoWhiteboxPhase
    {
        IntroDialogue,
        Explore,
        Camera,
        Publish,
        ReflectionDialogue,
        Arrival
    }

    public enum PhotoChoice
    {
        None,
        Summons,
        Butterfly
    }

    [RequireComponent(typeof(FSMOwner), typeof(Blackboard), typeof(DialogueTreeController))]
    public sealed class PhotoWhiteboxController : MonoBehaviour, IGameModeSaveProvider
    {
        private enum PresentationLayout
        {
            Dialogue,
            Viewfinder,
            ChoiceMatrix,
            Summary
        }

        private const string IntroCutsceneId = "photo.prologue.intro";
        private const string OutroCutsceneId = "photo.prologue.to_be_continued";
        private const string ExploreCheckpoint = "photo.explore";
        private const string CameraCheckpoint = "photo.camera";
        private const string PublishedCheckpoint = "photo.published";
        private const string ArrivalCheckpoint = "photo.arrival";
        private const int RequiredInspectionMask = PhotoCheckpointAdapter.RequiredInspectionMask;

        [Header("Production Dialogue Trees")]
        [SerializeField] private DialogueTree motherDialogue;
        [SerializeField] private DialogueTree borderDialogue;

        [Header("Production Audio Cues")]
        [SerializeField] private AudioCue roomAmbienceCue;
        [SerializeField] private AudioCue airportAmbienceCue;
        [SerializeField] private AudioCue shutterCue;
        [SerializeField] private AudioCue doorCue;
        [SerializeField] private AudioCue passportStampCue;

        private static readonly Color BackgroundColor = new(0.075f, 0.085f, 0.11f, 1f);
        private static readonly Color PanelColor = new(0.13f, 0.14f, 0.18f, 0.98f);
        private static readonly Color StageColor = new(0.19f, 0.20f, 0.24f, 1f);
        private static readonly Color AccentColor = new(0.78f, 0.44f, 0.68f, 1f);
        private static readonly Color ButtonColor = new(0.24f, 0.25f, 0.31f, 1f);
        private static readonly Color SelectedColor = new(0.42f, 0.24f, 0.39f, 1f);
        private static readonly Color TextColor = new(0.94f, 0.92f, 0.89f, 1f);
        private static readonly Color MutedTextColor = new(0.67f, 0.69f, 0.74f, 1f);

        private readonly string[] _introLines =
        {
            "24 февраля 2022 года. Утро начинается с новостей, которым не находится места в привычной жизни.",
            "Сообщение от редактора: агентство закрывает российский офис. Проекты остановлены. Команда распущена.",
            "Forbidgram недоступен. Клиенты молчат. В телефоне остаются архив, незакрытые счета и билет в один конец.",
            "Перед отъездом нужен ещё один кадр — достаточно честный, чтобы не предать себя, и достаточно заметный, чтобы оплатить дорогу."
        };

        private FSMOwner _fsmOwner;
        private Blackboard _blackboard;
        private DialogueTreeController _dialogueController;
        private TMP_Text _phaseText;
        private TMP_Text _speakerText;
        private TMP_Text _contentText;
        private TMP_Text _statusText;
        private RectTransform _actionsRoot;
        private RectTransform _actionsPanelRect;
        private RectTransform _contentRect;
        private LayoutElement _stageLayout;
        private LayoutElement _actionsLayout;
        private GridLayoutGroup _actionsGrid;
        private GameObject _viewfinderOverlay;
        private GameObject _dialoguePortrait;
        private RawImage _dialoguePortraitImage;
        private RectTransform _honestyFill;
        private RectTransform _recognitionFill;
        private TMP_Text _honestyLabel;
        private TMP_Text _recognitionLabel;
        private PhotoRoomDioramaPresenter _roomDiorama;
        private RawImage _roomDioramaImage;
        private float _actionButtonHeight = 64f;
        private PhotoWhiteboxPhase _phase;
        private PhotoChoice _choice;
        private int _introIndex;
        private int _inspectedMask;
        private int _truth;
        private int _reach;
        private bool _introCutsceneRunning;
        private bool _outroCutsceneRunning;
        private bool _episodeHandedOff;
        private bool _dialogueRunning;
        private bool _isShuttingDown;
        private PhotoPrologueStep _dialogueStep;
        private int _dialogueChoiceIndex = -1;
        private AudioPlaybackHandle _ambienceHandle;
        private PhotoCharacterSaveData _saveData = PhotoCheckpointAdapter.CreateNew();

        public bool CanSave => isActiveAndEnabled;
        public string ModeName => Loc.Get(LocalizationTables.Photo, "mode.name", "История фотографки");

        private void Awake()
        {
            _fsmOwner = GetComponent<FSMOwner>();
            _blackboard = GetComponent<Blackboard>();
            _dialogueController = GetComponent<DialogueTreeController>();
            _roomDiorama = FindFirstObjectByType<PhotoRoomDioramaPresenter>();
            EnsureEventSystem();
            BuildInterface();
        }

        private void Start()
        {
            RestoreOrBegin();
        }

        private void OnEnable()
        {
            _isShuttingDown = false;
            Loc.LocaleChanged += HandleLocaleChanged;
            DialogueTree.OnSubtitlesRequest += HandleDialogueSubtitle;
            DialogueTree.OnMultipleChoiceRequest += HandleDialogueChoices;
        }

        private void OnDisable()
        {
            _isShuttingDown = true;
            Loc.LocaleChanged -= HandleLocaleChanged;
            DialogueTree.OnSubtitlesRequest -= HandleDialogueSubtitle;
            DialogueTree.OnMultipleChoiceRequest -= HandleDialogueChoices;
            if (_dialogueRunning) _dialogueController?.StopDialogue();
            StopAmbience();
            UnsubscribeFromCutsceneDirector();
            UnsubscribeFromOutroCutscene();
            _introCutsceneRunning = false;
            _outroCutsceneRunning = false;
        }

        private void HandleLocaleChanged()
        {
            if (_introCutsceneRunning || _dialogueRunning)
            {
                return;
            }

            if (_phase != PhotoWhiteboxPhase.IntroDialogue && _saveData?.schemaVersion >= 3)
            {
                RenderProductionStep();
                return;
            }

            switch (_phase)
            {
                case PhotoWhiteboxPhase.IntroDialogue: RenderIntro(); break;
                case PhotoWhiteboxPhase.Explore: RenderExplore(); break;
                case PhotoWhiteboxPhase.Camera: RenderCamera(); break;
                case PhotoWhiteboxPhase.Publish: RenderPublish(); break;
                case PhotoWhiteboxPhase.ReflectionDialogue: RenderReflection(); break;
                case PhotoWhiteboxPhase.Arrival: RenderArrival(); break;
            }
        }

        private void RestoreOrBegin()
        {
            if (GameSaveService.TryGetCharacterCheckpoint(CharacterId.Photo, out var checkpoint)
                && checkpoint.sceneName == gameObject.scene.name
                && !string.IsNullOrWhiteSpace(checkpoint.payloadJson))
            {
                try
                {
                    if (PhotoCheckpointAdapter.TryLoad(checkpoint, out var restored))
                    {
                        _saveData = restored;
                        _introIndex = restored.prologue.introIndex;
                        _inspectedMask = restored.prologue.inspectedMask;
                        _choice = restored.prologue.photoChoice;
                        _truth = restored.prologue.truth;
                        _reach = restored.prologue.reach;
                        if (restored.schemaVersion >= 3
                            && checkpoint.checkpointId != "photo.intro")
                        {
                            EnterProductionStep(restored.prologue.step, false);
                            SetStatus(Loc.Get(LocalizationTables.Photo, "status.continue", "Продолжение: {0}", checkpoint.checkpointId));
                            return;
                        }

                        EnterPhase(PhotoCheckpointAdapter.ResolveResumePhase(restored, checkpoint.checkpointId), false);
                        SetStatus(Loc.Get(LocalizationTables.Photo, "status.continue", "Продолжение: {0}", checkpoint.checkpointId));
                        return;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Photo white-box checkpoint was ignored: {exception.Message}");
                }
            }

            EnterPhase(PhotoWhiteboxPhase.IntroDialogue, false);
        }

        private void EnterPhase(PhotoWhiteboxPhase phase, bool save)
        {
            SetRoomDioramaVisible(false);
            _phase = phase;
            SyncNodeCanvas();

            switch (phase)
            {
                case PhotoWhiteboxPhase.IntroDialogue:
                    StartIntroCutsceneOrFallback();
                    break;
                case PhotoWhiteboxPhase.Explore:
                    if (save) SaveCheckpoint(ExploreCheckpoint);
                    RenderExplore();
                    break;
                case PhotoWhiteboxPhase.Camera:
                    if (save) SaveCheckpoint(CameraCheckpoint);
                    RenderCamera();
                    break;
                case PhotoWhiteboxPhase.Publish:
                    if (save) SaveCheckpoint(PublishedCheckpoint);
                    RenderPublish();
                    break;
                case PhotoWhiteboxPhase.ReflectionDialogue:
                    RenderReflection();
                    break;
                case PhotoWhiteboxPhase.Arrival:
                    _saveData.prologue.completed = true;
                    if (save) SaveCheckpoint(ArrivalCheckpoint);
                    RenderArrival();
                    break;
            }
        }

        private void SyncNodeCanvas()
        {
            var stateName = _phase.ToString();
            _blackboard.SetVariableValue("phase", stateName);
            _blackboard.SetVariableValue("choiceId", _choice.ToString());
            _blackboard.SetVariableValue("truth", _truth);
            _blackboard.SetVariableValue("reach", _reach);
            _blackboard.SetVariableValue("inspectedCount", CountInspected());
            _blackboard.SetVariableValue("canUseCamera", _inspectedMask == RequiredInspectionMask);

            if (_fsmOwner.isRunning && _fsmOwner.TriggerState(stateName) == null)
            {
                Debug.LogWarning($"Photo FSM does not contain state '{stateName}'.");
            }
        }

        private void StartIntroCutsceneOrFallback()
        {
            if (_introCutsceneRunning)
            {
                return;
            }

            var director = CutsceneDirector.Instance;
            if (director == null)
            {
                RenderIntro();
                return;
            }

            _introCutsceneRunning = true;
            _blackboard.SetVariableValue("cutsceneId", IntroCutsceneId);
            _blackboard.SetVariableValue("cutsceneResult", "Playing");
            director.Finished += HandleIntroCutsceneFinished;

            var context = new CutsceneContext
            {
                characterId = CharacterId.Photo.ToString(),
                startCheckpointId = "photo.intro",
                completionCheckpointId = ExploreCheckpoint
            };

            if (director.TryPlay(IntroCutsceneId, context, out var error))
            {
                return;
            }

            UnsubscribeFromCutsceneDirector();
            _introCutsceneRunning = false;
            _blackboard.SetVariableValue("cutsceneResult", "Unavailable");
            Debug.LogWarning($"Photo intro cutscene fallback: {error}");
            RenderIntro();
        }

        private void HandleIntroCutsceneFinished(CutsceneResult result)
        {
            if (result.CutsceneId != IntroCutsceneId)
            {
                return;
            }

            UnsubscribeFromCutsceneDirector();
            _introCutsceneRunning = false;
            _blackboard.SetVariableValue("cutsceneResult", result.Reason.ToString());

            if (result.Succeeded)
            {
                _introIndex = _introLines.Length - 1;
                EnterProductionStep(PhotoPrologueStep.RoomSecret, true);
                return;
            }

            if (result.Reason == CutsceneEndReason.Failed)
            {
                Debug.LogWarning("Photo intro cutscene failed; using the white-box dialogue fallback.");
                RenderIntro();
            }
        }

        private void UnsubscribeFromCutsceneDirector()
        {
            if (CutsceneDirector.Instance != null)
            {
                CutsceneDirector.Instance.Finished -= HandleIntroCutsceneFinished;
            }
        }

        private void RenderIntro()
        {
            SetPhase(Loc.Get(LocalizationTables.Photo, "phase.intro", "ПРОЛОГ • САНКТ-ПЕТЕРБУРГ"));
            var entry = $"prologue.intro.{_introIndex + 1:000}";
            SetSpeaker(Loc.Get(LocalizationTables.Photo, entry + ".speaker", _introIndex == 1 ? "РЕДАКТОР" : _introIndex == 2 ? "ТЕЛЕФОН" : "ОНА"));
            SetContent(Loc.Get(LocalizationTables.Photo, entry + ".text", _introLines[_introIndex]));
            SetStatus(Loc.Get(LocalizationTables.Photo, "status.exposition", "Экспозиция {0} / {1}", _introIndex + 1, _introLines.Length));
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo,
                _introIndex + 1 < _introLines.Length ? "action.next" : "action.enter_courtyard",
                _introIndex + 1 < _introLines.Length ? "ДАЛЕЕ" : "ВЫЙТИ К ПОДЪЕЗДУ"), AdvanceIntro);
        }

        private void AdvanceIntro()
        {
            if (_introIndex + 1 < _introLines.Length)
            {
                _introIndex++;
                RenderIntro();
                return;
            }

            EnterProductionStep(PhotoPrologueStep.RoomSecret, true);
        }

        private void EnterProductionStep(PhotoPrologueStep step, bool save)
        {
            _saveData.prologue.step = step;
            _phase = PhaseForProductionStep(step);
            SyncNodeCanvas();

            if (save)
            {
                SaveCheckpoint(CheckpointForProductionStep(step));
            }

            UpdateAmbience(step);
            SetRoomDioramaVisible(_roomDiorama != null && _roomDiorama.Present(step));
            RenderProductionStep();
        }

        private void RenderProductionStep()
        {
            ApplyPresentationLayout(LayoutForStep(_saveData.prologue.step));
            UpdateScaleRibbon();

            switch (_saveData.prologue.step)
            {
                case PhotoPrologueStep.RoomSecret: RenderRoomSecret(); break;
                case PhotoPrologueStep.RoomPhoto: RenderRoomPhoto(); break;
                case PhotoPrologueStep.MotherDialogue: StartProductionDialogue(motherDialogue, PhotoPrologueStep.MotherDialogue, RenderMotherDialogue); break;
                case PhotoPrologueStep.MailboxHunt: RenderMailboxHunt(); break;
                case PhotoPrologueStep.MailboxPublication: RenderMailboxPublication(); break;
                case PhotoPrologueStep.MailboxReaction: RenderMailboxReaction(); break;
                case PhotoPrologueStep.AirportPhoto: RenderAirportPhoto(); break;
                case PhotoPrologueStep.BorderControl: StartProductionDialogue(borderDialogue, PhotoPrologueStep.BorderControl, RenderBorderControl); break;
                case PhotoPrologueStep.Summary: RenderProductionSummary(); break;
                case PhotoPrologueStep.Complete: RenderProductionSummary(); break;
                default:
                    _saveData.prologue.step = PhotoPrologueStep.RoomSecret;
                    RenderRoomSecret();
                    break;
            }
        }

        private void RenderRoomSecret()
        {
            SetPhase(Loc.Get(LocalizationTables.Photo, "production.room.phase", "СЦЕНА 1 • НЕОНОВАЯ КОМНАТА"));
            SetSpeaker(Loc.Get(LocalizationTables.Photo, "production.heroine", "ОНА"));
            SetContent(Loc.Get(LocalizationTables.Photo, "production.room.secret.prompt", "Они же не узнают…"));
            SetStatus(Loc.Get(LocalizationTables.Photo, "production.scales.hidden", "Выбор изменит внутренний вектор героини."));
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.room.secret.deny", "Они не узнают."), () => ChooseSecret(PhotoSecretChoice.TheyWillNotKnow));
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.room.secret.know", "Они уже знают.  •  +20 Признание"), () => ChooseSecret(PhotoSecretChoice.TheyAlreadyKnow), true, AccentColor);
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.room.secret.tell", "Пусть узнают.  •  +20 Честность"), () => ChooseSecret(PhotoSecretChoice.LetThemKnow), true, new Color(0.32f, 0.82f, 0.68f, 1f));
        }

        private void ChooseSecret(PhotoSecretChoice choice)
        {
            if (PhotoPrologueRules.ApplySecretChoice(_saveData.prologue, choice))
            {
                EnterProductionStep(PhotoPrologueStep.RoomPhoto, true);
            }
        }

        private void RenderRoomPhoto()
        {
            SetPhase(Loc.Get(LocalizationTables.Photo, "production.room.camera.phase", "КОМНАТА • ПЕРВЫЙ СНИМОК"));
            SetSpeaker(Loc.Get(LocalizationTables.Photo, "speaker.viewfinder", "ВИДОИСКАТЕЛЬ"));
            SetContent(Loc.Get(LocalizationTables.Photo, "production.room.camera.prompt", "В центре кадра — винтажная лампа. Бардак и пыль можно оставить или спрятать за красивым фильтром."));
            SetScaleStatus();
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.room.camera.honest", "Честный кадр: бардак и пыль  •  +20 Честность"), () => ChooseRoomShot(PhotoRoomShotChoice.Honest), true, new Color(0.32f, 0.82f, 0.68f, 1f));
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.room.camera.wings", "Глитч-фильтр с крыльями  •  +20 Признание"), () => ChooseRoomShot(PhotoRoomShotChoice.Wings), true, AccentColor);
        }

        private void ChooseRoomShot(PhotoRoomShotChoice choice)
        {
            if (PhotoPrologueRules.ApplyRoomShot(_saveData.prologue, choice))
            {
                PlayCue(shutterCue);
                EnterProductionStep(PhotoPrologueStep.MotherDialogue, true);
            }
        }

        private void RenderMotherDialogue()
        {
            SetPhase(Loc.Get(LocalizationTables.Photo, "production.mother.phase", "КОМНАТА • МАТЬ В ДВЕРНОМ ПРОЁМЕ"));
            SetSpeaker(Loc.Get(LocalizationTables.Photo, "production.mother.speaker", "МАТЬ"));
            SetContent(Loc.Get(LocalizationTables.Photo, "production.mother.prompt", "Ты всё ещё возишься с этой ерундой? Почему ты не на работе?!"));
            SetStatus(Loc.Get(LocalizationTables.Photo, "production.mother.status", "Честный разговор требует накопленной внутренней готовности."));
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.mother.honest", "Я увольняюсь и уезжаю. Мама… я устала.  •  −20 Честность"), () => ChooseMotherReply(PhotoMotherReply.Honest), _saveData.prologue.honesty >= 20, new Color(0.32f, 0.82f, 0.68f, 1f));
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.mother.lie", "Я работаю над крупным международным проектом.  •  +20 Признание"), () => ChooseMotherReply(PhotoMotherReply.ProtectiveLie), true, AccentColor);
        }

        private void ChooseMotherReply(PhotoMotherReply choice)
        {
            if (PhotoPrologueRules.ApplyMotherReply(_saveData.prologue, choice))
            {
                PlayCue(doorCue);
                EnterProductionStep(PhotoPrologueStep.MailboxHunt, true);
            }
        }

        private void RenderMailboxHunt()
        {
            var mask = _saveData.prologue.mailboxDetailsMask;
            SetPhase(Loc.Get(LocalizationTables.Photo, "production.mailbox.phase", "СЦЕНА 2 • ПОЧТОВЫЕ ЯЩИКИ"));
            SetSpeaker(Loc.Get(LocalizationTables.Photo, "speaker.viewfinder", "ВИДОИСКАТЕЛЬ"));
            SetContent(Loc.Get(LocalizationTables.Photo, "production.mailbox.prompt", "Мне нужен ещё один кадр перед выходом. В одной композиции видны повестка и бабочка."));
            SetStatus(Loc.Get(LocalizationTables.Photo, "production.mailbox.status", "Найдено деталей: {0}/2", CountMailboxDetails(mask)));
            ClearActions();
            CreateActionButton(((mask & PhotoPrologueRules.SummonsDetailBit) != 0 ? "[X] " : string.Empty) + Loc.Get(LocalizationTables.Photo, "production.mailbox.summons", "ПОВЕСТКА"), () => DiscoverMailbox(PhotoPrologueRules.SummonsDetailBit), (mask & PhotoPrologueRules.SummonsDetailBit) == 0, new Color(0.32f, 0.82f, 0.68f, 1f));
            CreateActionButton(((mask & PhotoPrologueRules.ButterflyDetailBit) != 0 ? "[X] " : string.Empty) + Loc.Get(LocalizationTables.Photo, "production.mailbox.butterfly", "БАБОЧКА"), () => DiscoverMailbox(PhotoPrologueRules.ButterflyDetailBit), (mask & PhotoPrologueRules.ButterflyDetailBit) == 0, AccentColor);
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "action.shutter", "СПУСК ЗАТВОРА"), BeginMailboxPublication, mask != 0, TextColor);
        }

        private void DiscoverMailbox(int detailBit)
        {
            if (PhotoPrologueRules.DiscoverMailboxDetail(_saveData.prologue, detailBit))
            {
                SaveCheckpoint(ExploreCheckpoint);
                RenderMailboxHunt();
            }
        }

        private void BeginMailboxPublication()
        {
            if (PhotoPrologueRules.BeginMailboxPublication(_saveData.prologue))
            {
                PlayCue(shutterCue);
                EnterProductionStep(PhotoPrologueStep.MailboxPublication, true);
            }
        }

        private void RenderMailboxPublication()
        {
            var mask = _saveData.prologue.mailboxDetailsMask;
            SetPhase(Loc.Get(LocalizationTables.Photo, "production.publish.phase", "ПЕРЕЛОМНЫЙ МОМЕНТ • КАКОЙ КАДР ОПУБЛИКОВАТЬ?"));
            SetSpeaker(Loc.Get(LocalizationTables.Photo, "production.heroine", "ОНА"));
            SetContent(Loc.Get(LocalizationTables.Photo, "production.publish.prompt", "Красивый образ защищает. Честный кадр рискует безопасностью. Баланс оставляет решение открытым."));
            SetStatus(Loc.Get(LocalizationTables.Photo, "production.publish.status", "Выберите, что увидит аудитория — и что останется за рамкой."));
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.publish.wings", "МАСКА «КРЫЛЬЯ»  •  +50 Признание"), () => ChooseMailboxPublication(PhotoMailboxPublication.Wings), (mask & PhotoPrologueRules.ButterflyDetailBit) != 0, AccentColor);
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.publish.honest", "ЧЕСТНОЕ ФОТО ПОВЕСТКИ  •  +50 Честность"), () => ChooseMailboxPublication(PhotoMailboxPublication.Honest), (mask & PhotoPrologueRules.SummonsDetailBit) != 0, new Color(0.32f, 0.82f, 0.68f, 1f));
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.publish.balance", "КАДРИРОВАНИЕ / БАЛАНС  •  +25 / +25"), () => ChooseMailboxPublication(PhotoMailboxPublication.Balance), mask == PhotoPrologueRules.AllMailboxDetailsMask, new Color(0.65f, 0.66f, 0.54f, 1f));
        }

        private void ChooseMailboxPublication(PhotoMailboxPublication publication)
        {
            if (PhotoPrologueRules.ApplyMailboxPublication(_saveData.prologue, publication))
            {
                EnterProductionStep(PhotoPrologueStep.MailboxReaction, true);
            }
        }

        private void RenderMailboxReaction()
        {
            SetPhase(Loc.Get(LocalizationTables.Photo, "production.reaction.phase", "FORBIDGRAM • ПУБЛИКАЦИЯ"));
            SetSpeaker(LocalizeProductionPath(_saveData.prologue.path));
            SetContent(_saveData.prologue.path switch
            {
                PhotoProloguePath.Honesty => Loc.Get(LocalizationTables.Photo, "production.reaction.honesty", "Пост скрывают из ленты. В личку приходит предупреждение: «Удали, в аэропорту проверяют публикации»."),
                PhotoProloguePath.Recognition => Loc.Get(LocalizationTables.Photo, "production.reaction.recognition", "Лайки растут. Никто не спрашивает, что находилось в нескольких сантиметрах от бабочки."),
                _ => Loc.Get(LocalizationTables.Photo, "production.reaction.balance", "Отклик остаётся ровным. Правда и красота помещаются рядом, но решение не становится легче.")
            });
            SetStatus(Loc.Get(LocalizationTables.Photo, "production.path.status", "Выбранный путь: {0}", LocalizeProductionPath(_saveData.prologue.path)));
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.action.airport", "ЕХАТЬ В АЭРОПОРТ"), ContinueToAirport, true, AccentColor);
        }

        private void ContinueToAirport()
        {
            if (PhotoPrologueRules.ContinueToAirport(_saveData.prologue))
            {
                EnterProductionStep(PhotoPrologueStep.AirportPhoto, true);
            }
        }

        private void RenderAirportPhoto()
        {
            SetPhase(Loc.Get(LocalizationTables.Photo, "production.airport.phase", "СЦЕНА 3 • АЭРОПОРТ ПУЛКОВО"));
            SetSpeaker(Loc.Get(LocalizationTables.Photo, "speaker.viewfinder", "ВИДОИСКАТЕЛЬ"));
            SetContent(Loc.Get(LocalizationTables.Photo, "production.airport.photo.prompt", "Ещё одно фото. Последнее с этой стороны границы."));
            SetStatus(Loc.Get(LocalizationTables.Photo, "production.airport.photo.status", "Этот снимок не меняет путь — его можно сделать или пропустить."));
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.airport.photo.take", "СНЯТЬ ОТРАЖЕНИЕ"), () => ResolveAirportPhoto(true), true, TextColor);
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.airport.photo.skip", "НЕ СНИМАТЬ"), () => ResolveAirportPhoto(false));
        }

        private void ResolveAirportPhoto(bool takePhoto)
        {
            if (PhotoPrologueRules.ResolveAirportPhoto(_saveData.prologue, takePhoto))
            {
                if (takePhoto) PlayCue(shutterCue);
                EnterProductionStep(PhotoPrologueStep.BorderControl, true);
            }
        }

        private void RenderBorderControl()
        {
            SetPhase(Loc.Get(LocalizationTables.Photo, "production.border.phase", "ПАСПОРТНЫЙ КОНТРОЛЬ"));
            SetSpeaker(Loc.Get(LocalizationTables.Photo, "production.border.officer", "ПОГРАНИЧНИК"));
            SetContent(Loc.Get(LocalizationTables.Photo, "production.border.prompt", "Цель поездки? Когда обратно?"));
            SetStatus(Loc.Get(LocalizationTables.Photo, "production.path.status", "Выбранный путь: {0}", LocalizeProductionPath(_saveData.prologue.path)));
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.border.honest", "Обратного билета нет."), () => ChooseBorderReply(PhotoBorderReply.Honest), PhotoPrologueRules.IsBorderReplyAvailable(_saveData.prologue, PhotoBorderReply.Honest), new Color(0.32f, 0.82f, 0.68f, 1f));
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.border.recognition", "Ну как я могу не вернуться в лучший город на планете?"), () => ChooseBorderReply(PhotoBorderReply.Recognition), PhotoPrologueRules.IsBorderReplyAvailable(_saveData.prologue, PhotoBorderReply.Recognition), AccentColor);
        }

        private void ChooseBorderReply(PhotoBorderReply reply)
        {
            if (PhotoPrologueRules.ApplyBorderReply(_saveData.prologue, reply))
            {
                PlayCue(passportStampCue);
                EnterProductionStep(PhotoPrologueStep.Summary, true);
            }
        }

        private void StartProductionDialogue(DialogueTree tree, PhotoPrologueStep step, Action fallback)
        {
            if (_dialogueRunning)
            {
                return;
            }

            if (tree == null || _dialogueController == null)
            {
                fallback();
                return;
            }

            _dialogueRunning = true;
            _dialogueStep = step;
            _dialogueChoiceIndex = -1;
            ApplyPresentationLayout(PresentationLayout.Dialogue);
            ClearActions();
            _dialogueController.StartDialogue(tree, _dialogueController, HandleDialogueFinished);
        }

        private void HandleDialogueSubtitle(SubtitlesRequestInfo request)
        {
            if (!_dialogueRunning || request?.statement == null)
            {
                return;
            }

            SetPhase(_dialogueStep == PhotoPrologueStep.MotherDialogue
                ? Loc.Get(LocalizationTables.Photo, "production.mother.phase", "КОМНАТА • МАТЬ В ДВЕРНОМ ПРОЁМЕ")
                : Loc.Get(LocalizationTables.Photo, "production.border.phase", "ПАСПОРТНЫЙ КОНТРОЛЬ"));
            SetSpeaker(_dialogueStep == PhotoPrologueStep.MotherDialogue
                ? Loc.Get(LocalizationTables.Photo, "production.mother.speaker", "МАТЬ")
                : Loc.Get(LocalizationTables.Photo, "production.border.officer", "ПОГРАНИЧНИК"));
            SetContent(LocalizeDialogueStatement(request.statement));
            SetStatus(Loc.Get(LocalizationTables.Photo, "production.dialogue.nodecanvas", "Dialogue Tree • NodeCanvas"));
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "action.next", "ДАЛЕЕ"), () => request.Continue(), true, AccentColor);
        }

        private void HandleDialogueChoices(MultipleChoiceRequestInfo request)
        {
            if (!_dialogueRunning || request == null)
            {
                return;
            }

            ClearActions();
            foreach (var option in request.options.OrderBy(pair => pair.Value))
            {
                var index = option.Value;
                var available = IsDialogueChoiceAvailable(_dialogueStep, index);
                var color = index == 0 ? new Color(0.32f, 0.82f, 0.68f, 1f) : AccentColor;
                CreateActionButton(LocalizeDialogueStatement(option.Key), () =>
                {
                    _dialogueChoiceIndex = index;
                    request.SelectOption(index);
                }, available, color);
            }
        }

        private void HandleDialogueFinished(bool succeeded)
        {
            if (_isShuttingDown)
            {
                _dialogueRunning = false;
                _dialogueChoiceIndex = -1;
                return;
            }

            var step = _dialogueStep;
            var choice = _dialogueChoiceIndex;
            _dialogueRunning = false;
            _dialogueChoiceIndex = -1;

            if (!succeeded || choice < 0)
            {
                if (step == PhotoPrologueStep.MotherDialogue) RenderMotherDialogue();
                else RenderBorderControl();
                return;
            }

            if (step == PhotoPrologueStep.MotherDialogue)
            {
                ChooseMotherReply(choice == 0 ? PhotoMotherReply.Honest : PhotoMotherReply.ProtectiveLie);
                return;
            }

            ChooseBorderReply(choice == 0 ? PhotoBorderReply.Honest : PhotoBorderReply.Recognition);
        }

        private bool IsDialogueChoiceAvailable(PhotoPrologueStep step, int index)
        {
            if (step == PhotoPrologueStep.MotherDialogue)
            {
                return index != 0 || _saveData.prologue.honesty >= 20;
            }

            var reply = index == 0 ? PhotoBorderReply.Honest : PhotoBorderReply.Recognition;
            return PhotoPrologueRules.IsBorderReplyAvailable(_saveData.prologue, reply);
        }

        private static string LocalizeDialogueStatement(NodeCanvas.DialogueTrees.IStatement statement)
        {
            var key = statement.text;
            return string.IsNullOrWhiteSpace(key)
                ? string.Empty
                : Loc.Get(LocalizationTables.Photo, key, key);
        }

        private void UpdateAmbience(PhotoPrologueStep step)
        {
            var target = step >= PhotoPrologueStep.AirportPhoto ? airportAmbienceCue : roomAmbienceCue;
            if (target == null)
            {
                return;
            }

            StopAmbience();
            _ambienceHandle = AudioService.Instance?.Play(target) ?? default;
        }

        private void StopAmbience()
        {
            if (_ambienceHandle.IsValid)
            {
                AudioService.Instance?.Stop(_ambienceHandle, 0.25f);
                _ambienceHandle = default;
            }
        }

        private static void PlayCue(AudioCue cue)
        {
            if (cue != null)
            {
                AudioService.Instance?.Play(cue);
            }
        }

        private void RenderProductionSummary()
        {
            SetPhase(Loc.Get(LocalizationTables.Photo, "production.summary.phase", "РЕЖИМ ПОЛЁТА: ВКЛЮЧЁН"));
            SetSpeaker(Loc.Get(LocalizationTables.Photo, "production.heroine", "ОНА"));
            SetContent(Loc.Get(LocalizationTables.Photo, "production.summary.body", "Началось.\n\nИтоговый путь: {0}", LocalizeProductionPath(_saveData.prologue.path)));
            SetStatus(Loc.Get(LocalizationTables.Photo, "production.summary.status", "Штамп поставлен. Следующая остановка — транзитная гостиница."));
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "production.summary.complete", "ЗАВЕРШИТЬ ПРОЛОГ"), CompleteProductionPrologue, true, AccentColor);
        }

        private void CompleteProductionPrologue()
        {
            if (_outroCutsceneRunning || _episodeHandedOff)
            {
                return;
            }

            if (_saveData.prologue.step == PhotoPrologueStep.Summary
                && !PhotoPrologueRules.Complete(_saveData.prologue))
            {
                return;
            }

            _saveData.prologue.completed = true;
            _phase = PhotoWhiteboxPhase.Arrival;
            SaveCheckpoint(ArrivalCheckpoint);
            StartOutroCutsceneOrComplete();
        }

        private void StartOutroCutsceneOrComplete()
        {
            var director = CutsceneDirector.Instance;
            if (director == null)
            {
                CompleteProductionFlow();
                return;
            }

            _outroCutsceneRunning = true;
            ClearActions();
            SetStatus(Loc.Get(LocalizationTables.Photo, "production.outro.loading", "Финальная сцена…"));
            director.Finished += HandleOutroCutsceneFinished;

            var context = new CutsceneContext
            {
                characterId = CharacterId.Photo.ToString(),
                startCheckpointId = PublishedCheckpoint,
                completionCheckpointId = ArrivalCheckpoint
            };

            if (director.TryPlay(OutroCutsceneId, context, out var error))
            {
                return;
            }

            UnsubscribeFromOutroCutscene();
            _outroCutsceneRunning = false;
            Debug.LogWarning($"Photo outro cutscene fallback: {error}");
            CompleteProductionFlow();
        }

        private void HandleOutroCutsceneFinished(CutsceneResult result)
        {
            if (result.CutsceneId != OutroCutsceneId)
            {
                return;
            }

            UnsubscribeFromOutroCutscene();
            _outroCutsceneRunning = false;
            CompleteProductionFlow();
        }

        private void UnsubscribeFromOutroCutscene()
        {
            if (CutsceneDirector.Instance != null)
            {
                CutsceneDirector.Instance.Finished -= HandleOutroCutsceneFinished;
            }
        }

        private void CompleteProductionFlow()
        {
            if (_episodeHandedOff)
            {
                return;
            }

            _episodeHandedOff = true;

            var result = new EpisodeResult
            {
                characterId = CharacterId.Photo,
                sceneName = gameObject.scene.name,
                checkpointId = ArrivalCheckpoint,
                payloadJson = PhotoCheckpointAdapter.Serialize(_saveData, ArrivalCheckpoint),
                episodeCompleted = true,
                arrivalTable = LocalizationTables.Photo,
                arrivalKey = "production.arrival.body",
                arrivalFallback = "Дверь транзитного номера закрывается. Впервые за день уведомления молчат."
            };

            result
                .AddLine(LocalizationTables.Photo, "production.result.path", "ПУТЬ", LocalizeProductionPath(_saveData.prologue.path));

            GameFlowService.CompleteEpisodeAndReturnToCharacterSelect(result);
        }

        private void SetScaleStatus()
        {
            SetStatus(Loc.Get(LocalizationTables.Photo, "production.scales", "Честность {0}/100  •  Признание {1}/100", _saveData.prologue.honesty, _saveData.prologue.recognition));
        }

        private static int CountMailboxDetails(int mask)
        {
            var count = 0;
            if ((mask & PhotoPrologueRules.SummonsDetailBit) != 0) count++;
            if ((mask & PhotoPrologueRules.ButterflyDetailBit) != 0) count++;
            return count;
        }

        private static PhotoWhiteboxPhase PhaseForProductionStep(PhotoPrologueStep step)
        {
            return step switch
            {
                PhotoPrologueStep.MailboxPublication => PhotoWhiteboxPhase.Camera,
                PhotoPrologueStep.MailboxReaction => PhotoWhiteboxPhase.Publish,
                PhotoPrologueStep.AirportPhoto => PhotoWhiteboxPhase.Publish,
                PhotoPrologueStep.BorderControl => PhotoWhiteboxPhase.ReflectionDialogue,
                PhotoPrologueStep.Summary => PhotoWhiteboxPhase.ReflectionDialogue,
                PhotoPrologueStep.Complete => PhotoWhiteboxPhase.Arrival,
                _ => PhotoWhiteboxPhase.Explore
            };
        }

        private static string CheckpointForProductionStep(PhotoPrologueStep step)
        {
            return step switch
            {
                PhotoPrologueStep.MailboxPublication => CameraCheckpoint,
                PhotoPrologueStep.MailboxReaction => PublishedCheckpoint,
                PhotoPrologueStep.AirportPhoto => PublishedCheckpoint,
                PhotoPrologueStep.BorderControl => PublishedCheckpoint,
                PhotoPrologueStep.Summary => PublishedCheckpoint,
                PhotoPrologueStep.Complete => ArrivalCheckpoint,
                _ => ExploreCheckpoint
            };
        }

        private static string LocalizeProductionPath(PhotoProloguePath path)
        {
            return path switch
            {
                PhotoProloguePath.Honesty => Loc.Get(LocalizationTables.Photo, "production.path.honesty", "ЧЕСТНОСТЬ"),
                PhotoProloguePath.Recognition => Loc.Get(LocalizationTables.Photo, "production.path.recognition", "ПРИЗНАНИЕ"),
                PhotoProloguePath.Balance => Loc.Get(LocalizationTables.Photo, "production.path.balance", "БАЛАНС"),
                _ => Loc.Get(LocalizationTables.Photo, "production.path.undecided", "НЕ ОПРЕДЕЛЁН")
            };
        }

        private void RenderExplore()
        {
            SetPhase(Loc.Get(LocalizationTables.Photo, "phase.explore", "ИССЛЕДОВАНИЕ • ДВОР И ПОЧТОВЫЕ ЯЩИКИ"));
            SetSpeaker(Loc.Get(LocalizationTables.Photo, "speaker.whitebox", "WHITE-BOX СЦЕНА"));
            SetContent(Loc.Get(LocalizationTables.Photo, "explore.description", "Серый подъезд. У стены — почтовые ящики. Рядом лежит собранный чемодан. Телефон продолжает вибрировать. Осмотрите три детали, чтобы разблокировать камеру."));
            SetStatus(Loc.Get(LocalizationTables.Photo, "status.inspected", "Осмотрено: {0} / 3", CountInspected()));
            ClearActions();
            CreateInspectionButton(0b001, Loc.Get(LocalizationTables.Photo, "inspect.phone.label", "ТЕЛЕФОН • сообщение редактора"), Loc.Get(LocalizationTables.Photo, "inspect.phone.text", "Агентство уезжает. Зарплаты за последний месяц может не быть."));
            CreateInspectionButton(0b010, Loc.Get(LocalizationTables.Photo, "inspect.suitcase.label", "ЧЕМОДАН • билет через Дубай"), Loc.Get(LocalizationTables.Photo, "inspect.suitcase.text", "Маршрут заканчивается словом «Бали». Дальше — пустое место."));
            CreateInspectionButton(0b100, Loc.Get(LocalizationTables.Photo, "inspect.mailbox.label", "ПОЧТОВЫЙ ЯЩИК • движение внутри"), Loc.Get(LocalizationTables.Photo, "inspect.mailbox.text", "Из щели торчит военная повестка. На холодном металле садится бабочка."));

            if (_inspectedMask == RequiredInspectionMask)
            {
                CreateActionButton(Loc.Get(LocalizationTables.Photo, "action.take_camera", "ДОСТАТЬ КАМЕРУ"), () => EnterPhase(PhotoWhiteboxPhase.Camera, true), true, AccentColor);
            }
        }

        private void CreateInspectionButton(int bit, string label, string observation)
        {
            var inspected = (_inspectedMask & bit) != 0;
            CreateActionButton(
                inspected ? $"✓ {label}" : label,
                () => Inspect(bit, observation),
                !inspected,
                inspected ? SelectedColor : ButtonColor);
        }

        private void Inspect(int bit, string observation)
        {
            _inspectedMask |= bit;
            SaveCheckpoint(ExploreCheckpoint);
            RenderExplore();
            SetContent(observation);
            SyncNodeCanvas();
        }

        private void RenderCamera()
        {
            SetPhase(Loc.Get(LocalizationTables.Photo, "phase.camera", "КАМЕРА • ВЫБОР КАДРА"));
            SetSpeaker(Loc.Get(LocalizationTables.Photo, "speaker.viewfinder", "ВИДОИСКАТЕЛЬ"));
            SetContent(_choice switch
            {
                PhotoChoice.Summons => Loc.Get(LocalizationTables.Photo, "camera.summons", "В рамке — край почтового ящика и торчащая повестка. Это честно, но небезопасно."),
                PhotoChoice.Butterfly => Loc.Get(LocalizationTables.Photo, "camera.butterfly", "В рамке — бабочка на металле. Красиво, безопасно и почти ничего не говорит о происходящем."),
                _ => Loc.Get(LocalizationTables.Photo, "camera.none", "Обе цели находятся в одной композиции. Выберите, на чём сфокусировать кадр.")
            });
            SetStatus(_choice == PhotoChoice.None
                ? Loc.Get(LocalizationTables.Photo, "status.no_target", "Цель не выбрана")
                : Loc.Get(LocalizationTables.Photo, "status.focus", "Фокус: {0}", LocalizeChoice(_choice)));
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "action.summons", "[ ПОВЕСТКА ] • Truth"), () => SelectTarget(PhotoChoice.Summons), true,
                _choice == PhotoChoice.Summons ? SelectedColor : ButtonColor);
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "action.butterfly", "[ БАБОЧКА ] • Reach"), () => SelectTarget(PhotoChoice.Butterfly), true,
                _choice == PhotoChoice.Butterfly ? SelectedColor : ButtonColor);
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "action.shutter", "СПУСК ЗАТВОРА"), CapturePhoto, _choice != PhotoChoice.None, AccentColor);
        }

        private void SelectTarget(PhotoChoice choice)
        {
            _choice = choice;
            RenderCamera();
            SyncNodeCanvas();
        }

        private void CapturePhoto()
        {
            if (_choice == PhotoChoice.None)
            {
                return;
            }

            (_truth, _reach) = _choice == PhotoChoice.Summons ? (2, 1) : (0, 2);
            _saveData.prologue.publicationCommitted = true;
            EnterPhase(PhotoWhiteboxPhase.Publish, true);
        }

        private void RenderPublish()
        {
            SetPhase(Loc.Get(LocalizationTables.Photo, "phase.publish", "FORBIDGRAM • ПУБЛИКАЦИЯ"));
            SetSpeaker(Loc.Get(LocalizationTables.Photo, _choice == PhotoChoice.Summons ? "speaker.honest_shot" : "speaker.safe_shot", _choice == PhotoChoice.Summons ? "ЧЕСТНЫЙ КАДР" : "БЕЗОПАСНЫЙ КАДР"));
            SetContent(_choice == PhotoChoice.Summons
                ? Loc.Get(LocalizationTables.Photo, "publish.summons", "Публикацию замечают быстро. Вместе с поддержкой приходят вопросы и страх удалить снимок.")
                : Loc.Get(LocalizationTables.Photo, "publish.butterfly", "Лайки растут быстрее обычного. Никто не спрашивает, что находилось в нескольких сантиметрах от бабочки."));
            SetStatus(Loc.Get(LocalizationTables.Photo, "status.published", "Truth +{0}   •   Reach +{1}   •   Платёж получен", _truth, _reach));
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "action.reflect", "ПОСМОТРЕТЬ НА СВОЙ ВЫБОР"), () => EnterPhase(PhotoWhiteboxPhase.ReflectionDialogue, false));
        }

        private void RenderReflection()
        {
            SetPhase(Loc.Get(LocalizationTables.Photo, "phase.reflection", "ОТРАЖЕНИЕ"));
            SetSpeaker(Loc.Get(LocalizationTables.Photo, "prologue.intro.001.speaker", "ОНА"));
            SetContent(_choice == PhotoChoice.Summons
                ? Loc.Get(LocalizationTables.Photo, "reflection.summons", "Я хотя бы не сделала вид, что ничего не происходит. Теперь надо решить, сколько правды я смогу увезти с собой.")
                : Loc.Get(LocalizationTables.Photo, "reflection.butterfly", "Красивый кадр снова сработал. Только почему он ощущается ещё одной вещью, которую я оставляю здесь?")
            );
            SetStatus(Loc.Get(LocalizationTables.Photo, "status.saved_choice", "Сохранённый выбор: {0} • Truth {1} • Reach {2}", LocalizeChoice(_choice), _truth, _reach));
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "action.airport", "ЕХАТЬ В АЭРОПОРТ"), () => EnterPhase(PhotoWhiteboxPhase.Arrival, true), true, AccentColor);
        }

        private void RenderArrival()
        {
            SetPhase(Loc.Get(LocalizationTables.Photo, "phase.arrival", "ДУБАЙ • ТРАНЗИТНАЯ ГОСТИНИЦА"));
            SetSpeaker(Loc.Get(LocalizationTables.Photo, "speaker.prologue_finale", "WHITE-BOX ФИНАЛ ПРОЛОГА"));
            SetContent(Loc.Get(LocalizationTables.Photo, "arrival.description", "Дверь гостиничного номера закрывается. На экране телефона остаётся опубликованный кадр. Маршрут продолжается на Бали — уже в следующем акте."));
            SetStatus(Loc.Get(LocalizationTables.Photo, "status.arrival_saved", "Checkpoint photo.arrival сохранён"));
            ClearActions();
            CreateActionButton(Loc.Get(LocalizationTables.Photo, "action.complete", "ЗАВЕРШИТЬ ПРОЛОГ"), CompletePrologue, true, AccentColor);
        }

        private void CompletePrologue()
        {
            _saveData.prologue.completed = true;
            SaveCheckpoint(ArrivalCheckpoint);
            GameSaveService.LeaveCharacterLine(CharacterId.Photo, "CharacterSelect");
            SceneManager.LoadSceneAsync("CharacterSelect", LoadSceneMode.Single);
        }

        private void SaveCheckpoint(string checkpointId)
        {
            _saveData.activeAct = PhotoAct.Prologue;
            _saveData.prologue.phase = _phase;
            _saveData.prologue.introIndex = _introIndex;
            _saveData.prologue.inspectedMask = _inspectedMask;
            _saveData.prologue.photoChoice = _choice;
            _saveData.prologue.truth = _truth;
            _saveData.prologue.reach = _reach;

            GameSaveService.SaveCharacterCheckpoint(
                CharacterId.Photo,
                gameObject.scene.name,
                checkpointId,
                PhotoCheckpointAdapter.Serialize(_saveData, checkpointId));
        }

        public bool TrySave(out string message)
        {
            var checkpointId = _phase switch
            {
                PhotoWhiteboxPhase.IntroDialogue => "photo.intro",
                PhotoWhiteboxPhase.Explore => ExploreCheckpoint,
                PhotoWhiteboxPhase.Camera => CameraCheckpoint,
                PhotoWhiteboxPhase.Publish => PublishedCheckpoint,
                PhotoWhiteboxPhase.ReflectionDialogue => PublishedCheckpoint,
                PhotoWhiteboxPhase.Arrival => ArrivalCheckpoint,
                _ => ExploreCheckpoint
            };

            SaveCheckpoint(checkpointId);
            message = Loc.Get(LocalizationTables.Photo, "status.saved", "Сохранено: {0}", checkpointId);
            return true;
        }

        private int CountInspected()
        {
            var count = 0;
            if ((_inspectedMask & 0b001) != 0) count++;
            if ((_inspectedMask & 0b010) != 0) count++;
            if ((_inspectedMask & 0b100) != 0) count++;
            return count;
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject(
                "PhotoWhiteboxCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var background = CreateImage("Background", canvasObject.GetComponent<RectTransform>(), BackgroundColor);
            Stretch(background.rectTransform);

            var topAccent = CreateImage("TopAccent", background.rectTransform, AccentColor);
            SetAnchoredRect(topAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 6f));

            var panel = CreateImage("StoryPanel", background.rectTransform, PanelColor);
            SetAnchoredRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1740f, 980f));

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 26, 22);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _phaseText = CreateLabel("Phase", panel.rectTransform, string.Empty, 24, FontStyles.Bold, AccentColor, 36f);
            CreateScaleRibbon(panel.rectTransform);
            _speakerText = CreateLabel("Speaker", panel.rectTransform, string.Empty, 18, FontStyles.Bold, MutedTextColor, 26f);

            var stage = CreateImage("Stage", panel.rectTransform, StageColor);
            _stageLayout = stage.gameObject.AddComponent<LayoutElement>();
            _stageLayout.preferredHeight = 480f;
            var roomImageObject = new GameObject("RoomDiorama", typeof(RectTransform), typeof(RawImage));
            roomImageObject.transform.SetParent(stage.rectTransform, false);
            _roomDioramaImage = roomImageObject.GetComponent<RawImage>();
            _roomDioramaImage.texture = _roomDiorama != null ? _roomDiorama.OutputTexture : null;
            _roomDioramaImage.color = Color.white;
            _roomDioramaImage.raycastTarget = false;
            Stretch(_roomDioramaImage.rectTransform);
            _roomDioramaImage.gameObject.SetActive(false);
            CreateStageDecorations(stage.rectTransform);
            _contentText = CreateText("Content", stage.rectTransform, string.Empty, 28, FontStyles.Normal, TextColor);
            _contentRect = _contentText.rectTransform;
            Stretch(_contentRect, new Vector2(56f, 36f), new Vector2(-56f, -36f));

            _statusText = CreateLabel("Status", panel.rectTransform, string.Empty, 17, FontStyles.Normal, MutedTextColor, 32f);

            var actions = new GameObject("DecisionBand", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            actions.transform.SetParent(panel.rectTransform, false);
            actions.GetComponent<Image>().color = new Color(0.28f, 0.02f, 0.15f, 0.82f);
            _actionsLayout = actions.GetComponent<LayoutElement>();
            _actionsLayout.preferredHeight = 240f;
            _actionsPanelRect = actions.GetComponent<RectTransform>();

            _dialoguePortrait = new GameObject("RenderedPortrait", typeof(RectTransform), typeof(RawImage));
            _dialoguePortrait.transform.SetParent(_actionsPanelRect, false);
            var portraitRect = _dialoguePortrait.GetComponent<RectTransform>();
            SetAnchoredRect(portraitRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(132f, 0f), new Vector2(190f, 190f));
            _dialoguePortraitImage = _dialoguePortrait.GetComponent<RawImage>();
            _dialoguePortraitImage.color = Color.white;
            _dialoguePortraitImage.raycastTarget = false;
            _dialoguePortraitImage.texture = _roomDiorama != null ? _roomDiorama.PortraitTexture : null;

            var decisions = new GameObject("Choices", typeof(RectTransform), typeof(GridLayoutGroup));
            decisions.transform.SetParent(_actionsPanelRect, false);
            _actionsRoot = decisions.GetComponent<RectTransform>();
            Stretch(_actionsRoot, new Vector2(24f, 20f), new Vector2(-24f, -20f));
            _actionsGrid = decisions.GetComponent<GridLayoutGroup>();
            _actionsGrid.padding = new RectOffset(24, 24, 20, 20);
            _actionsGrid.spacing = new Vector2(18f, 14f);
            _actionsGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            _actionsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _actionsGrid.constraintCount = 1;
            _actionsGrid.cellSize = new Vector2(1596f, 64f);

            CreateLabel("Footer", panel.rectTransform, "REFLECTION / MOMENTUM  •  PHOTO", 13, FontStyles.Normal, MutedTextColor, 20f);
            ApplyPresentationLayout(PresentationLayout.Dialogue);
        }

        private void SetRoomDioramaVisible(bool visible)
        {
            if (_roomDioramaImage != null)
            {
                _roomDioramaImage.texture = _roomDiorama != null ? _roomDiorama.OutputTexture : null;
                _roomDioramaImage.gameObject.SetActive(visible && _roomDioramaImage.texture != null);
            }

            if (_dialoguePortraitImage != null)
            {
                _dialoguePortraitImage.texture = visible && _roomDiorama != null ? _roomDiorama.PortraitTexture : null;
            }

            if (!visible) _roomDiorama?.Hide();
        }

        private void CreateScaleRibbon(RectTransform parent)
        {
            var ribbon = CreateImage("InnerVector", parent, new Color(0.06f, 0.065f, 0.085f, 0.95f));
            ribbon.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

            var honesty = CreateImage("HonestyFill", ribbon.rectTransform, new Color(0.32f, 0.82f, 0.68f, 0.92f));
            honesty.rectTransform.anchorMin = Vector2.zero;
            honesty.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            honesty.rectTransform.offsetMin = new Vector2(4f, 22f);
            honesty.rectTransform.offsetMax = new Vector2(-2f, -6f);
            _honestyFill = honesty.rectTransform;

            var recognition = CreateImage("RecognitionFill", ribbon.rectTransform, AccentColor);
            recognition.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            recognition.rectTransform.anchorMax = Vector2.one;
            recognition.rectTransform.offsetMin = new Vector2(2f, 22f);
            recognition.rectTransform.offsetMax = new Vector2(-4f, -6f);
            _recognitionFill = recognition.rectTransform;

            _honestyLabel = CreateText("HonestyLabel", ribbon.rectTransform, "ЧЕСТНОСТЬ", 14, FontStyles.Bold, new Color(0.32f, 0.82f, 0.68f, 1f));
            SetAnchoredRect(_honestyLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(76f, 11f), new Vector2(150f, 22f));
            _honestyLabel.alignment = TextAlignmentOptions.MidlineLeft;

            _recognitionLabel = CreateText("RecognitionLabel", ribbon.rectTransform, "ПРИЗНАНИЕ", 14, FontStyles.Bold, AccentColor);
            SetAnchoredRect(_recognitionLabel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-82f, 11f), new Vector2(164f, 22f));
            _recognitionLabel.alignment = TextAlignmentOptions.MidlineRight;
        }

        private void CreateStageDecorations(RectTransform stage)
        {
            _viewfinderOverlay = new GameObject("ViewfinderOverlay", typeof(RectTransform));
            _viewfinderOverlay.transform.SetParent(stage, false);
            Stretch(_viewfinderOverlay.GetComponent<RectTransform>(), new Vector2(30f, 24f), new Vector2(-30f, -24f));

            CreateGuideLine("ThirdLeft", _viewfinderOverlay.transform, new Vector2(0.333f, 0f), new Vector2(0.333f, 1f), new Vector2(2f, 0f));
            CreateGuideLine("ThirdRight", _viewfinderOverlay.transform, new Vector2(0.666f, 0f), new Vector2(0.666f, 1f), new Vector2(2f, 0f));
            CreateGuideLine("ThirdTop", _viewfinderOverlay.transform, new Vector2(0f, 0.666f), new Vector2(1f, 0.666f), new Vector2(0f, 2f));
            CreateGuideLine("ThirdBottom", _viewfinderOverlay.transform, new Vector2(0f, 0.333f), new Vector2(1f, 0.333f), new Vector2(0f, 2f));
            var shutter = CreateText("Shutter", _viewfinderOverlay.GetComponent<RectTransform>(), "O", 72, FontStyles.Bold, TextColor);
            SetAnchoredRect(shutter.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-70f, 0f), new Vector2(100f, 100f));

        }

        private static void CreateGuideLine(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            var line = CreateImage(name, parent.GetComponent<RectTransform>(), new Color(0.78f, 0.44f, 0.68f, 0.32f));
            line.rectTransform.anchorMin = anchorMin;
            line.rectTransform.anchorMax = anchorMax;
            line.rectTransform.anchoredPosition = Vector2.zero;
            line.rectTransform.sizeDelta = size;
        }

        private void ApplyPresentationLayout(PresentationLayout presentation)
        {
            var viewfinder = presentation == PresentationLayout.Viewfinder;
            var matrix = presentation == PresentationLayout.ChoiceMatrix;
            var dialogue = presentation == PresentationLayout.Dialogue;

            _viewfinderOverlay?.SetActive(viewfinder);
            _dialoguePortrait?.SetActive(dialogue);
            if (_actionsRoot != null)
            {
                _actionsRoot.offsetMin = new Vector2(dialogue ? 250f : 24f, 20f);
                _actionsRoot.offsetMax = new Vector2(-24f, -20f);
            }
            _actionsGrid.constraintCount = matrix ? 3 : viewfinder ? 2 : 1;
            _actionsGrid.cellSize = matrix
                ? new Vector2(510f, 300f)
                : viewfinder
                    ? new Vector2(790f, 76f)
                    : new Vector2(dialogue ? 1370f : 1596f, presentation == PresentationLayout.Summary ? 64f : 58f);
            _stageLayout.preferredHeight = matrix ? 250f : viewfinder ? 520f : 450f;
            _actionsLayout.preferredHeight = matrix ? 340f : viewfinder ? 210f : presentation == PresentationLayout.Summary ? 110f : 240f;
            _actionButtonHeight = matrix ? 300f : viewfinder ? 76f : 64f;

            _contentText.fontSize = matrix ? 25f : 29f;
            _contentText.alignment = dialogue ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            Stretch(
                _contentRect,
                dialogue ? new Vector2(340f, 42f) : new Vector2(70f, 42f),
                viewfinder ? new Vector2(-160f, -42f) : new Vector2(-70f, -42f));
        }

        private void UpdateScaleRibbon()
        {
            if (_honestyFill == null || _recognitionFill == null)
            {
                return;
            }

            var total = Mathf.Max(1, _saveData.prologue.honesty + _saveData.prologue.recognition);
            var split = Mathf.Clamp01((float)_saveData.prologue.honesty / total);
            _honestyFill.anchorMax = new Vector2(split, 1f);
            _recognitionFill.anchorMin = new Vector2(split, 0f);
            _honestyLabel.text = Loc.Get(LocalizationTables.Photo, "production.path.honesty", "ЧЕСТНОСТЬ");
            _recognitionLabel.text = Loc.Get(LocalizationTables.Photo, "production.path.recognition", "ПРИЗНАНИЕ");
        }

        private static PresentationLayout LayoutForStep(PhotoPrologueStep step)
        {
            return step switch
            {
                PhotoPrologueStep.RoomPhoto or PhotoPrologueStep.MailboxHunt or PhotoPrologueStep.AirportPhoto => PresentationLayout.Viewfinder,
                PhotoPrologueStep.MailboxPublication => PresentationLayout.ChoiceMatrix,
                PhotoPrologueStep.MailboxReaction or PhotoPrologueStep.Summary or PhotoPrologueStep.Complete => PresentationLayout.Summary,
                _ => PresentationLayout.Dialogue
            };
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private void ClearActions()
        {
            foreach (Transform child in _actionsRoot)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private Button CreateActionButton(
            string label,
            UnityEngine.Events.UnityAction action,
            bool interactable = true,
            Color? color = null)
        {
            var buttonObject = new GameObject("Action", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(_actionsRoot, false);
            buttonObject.GetComponent<LayoutElement>().preferredHeight = _actionButtonHeight;

            var image = buttonObject.GetComponent<Image>();
            var tint = color ?? ButtonColor;
            image.color = Color.Lerp(BackgroundColor, tint, 0.42f);
            var outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(tint.r, tint.g, tint.b, interactable ? 0.8f : 0.25f);
            outline.effectDistance = new Vector2(2f, -2f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.interactable = interactable;
            button.onClick.AddListener(action);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = AccentColor;
            colors.selectedColor = AccentColor;
            colors.pressedColor = TextColor;
            colors.disabledColor = new Color(ButtonColor.r, ButtonColor.g, ButtonColor.b, 0.35f);
            button.colors = colors;

            var labelText = CreateText("Label", buttonObject.GetComponent<RectTransform>(), label, 18, FontStyles.Bold, TextColor);
            Stretch(labelText.rectTransform, new Vector2(20f, 0f), new Vector2(-20f, 0f));
            return button;
        }

        private TMP_Text CreateLabel(string name, RectTransform parent, string value, int size, FontStyles style, Color color, float height)
        {
            var text = CreateText(name, parent, value, size, style, color);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return text;
        }

        private TMP_Text CreateText(string name, RectTransform parent, string value, int size, FontStyles style, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(string name, RectTransform parent, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void Stretch(RectTransform rect, Vector2? min = null, Vector2? max = null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min ?? Vector2.zero;
            rect.offsetMax = max ?? Vector2.zero;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void SetPhase(string value) => _phaseText.text = value;
        private void SetSpeaker(string value) => _speakerText.text = value;
        private void SetContent(string value) => _contentText.text = value;
        private void SetStatus(string value) => _statusText.text = value;

        private static string LocalizeChoice(PhotoChoice choice)
        {
            return Loc.Get(LocalizationTables.Photo, "choice." + choice.ToString().ToLowerInvariant(), choice.ToString());
        }
    }
}
