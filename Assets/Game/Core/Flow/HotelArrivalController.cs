using System.Collections.Generic;
using Jam.Core.Localization;
using Jam.Core.Save;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Jam.Core.Flow
{
    /// <summary>
    /// Общая параметризованная сцена прибытия. Она не знает механик эпизода: весь
    /// текст и цифры приходят одним <see cref="EpisodeResult"/> от самого эпизода.
    /// </summary>
    public sealed class HotelArrivalController : MonoBehaviour
    {
        private static readonly Color BackgroundColor = new(0.035f, 0.045f, 0.065f, 1f);
        private static readonly Color PanelColor = new(0.075f, 0.09f, 0.12f, 0.98f);
        private static readonly Color AccentColor = new(0.91f, 0.55f, 0.24f, 1f);
        private static readonly Color ButtonColor = new(0.13f, 0.16f, 0.21f, 1f);
        private static readonly Color ButtonHoverColor = new(0.20f, 0.24f, 0.31f, 1f);
        private static readonly Color TextColor = new(0.94f, 0.93f, 0.89f, 1f);
        private static readonly Color MutedTextColor = new(0.60f, 0.64f, 0.70f, 1f);

        private readonly List<TMP_Text> _resultLabels = new();

        private EpisodeResult _result;
        private TMP_Text _titleText;
        private TMP_Text _routeText;
        private TMP_Text _bodyText;
        private TMP_Text _statusText;
        private RectTransform _resultPanel;
        private Button _continueButton;
        private TMP_Text _continueLabel;
        private Button _mainMenuButton;
        private TMP_Text _mainMenuLabel;
        private bool _isLeaving;

        private void Awake()
        {
            _result = GameFlowService.PendingResult;
            EnsureEventSystem();
            BuildInterface();
            RefreshContent();
        }

        private void OnEnable()
        {
            Loc.LocaleChanged += RefreshContent;
        }

        private void OnDisable()
        {
            Loc.LocaleChanged -= RefreshContent;
        }

        /// <summary>Закрывает дверь номера и возвращает игрока в выбор героя.</summary>
        public void Continue()
        {
            if (!BeginLeaving())
            {
                return;
            }

            GameFlowService.FinishArrival(_result);
        }

        /// <summary>
        /// Финализирует прибытие и открывает Main. Сохранённой точкой продолжения
        /// остаётся CharacterSelect, а не runtime-only экран результата.
        /// </summary>
        public void ReturnToMainMenu()
        {
            if (!BeginLeaving())
            {
                return;
            }

            GameFlowService.FinishArrivalToMainMenu(_result);
        }

        private void RefreshContent()
        {
            if (_titleText == null)
            {
                return;
            }

            var characterId = _result?.characterId ?? GameSaveService.ActiveCharacter;

            _titleText.text = Loc.Get(LocalizationTables.Common, "ui.arrival.title", "ТРАНЗИТНАЯ ГОСТИНИЦА");
            _routeText.text = ResolveRoute(characterId);
            _bodyText.text = ResolveBody(characterId);
            _statusText.text = Loc.Get(
                LocalizationTables.Common,
                "ui.arrival.status",
                "Прогресс сохранён. Оставшиеся истории доступны в любом порядке.");
            _continueLabel.text = Loc.Get(LocalizationTables.Common, "ui.arrival.continue", "ВЕРНУТЬСЯ К ВЫБОРУ ИСТОРИИ");
            _mainMenuLabel.text = Loc.Get(LocalizationTables.Common, "ui.hud.exit", "ВЫЙТИ В ГЛАВНОЕ МЕНЮ");

            RefreshResultLines();
        }

        private void RefreshResultLines()
        {
            var lines = _result?.lines;
            var count = lines?.Count ?? 0;

            for (var index = 0; index < _resultLabels.Count; index++)
            {
                var visible = index < count;
                _resultLabels[index].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                var line = lines[index];
                var label = string.IsNullOrWhiteSpace(line.key)
                    ? line.fallback
                    : Loc.Get(
                        string.IsNullOrWhiteSpace(line.table) ? LocalizationTables.Common : line.table,
                        line.key,
                        line.fallback);
                _resultLabels[index].text = $"{label}   {line.value}";
            }

            if (_resultPanel != null)
            {
                _resultPanel.gameObject.SetActive(count > 0);
            }
        }

        private string ResolveRoute(CharacterId characterId)
        {
            return characterId switch
            {
                CharacterId.Drive => Loc.Get(LocalizationTables.Common, "ui.character.drive.route", "МОСКВА → ТБИЛИСИ"),
                CharacterId.Office => Loc.Get(LocalizationTables.Common, "ui.character.office.route", "ЕКАТЕРИНБУРГ → АЛМАТЫ"),
                CharacterId.Photo => Loc.Get(LocalizationTables.Common, "ui.character.photo.route", "САНКТ-ПЕТЕРБУРГ → БАЛИ"),
                _ => Loc.Get(LocalizationTables.Common, "ui.arrival.route.unknown", "МАРШРУТ ПРОДОЛЖАЕТСЯ")
            };
        }

        private string ResolveBody(CharacterId characterId)
        {
            if (_result != null && !string.IsNullOrWhiteSpace(_result.arrivalKey))
            {
                return Loc.Get(
                    string.IsNullOrWhiteSpace(_result.arrivalTable) ? LocalizationTables.Common : _result.arrivalTable,
                    _result.arrivalKey,
                    _result.arrivalFallback);
            }

            return characterId switch
            {
                CharacterId.Drive => Loc.Get(
                    LocalizationTables.Common,
                    "ui.arrival.drive",
                    "Дверь номера закрывается. Семья наконец спит, а очередь на границе осталась позади."),
                CharacterId.Office => Loc.Get(
                    LocalizationTables.Common,
                    "ui.arrival.office",
                    "Дверь номера закрывается. Ноутбук и кружка лежат на столе, офис остался во сне."),
                CharacterId.Photo => Loc.Get(
                    LocalizationTables.Common,
                    "ui.arrival.photo",
                    "Дверь номера закрывается. На экране телефона остаётся опубликованный кадр."),
                _ => Loc.Get(
                    LocalizationTables.Common,
                    "ui.arrival.default",
                    "Дверь номера закрывается. Первая относительная тишина за весь день.")
            };
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));

            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject(
                "HotelArrivalCanvas",
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

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            var background = CreateImage("Background", canvasRect, BackgroundColor);
            Stretch(background.rectTransform);

            var accent = CreateImage("Accent", background.rectTransform, AccentColor);
            SetAnchoredRect(
                accent.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                Vector2.zero,
                new Vector2(0f, 8f));

            var panel = CreateImage("ArrivalPanel", background.rectTransform, PanelColor);
            SetAnchoredRect(
                panel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1180f, 840f));

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(70, 70, 46, 38);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _titleText = CreateLabel("Title", panel.rectTransform, string.Empty, 42, FontStyles.Bold, TextColor, 64f);
            _routeText = CreateLabel("Route", panel.rectTransform, string.Empty, 20, FontStyles.Bold, AccentColor, 32f);
            CreateSpacer(panel.rectTransform, 14f);
            _bodyText = CreateLabel("Body", panel.rectTransform, string.Empty, 22, FontStyles.Normal, TextColor, 150f);
            CreateSpacer(panel.rectTransform, 10f);

            _resultPanel = CreateImage("ResultPanel", panel.rectTransform, new Color(0.05f, 0.06f, 0.08f, 0.9f)).rectTransform;
            _resultPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 190f;
            var resultLayout = _resultPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            resultLayout.padding = new RectOffset(28, 28, 16, 16);
            resultLayout.spacing = 4f;
            resultLayout.childAlignment = TextAnchor.UpperLeft;
            resultLayout.childControlWidth = true;
            resultLayout.childControlHeight = true;
            resultLayout.childForceExpandWidth = true;
            resultLayout.childForceExpandHeight = false;

            for (var index = 0; index < 6; index++)
            {
                var label = CreateLabel(
                    $"ResultLine{index}",
                    _resultPanel,
                    string.Empty,
                    19,
                    FontStyles.Normal,
                    MutedTextColor,
                    26f);
                label.alignment = TextAlignmentOptions.Left;
                label.gameObject.SetActive(false);
                _resultLabels.Add(label);
            }

            CreateSpacer(panel.rectTransform, 12f);
            _continueButton = CreateButton("ContinueButton", panel.rectTransform, string.Empty, Continue, 64f);
            _continueLabel = _continueButton.transform.Find("Label").GetComponent<TMP_Text>();
            _mainMenuButton = CreateButton("MainMenuButton", panel.rectTransform, string.Empty, ReturnToMainMenu, 54f);
            _mainMenuLabel = _mainMenuButton.transform.Find("Label").GetComponent<TMP_Text>();
            _statusText = CreateLabel("Status", panel.rectTransform, string.Empty, 16, FontStyles.Normal, MutedTextColor, 38f);

            _continueButton.Select();
        }

        private bool BeginLeaving()
        {
            if (_isLeaving)
            {
                return false;
            }

            _isLeaving = true;
            if (_continueButton != null)
            {
                _continueButton.interactable = false;
            }
            if (_mainMenuButton != null)
            {
                _mainMenuButton.interactable = false;
            }
            return true;
        }

        private Button CreateButton(
            string name,
            RectTransform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            float height)
        {
            var buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));

            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<LayoutElement>().preferredHeight = height;

            var image = buttonObject.GetComponent<Image>();
            image.color = ButtonColor;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            var colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = ButtonHoverColor;
            colors.selectedColor = ButtonHoverColor;
            colors.pressedColor = AccentColor;
            colors.disabledColor = new Color(ButtonColor.r, ButtonColor.g, ButtonColor.b, 0.42f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(action);

            var text = CreateTextObject("Label", buttonObject.GetComponent<RectTransform>(), label, 19, FontStyles.Bold, TextColor);
            Stretch(text.rectTransform, new Vector2(24f, 0f), new Vector2(-24f, 0f));
            return button;
        }

        private TMP_Text CreateLabel(
            string name,
            RectTransform parent,
            string value,
            int fontSize,
            FontStyles fontStyle,
            Color color,
            float preferredHeight)
        {
            var text = CreateTextObject(name, parent, value, fontSize, fontStyle, color);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            return text;
        }

        private TMP_Text CreateTextObject(
            string name,
            RectTransform parent,
            string value,
            int fontSize,
            FontStyles fontStyle,
            Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
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

        private static void CreateSpacer(RectTransform parent, float height)
        {
            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(parent, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = height;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetAnchoredRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, anchorMin.y > 0.9f ? 1f : 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
