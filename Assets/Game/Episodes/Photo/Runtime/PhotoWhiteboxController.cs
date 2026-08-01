using System;
using Jam.Core.Save;
using NodeCanvas.Framework;
using NodeCanvas.StateMachines;
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
        private Font _font;
        private Text _phaseText;
        private Text _speakerText;
        private Text _contentText;
        private Text _statusText;
        private RectTransform _actionsRoot;
        private PhotoWhiteboxPhase _phase;
        private PhotoChoice _choice;
        private int _introIndex;
        private int _inspectedMask;
        private int _truth;
        private int _reach;
        private PhotoCharacterSaveData _saveData = PhotoCheckpointAdapter.CreateNew();

        public bool CanSave => isActiveAndEnabled;
        public string ModeName => "История фотографки";

        private void Awake()
        {
            _fsmOwner = GetComponent<FSMOwner>();
            _blackboard = GetComponent<Blackboard>();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildInterface();
        }

        private void Start()
        {
            RestoreOrBegin();
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
                        SetStatus($"Продолжение: {checkpoint.checkpointId}");
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
                    RenderIntro();
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

        private void RenderIntro()
        {
            SetPhase("ПРОЛОГ • САНКТ-ПЕТЕРБУРГ");
            SetSpeaker(_introIndex == 1 ? "РЕДАКТОР" : _introIndex == 2 ? "ТЕЛЕФОН" : "ОНА");
            SetContent(_introLines[_introIndex]);
            SetStatus($"Экспозиция {_introIndex + 1} / {_introLines.Length}");
            ClearActions();
            CreateActionButton(_introIndex + 1 < _introLines.Length ? "ДАЛЕЕ" : "ВЫЙТИ К ПОДЪЕЗДУ", AdvanceIntro);
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
            SetPhase("ИССЛЕДОВАНИЕ • ДВОР И ПОЧТОВЫЕ ЯЩИКИ");
            SetSpeaker("WHITE-BOX СЦЕНА");
            SetContent(
                "Серый подъезд. У стены — почтовые ящики. Рядом лежит собранный чемодан. " +
                "Телефон продолжает вибрировать. Осмотрите три детали, чтобы разблокировать камеру.");
            SetStatus($"Осмотрено: {CountInspected()} / 3");
            ClearActions();
            CreateInspectionButton(0b001, "ТЕЛЕФОН • сообщение редактора", "Агентство уезжает. Зарплаты за последний месяц может не быть.");
            CreateInspectionButton(0b010, "ЧЕМОДАН • билет через Дубай", "Маршрут заканчивается словом «Бали». Дальше — пустое место.");
            CreateInspectionButton(0b100, "ПОЧТОВЫЙ ЯЩИК • движение внутри", "Из щели торчит военная повестка. На холодном металле садится бабочка.");

            if (_inspectedMask == RequiredInspectionMask)
            {
                CreateActionButton("ДОСТАТЬ КАМЕРУ", () => EnterPhase(PhotoWhiteboxPhase.Camera, true), true, AccentColor);
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
            SetPhase("КАМЕРА • ВЫБОР КАДРА");
            SetSpeaker("ВИДОИСКАТЕЛЬ");
            SetContent(_choice switch
            {
                PhotoChoice.Summons => "В рамке — край почтового ящика и торчащая повестка. Это честно, но небезопасно.",
                PhotoChoice.Butterfly => "В рамке — бабочка на металле. Красиво, безопасно и почти ничего не говорит о происходящем.",
                _ => "Обе цели находятся в одной композиции. Выберите, на чём сфокусировать кадр."
            });
            SetStatus(_choice == PhotoChoice.None ? "Цель не выбрана" : $"Фокус: {_choice}");
            ClearActions();
            CreateActionButton("[ ПОВЕСТКА ] • Truth", () => SelectTarget(PhotoChoice.Summons), true,
                _choice == PhotoChoice.Summons ? SelectedColor : ButtonColor);
            CreateActionButton("[ БАБОЧКА ] • Reach", () => SelectTarget(PhotoChoice.Butterfly), true,
                _choice == PhotoChoice.Butterfly ? SelectedColor : ButtonColor);
            CreateActionButton("СПУСК ЗАТВОРА", CapturePhoto, _choice != PhotoChoice.None, AccentColor);
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
            SetPhase("FORBIDGRAM • ПУБЛИКАЦИЯ");
            SetSpeaker(_choice == PhotoChoice.Summons ? "ЧЕСТНЫЙ КАДР" : "БЕЗОПАСНЫЙ КАДР");
            SetContent(_choice == PhotoChoice.Summons
                ? "Публикацию замечают быстро. Вместе с поддержкой приходят вопросы и страх удалить снимок."
                : "Лайки растут быстрее обычного. Никто не спрашивает, что находилось в нескольких сантиметрах от бабочки.");
            SetStatus($"Truth +{_truth}   •   Reach +{_reach}   •   Платёж получен");
            ClearActions();
            CreateActionButton("ПОСМОТРЕТЬ НА СВОЙ ВЫБОР", () => EnterPhase(PhotoWhiteboxPhase.ReflectionDialogue, false));
        }

        private void RenderReflection()
        {
            SetPhase("ОТРАЖЕНИЕ");
            SetSpeaker("ОНА");
            SetContent(_choice == PhotoChoice.Summons
                ? "Я хотя бы не сделала вид, что ничего не происходит. Теперь надо решить, сколько правды я смогу увезти с собой."
                : "Красивый кадр снова сработал. Только почему он ощущается ещё одной вещью, которую я оставляю здесь?"
            );
            SetStatus($"Сохранённый выбор: {_choice} • Truth {_truth} • Reach {_reach}");
            ClearActions();
            CreateActionButton("ЕХАТЬ В АЭРОПОРТ", () => EnterPhase(PhotoWhiteboxPhase.Arrival, true), true, AccentColor);
        }

        private void RenderArrival()
        {
            SetPhase("ДУБАЙ • ТРАНЗИТНАЯ ГОСТИНИЦА");
            SetSpeaker("WHITE-BOX ФИНАЛ ПРОЛОГА");
            SetContent(
                "Дверь гостиничного номера закрывается. На экране телефона остаётся опубликованный кадр. " +
                "Маршрут продолжается на Бали — уже в следующем акте.");
            SetStatus("Checkpoint photo.arrival сохранён");
            ClearActions();
            CreateActionButton("ЗАВЕРШИТЬ ПРОЛОГ", CompletePrologue, true, AccentColor);
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
            message = $"Сохранено: {checkpointId}";
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

            _phaseText = CreateLabel("Phase", panel.rectTransform, string.Empty, 22, FontStyle.Bold, AccentColor, 38f);
            _speakerText = CreateLabel("Speaker", panel.rectTransform, string.Empty, 18, FontStyle.Bold, MutedTextColor, 30f);

            var stage = CreateImage("Stage", panel.rectTransform, StageColor);
            stage.gameObject.AddComponent<LayoutElement>().preferredHeight = 300f;
            _contentText = CreateText("Content", stage.rectTransform, string.Empty, 28, FontStyle.Normal, TextColor);
            Stretch(_contentText.rectTransform, new Vector2(42f, 30f), new Vector2(-42f, -30f));

            _statusText = CreateLabel("Status", panel.rectTransform, string.Empty, 17, FontStyle.Normal, MutedTextColor, 34f);

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

            CreateLabel("Footer", panel.rectTransform, "WHITE-BOX • PHOTO / CHARACTER 3", 14, FontStyle.Normal, MutedTextColor, 24f);
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
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

            var labelText = CreateText("Label", buttonObject.GetComponent<RectTransform>(), label, 18, FontStyle.Bold, TextColor);
            Stretch(labelText.rectTransform, new Vector2(20f, 0f), new Vector2(-20f, 0f));
            return button;
        }

        private Text CreateLabel(string name, RectTransform parent, string value, int size, FontStyle style, Color color, float height)
        {
            var text = CreateText(name, parent, value, size, style, color);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return text;
        }

        private Text CreateText(string name, RectTransform parent, string value, int size, FontStyle style, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
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
    }
}
