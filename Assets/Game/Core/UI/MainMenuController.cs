using System.IO;
using System.Collections;
using Jam.Core.Localization;
using Jam.Core.Save;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

namespace Jam.Core.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string newGameScene = "CharacterSelect";

        [Header("DarkUI vendor styling")]
        [SerializeField] private Sprite darkButtonSprite;
        [SerializeField] private Sprite darkDividerSprite;
        [SerializeField] private Sprite playIcon;
        [SerializeField] private Sprite continueIcon;
        [SerializeField] private Sprite languageIcon;
        [SerializeField] private Sprite quitIcon;

        private static readonly Color BackgroundColor = new(0.035f, 0.045f, 0.065f, 1f);
        private static readonly Color PanelColor = new(0.075f, 0.09f, 0.12f, 0.97f);
        private static readonly Color AccentColor = new(0.91f, 0.55f, 0.24f, 1f);
        private static readonly Color ButtonColor = new(0.13f, 0.16f, 0.21f, 1f);
        private static readonly Color ButtonHoverColor = new(0.20f, 0.24f, 0.31f, 1f);
        private static readonly Color TextColor = new(0.94f, 0.93f, 0.89f, 1f);
        private static readonly Color MutedTextColor = new(0.60f, 0.64f, 0.70f, 1f);

        private Button _newGameButton;
        private Button _continueButton;
        private Button _languageButton;
        private Button _quitButton;
        private TMP_Text _statusText;
        private bool _isLoading;

        private void Awake()
        {
            EnsureEventSystem();
            BuildDarkInterface();
            RefreshState();
        }

        private void OnEnable()
        {
            Loc.LocaleChanged += RefreshState;
            if (_continueButton != null)
            {
                RefreshState();
            }
        }

        private void OnDisable()
        {
            Loc.LocaleChanged -= RefreshState;
        }

        private IEnumerator Start()
        {
            yield return LocalizationSettings.InitializationOperation;
            yield return null;
            RefreshState();
        }

        public void StartNewGame()
        {
            if (_isLoading)
            {
                return;
            }

            var sceneName = ResolveNewGameScene();
            if (string.IsNullOrEmpty(sceneName))
            {
                SetStatus(Loc.Get(LocalizationTables.Common, "ui.main.error.no_start", "Не найдена сцена начала игры."));
                Debug.LogError("Main menu could not find CharacterSelect in Build Settings.");
                return;
            }

            GameSaveService.StartNewGame(sceneName);
            LoadScene(sceneName);
        }

        public void ContinueGame()
        {
            if (_isLoading)
            {
                return;
            }

            if (!GameSaveService.TryGetContinueScene(out var sceneName) || !IsSceneInBuild(sceneName))
            {
                SetStatus(Loc.Get(LocalizationTables.Common, "ui.main.error.invalid_save", "Сохранение не найдено или устарело."));
                GameSaveService.Clear();
                RefreshState();
                return;
            }

            LoadScene(sceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LoadScene(string sceneName)
        {
            _isLoading = true;
            SetButtonsInteractable(false);
            SetStatus(Loc.Get(LocalizationTables.Common, "ui.main.loading", "Загрузка…"));
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }

        private string ResolveNewGameScene()
        {
            return IsSceneInBuild(newGameScene) ? newGameScene : null;
        }

        private static bool IsSceneInBuild(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            for (var index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(index);
                if (Path.GetFileNameWithoutExtension(path) == sceneName)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshState()
        {
            var canContinue = GameSaveService.TryGetContinueScene(out var sceneName) && IsSceneInBuild(sceneName);

            _newGameButton.interactable = true;
            _continueButton.interactable = canContinue;
            _quitButton.interactable = true;

            SetStatus(canContinue
                ? Loc.Get(LocalizationTables.Common, "ui.main.last_scene", "Последняя точка: {0}", sceneName)
                : Loc.Get(LocalizationTables.Common, "ui.main.no_save", "Сохранение не найдено"));
        }

        public void ToggleLanguage()
        {
            Loc.ToggleRussianEnglish();
        }

        private void SetButtonsInteractable(bool value)
        {
            _newGameButton.interactable = value;
            _continueButton.interactable = value;
            _languageButton.interactable = value;
            _quitButton.interactable = value;
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
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

            var inputModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private void BuildDarkInterface()
        {
            var canvasObject = new GameObject(
                "MainMenuCanvas",
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
            SetAnchoredRect(topAccent.rectTransform, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 8f));

            var panel = CreateImage("MenuPanel", background.rectTransform, PanelColor);
            SetAnchoredRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1520f, 760f));

            var brandPanel = CreateImage("BrandPanel", panel.rectTransform, new Color(0.045f, 0.055f, 0.075f, 1f));
            Stretch(brandPanel.rectTransform, Vector2.zero, new Vector2(-684f, 0f));

            var brandAccent = CreateImage("BrandAccent", brandPanel.rectTransform, new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.12f));
            SetAnchoredRect(brandAccent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(10f, 0f));

            var title = CreateTextObject("Title", brandPanel.rectTransform, "REFLECTION / MOMENTUM", 48, FontStyles.Bold, TextColor);
            LocalizedTextBinding.Attach(title, LocalizationTables.Common, "ui.main.title", "REFLECTION / MOMENTUM");
            title.alignment = TextAlignmentOptions.BottomLeft;
            SetAnchoredRect(title.rectTransform, new Vector2(0f, 0.56f), new Vector2(1f, 0.84f), Vector2.zero, Vector2.zero);
            title.rectTransform.offsetMin = new Vector2(82f, 0f);
            title.rectTransform.offsetMax = new Vector2(-72f, 0f);

            var subtitle = CreateTextObject("Subtitle", brandPanel.rectTransform, "REFLECTION + MOMENTUM", 20, FontStyles.Bold, AccentColor);
            LocalizedTextBinding.Attach(subtitle, LocalizationTables.Common, "ui.main.subtitle", "REFLECTION + MOMENTUM");
            subtitle.alignment = TextAlignmentOptions.TopLeft;
            SetAnchoredRect(subtitle.rectTransform, new Vector2(0f, 0.43f), new Vector2(1f, 0.51f), Vector2.zero, Vector2.zero);
            subtitle.rectTransform.offsetMin = new Vector2(84f, 0f);
            subtitle.rectTransform.offsetMax = new Vector2(-72f, 0f);

            var divider = CreateImage("Divider", brandPanel.rectTransform, new Color(1f, 1f, 1f, 0.22f), darkDividerSprite);
            SetAnchoredRect(divider.rectTransform, new Vector2(0f, 0.39f), new Vector2(1f, 0.39f), Vector2.zero, new Vector2(0f, 2f));
            divider.rectTransform.offsetMin = new Vector2(82f, 0f);
            divider.rectTransform.offsetMax = new Vector2(-82f, 2f);

            var footer = CreateTextObject("Footer", brandPanel.rectTransform, "THREE STORIES / ONE MOMENT", 16, FontStyles.Normal, MutedTextColor);
            LocalizedTextBinding.Attach(footer, LocalizationTables.Common, "ui.main.footer", "THREE STORIES / ONE MOMENT");
            footer.alignment = TextAlignmentOptions.TopLeft;
            SetAnchoredRect(footer.rectTransform, new Vector2(0f, 0.19f), new Vector2(1f, 0.39f), Vector2.zero, Vector2.zero);
            footer.rectTransform.offsetMin = new Vector2(84f, 0f);
            footer.rectTransform.offsetMax = new Vector2(-72f, 0f);

            var menuPanel = CreateImage("ActionsPanel", panel.rectTransform, new Color(0.09f, 0.105f, 0.135f, 1f));
            Stretch(menuPanel.rectTransform, new Vector2(836f, 0f), Vector2.zero);

            var actionsObject = new GameObject("Actions", typeof(RectTransform), typeof(VerticalLayoutGroup));
            actionsObject.transform.SetParent(menuPanel.rectTransform, false);
            var actionsRect = actionsObject.GetComponent<RectTransform>();
            SetAnchoredRect(actionsRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 470f));
            actionsRect.offsetMin = new Vector2(66f, -235f);
            actionsRect.offsetMax = new Vector2(-66f, 235f);

            var actionsLayout = actionsObject.GetComponent<VerticalLayoutGroup>();
            actionsLayout.spacing = 16f;
            actionsLayout.childAlignment = TextAnchor.MiddleCenter;
            actionsLayout.childControlWidth = true;
            actionsLayout.childControlHeight = false;
            actionsLayout.childForceExpandWidth = true;
            actionsLayout.childForceExpandHeight = false;

            _newGameButton = CreateButton("NewGameButton", actionsRect, "NEW GAME", StartNewGame, "ui.main.new_game", playIcon);
            _continueButton = CreateButton("ContinueButton", actionsRect, "CONTINUE", ContinueGame, "ui.main.continue", continueIcon);
            _languageButton = CreateButton("LanguageButton", actionsRect, "LANGUAGE", ToggleLanguage, "ui.main.language", languageIcon);
            _quitButton = CreateButton("QuitButton", actionsRect, "QUIT", QuitGame, "ui.main.quit", quitIcon);

            CreateSpacer(actionsRect, 14f);
            _statusText = CreateLabel("SaveStatus", actionsRect, string.Empty, 17, FontStyles.Normal, MutedTextColor, 30f);
            _newGameButton.Select();
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject(
                "MainMenuCanvas",
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
            SetAnchoredRect(accent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 8f));

            var panel = CreateImage("MenuPanel", background.rectTransform, PanelColor);
            SetAnchoredRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1520f, 760f));

            var brandPanel = CreateImage("BrandPanel", panel.rectTransform, new Color(0.045f, 0.055f, 0.075f, 1f));
            SetAnchoredRect(brandPanel.rectTransform, new Vector2(0f, 0f), new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero);
            brandPanel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            brandPanel.rectTransform.offsetMin = Vector2.zero;
            brandPanel.rectTransform.offsetMax = Vector2.zero;

            var brandAccent = CreateImage("BrandAccent", brandPanel.rectTransform, new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.10f));
            SetAnchoredRect(brandAccent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(10f, 0f));

            var title = CreateTextObject("Title", brandPanel.rectTransform, "ОТРАЖЕНИЕ / ИМПУЛЬС", 54, FontStyles.Bold, TextColor);
            LocalizedTextBinding.Attach(title, LocalizationTables.Common, "ui.main.title", "ОТРАЖЕНИЕ / ИМПУЛЬС");
            title.alignment = TextAlignmentOptions.BottomLeft;
            SetAnchoredRect(title.rectTransform, new Vector2(0f, 0.54f), new Vector2(1f, 0.82f), Vector2.zero, Vector2.zero);
            title.rectTransform.offsetMin = new Vector2(82f, 0f);
            title.rectTransform.offsetMax = new Vector2(-72f, 0f);

            var subtitle = CreateTextObject("Subtitle", brandPanel.rectTransform, "REFLECTION + MOMENTUM", 20, FontStyles.Bold, AccentColor);
            LocalizedTextBinding.Attach(subtitle, LocalizationTables.Common, "ui.main.subtitle", "REFLECTION + MOMENTUM");
            subtitle.alignment = TextAlignmentOptions.TopLeft;
            SetAnchoredRect(subtitle.rectTransform, new Vector2(0f, 0.46f), new Vector2(1f, 0.56f), Vector2.zero, Vector2.zero);
            subtitle.rectTransform.offsetMin = new Vector2(84f, 0f);
            subtitle.rectTransform.offsetMax = new Vector2(-72f, 0f);

            var divider = CreateImage("Divider", brandPanel.rectTransform, new Color(1f, 1f, 1f, 0.22f), darkDividerSprite);
            SetAnchoredRect(divider.rectTransform, new Vector2(0f, 0.43f), new Vector2(1f, 0.43f), Vector2.zero, new Vector2(0f, 2f));
            divider.rectTransform.offsetMin = new Vector2(82f, 0f);
            divider.rectTransform.offsetMax = new Vector2(-82f, 2f);

            var footer = CreateTextObject("Footer", brandPanel.rectTransform, "ТРИ ИСТОРИИ • ОДИН МОМЕНТ", 16, FontStyles.Normal, MutedTextColor);
            LocalizedTextBinding.Attach(footer, LocalizationTables.Common, "ui.main.footer", "ТРИ ИСТОРИИ • ОДИН МОМЕНТ");
            footer.alignment = TextAlignmentOptions.TopLeft;
            SetAnchoredRect(footer.rectTransform, new Vector2(0f, 0.19f), new Vector2(1f, 0.39f), Vector2.zero, Vector2.zero);
            footer.rectTransform.offsetMin = new Vector2(84f, 0f);
            footer.rectTransform.offsetMax = new Vector2(-72f, 0f);

            var menuPanel = CreateImage("ActionsPanel", panel.rectTransform, new Color(0.09f, 0.105f, 0.135f, 1f));
            SetAnchoredRect(menuPanel.rectTransform, new Vector2(0.55f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
            menuPanel.rectTransform.offsetMin = Vector2.zero;
            menuPanel.rectTransform.offsetMax = Vector2.zero;

            var actions = new GameObject("Actions", typeof(RectTransform), typeof(VerticalLayoutGroup));
            actions.transform.SetParent(menuPanel.rectTransform, false);
            SetAnchoredRect(actions.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 470f));
            actions.GetComponent<RectTransform>().offsetMin = new Vector2(66f, -235f);
            actions.GetComponent<RectTransform>().offsetMax = new Vector2(-66f, 235f);

            var actionsLayout = actions.GetComponent<VerticalLayoutGroup>();
            actionsLayout.spacing = 16f;
            actionsLayout.childAlignment = TextAnchor.MiddleCenter;
            actionsLayout.childControlWidth = true;
            actionsLayout.childControlHeight = false;
            actionsLayout.childForceExpandWidth = true;
            actionsLayout.childForceExpandHeight = false;

            _newGameButton = CreateButton("NewGameButton", actions.GetComponent<RectTransform>(), "НАЧАТЬ НОВУЮ ИГРУ", StartNewGame, "ui.main.new_game", playIcon);
            _continueButton = CreateButton("ContinueButton", actions.GetComponent<RectTransform>(), "ПРОДОЛЖИТЬ", ContinueGame, "ui.main.continue", continueIcon);
            _languageButton = CreateButton("LanguageButton", actions.GetComponent<RectTransform>(), "ЯЗЫК: РУССКИЙ", ToggleLanguage, "ui.main.language", languageIcon);
            _quitButton = CreateButton("QuitButton", actions.GetComponent<RectTransform>(), "ВЫХОД", QuitGame, "ui.main.quit", quitIcon);

            CreateSpacer(actions.GetComponent<RectTransform>(), 14f);
            _statusText = CreateLabel("SaveStatus", actions.GetComponent<RectTransform>(), string.Empty, 17, FontStyles.Normal, MutedTextColor, 30f);

            _newGameButton.Select();
        }

        private Button CreateButton(
            string name,
            RectTransform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            string localizationKey = null,
            Sprite iconSprite = null)
        {
            var buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));

            buttonObject.transform.SetParent(parent, false);

            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 68f;

            var image = buttonObject.GetComponent<Image>();
            image.color = ButtonColor;
            image.sprite = darkButtonSprite;

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

            var accentStrip = CreateImage("Accent", buttonObject.GetComponent<RectTransform>(), AccentColor);
            SetAnchoredRect(accentStrip.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(5f, 0f));

            if (iconSprite != null)
            {
                var icon = CreateImage("Icon", buttonObject.GetComponent<RectTransform>(), TextColor, iconSprite);
                icon.preserveAspect = true;
                SetAnchoredRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(28f, 28f));
            }

            var text = CreateTextObject("Label", buttonObject.GetComponent<RectTransform>(), label, 21, FontStyles.Bold, TextColor);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            if (!string.IsNullOrWhiteSpace(localizationKey))
            {
                LocalizedTextBinding.Attach(text, LocalizationTables.Common, localizationKey, label);
            }
            Stretch(text.rectTransform, new Vector2(76f, 0f), new Vector2(-20f, 0f));

            return button;
        }

        private TMP_Text CreateLabel(
            string name,
            RectTransform parent,
            string value,
            int fontSize,
            FontStyles fontStyle,
            Color color,
            float preferredHeight,
            string localizationKey = null)
        {
            var text = CreateTextObject(name, parent, value, fontSize, fontStyle, color);
            if (!string.IsNullOrWhiteSpace(localizationKey))
            {
                LocalizedTextBinding.Attach(text, LocalizationTables.Common, localizationKey, value);
            }
            var layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
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

        private static Image CreateImage(string name, RectTransform parent, Color color, Sprite sprite = null)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            var image = imageObject.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            return image;
        }

        private static void CreateSpacer(RectTransform parent, float height)
        {
            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(parent, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = height;
        }

        private static void Stretch(RectTransform rect, Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin ?? Vector2.zero;
            rect.offsetMax = offsetMax ?? Vector2.zero;
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
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
