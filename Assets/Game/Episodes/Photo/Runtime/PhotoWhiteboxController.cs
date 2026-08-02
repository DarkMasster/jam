using System;
using Jam.Core.Cutscenes;
using Jam.Core.Flow;
using Jam.Core.Localization;
using Jam.Core.Save;
using NodeCanvas.Framework;
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

    [RequireComponent(typeof(FSMOwner), typeof(Blackboard))]
    public sealed class PhotoWhiteboxController : MonoBehaviour, IGameModeSaveProvider
    {
        private const string IntroCutsceneId = "photo.prologue.intro";
        private const string OutroCutsceneId = "photo.prologue.to_be_continued";
        private const string ExploreCheckpoint = "photo.explore";
        private const string CameraCheckpoint = "photo.camera";
        private const string PublishedCheckpoint = "photo.published";
        private const string ArrivalCheckpoint = "photo.arrival";
        private const int RequiredInspectionMask = PhotoCheckpointAdapter.RequiredInspectionMask;

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
        private TMP_Text _phaseText;
        private TMP_Text _speakerText;
        private TMP_Text _contentText;
        private TMP_Text _statusText;
        private RectTransform _actionsRoot;
        private PhotoWhiteboxPhase _phase;
        private PhotoChoice _choice;
        private int _introIndex;
        private int _inspectedMask;
        private int _truth;
        private int _reach;
        private bool _introCutsceneRunning;
        private bool _outroCutsceneRunning;
        private bool _episodeHandedOff;
        private PhotoCharacterSaveData _saveData = PhotoCheckpointAdapter.CreateNew();

        public bool CanSave => isActiveAndEnabled;
        public string ModeName => Loc.Get(LocalizationTables.Photo, "mode.name", "История фотографки");

        private void Awake()
        {
            _fsmOwner = GetComponent<FSMOwner>();
            _blackboard = GetComponent<Blackboard>();
            EnsureEventSystem();
            BuildInterface();
        }

        private void Start()
        {
            RestoreOrBegin();
        }

        private void OnEnable()
        {
            Loc.LocaleChanged += HandleLocaleChanged;
        }

        private void OnDisable()
        {
            Loc.LocaleChanged -= HandleLocaleChanged;
            UnsubscribeFromCutsceneDirector();
            UnsubscribeFromOutroCutscene();
            _introCutsceneRunning = false;
            _outroCutsceneRunning = false;
        }

        private void HandleLocaleChanged()
        {
            if (_introCutsceneRunning)
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

            RenderProductionStep();
        }

        private void RenderProductionStep()
        {
            switch (_saveData.prologue.step)
            {
                case PhotoPrologueStep.RoomSecret: RenderRoomSecret(); break;
                case PhotoPrologueStep.RoomPhoto: RenderRoomPhoto(); break;
                case PhotoPrologueStep.MotherDialogue: RenderMotherDialogue(); break;
                case PhotoPrologueStep.MailboxHunt: RenderMailboxHunt(); break;
                case PhotoPrologueStep.MailboxPublication: RenderMailboxPublication(); break;
                case PhotoPrologueStep.MailboxReaction: RenderMailboxReaction(); break;
                case PhotoPrologueStep.AirportPhoto: RenderAirportPhoto(); break;
                case PhotoPrologueStep.BorderControl: RenderBorderControl(); break;
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
                EnterProductionStep(PhotoPrologueStep.Summary, true);
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
            SetAnchoredRect(topAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 8f));

            var panel = CreateImage("StoryPanel", background.rectTransform, PanelColor);
            SetAnchoredRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 880f));

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(58, 58, 42, 36);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _phaseText = CreateLabel("Phase", panel.rectTransform, string.Empty, 22, FontStyles.Bold, AccentColor, 38f);
            _speakerText = CreateLabel("Speaker", panel.rectTransform, string.Empty, 18, FontStyles.Bold, MutedTextColor, 30f);

            var stage = CreateImage("Stage", panel.rectTransform, StageColor);
            stage.gameObject.AddComponent<LayoutElement>().preferredHeight = 300f;
            _contentText = CreateText("Content", stage.rectTransform, string.Empty, 28, FontStyles.Normal, TextColor);
            Stretch(_contentText.rectTransform, new Vector2(42f, 30f), new Vector2(-42f, -30f));

            _statusText = CreateLabel("Status", panel.rectTransform, string.Empty, 17, FontStyles.Normal, MutedTextColor, 34f);

            var actions = new GameObject("Actions", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            actions.transform.SetParent(panel.rectTransform, false);
            actions.GetComponent<LayoutElement>().preferredHeight = 340f;
            var actionsLayout = actions.GetComponent<VerticalLayoutGroup>();
            actionsLayout.spacing = 10f;
            actionsLayout.childControlWidth = true;
            actionsLayout.childControlHeight = false;
            actionsLayout.childForceExpandWidth = true;
            actionsLayout.childForceExpandHeight = false;
            _actionsRoot = actions.GetComponent<RectTransform>();

            CreateLabel("Footer", panel.rectTransform, "WHITE-BOX • PHOTO / CHARACTER 3", 14, FontStyles.Normal, MutedTextColor, 24f);
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
            buttonObject.GetComponent<LayoutElement>().preferredHeight = 58f;

            var image = buttonObject.GetComponent<Image>();
            image.color = color ?? ButtonColor;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.interactable = interactable;
            button.onClick.AddListener(action);

            var colors = button.colors;
            colors.normalColor = color ?? ButtonColor;
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
