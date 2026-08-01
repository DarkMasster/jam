using System;
using Jam.Core.Cutscenes;
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
            _introCutsceneRunning = false;
        }

        private void HandleLocaleChanged()
        {
            if (_introCutsceneRunning)
            {
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
                EnterPhase(PhotoWhiteboxPhase.Explore, true);
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

            EnterPhase(PhotoWhiteboxPhase.Explore, true);
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
