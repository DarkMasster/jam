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
            BuildInterface();
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
            SetAnchoredRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 690f));

            var panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(54, 54, 48, 42);
            panelLayout.spacing = 18f;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = false;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            CreateLabel("Title", panel.rectTransform, "ОТРАЖЕНИЕ / ИМПУЛЬС", 42, FontStyles.Bold, TextColor, 72f, "ui.main.title");
            CreateLabel("Subtitle", panel.rectTransform, "REFLECTION + MOMENTUM", 19, FontStyles.Normal, AccentColor, 34f, "ui.main.subtitle");
            CreateSpacer(panel.rectTransform, 30f);

            _newGameButton = CreateButton("NewGameButton", panel.rectTransform, "НАЧАТЬ НОВУЮ ИГРУ", StartNewGame, "ui.main.new_game");
            _continueButton = CreateButton("ContinueButton", panel.rectTransform, "ПРОДОЛЖИТЬ", ContinueGame, "ui.main.continue");
            _languageButton = CreateButton("LanguageButton", panel.rectTransform, "ЯЗЫК: РУССКИЙ", ToggleLanguage, "ui.main.language");
            _quitButton = CreateButton("QuitButton", panel.rectTransform, "ВЫХОД", QuitGame, "ui.main.quit");

            CreateSpacer(panel.rectTransform, 22f);
            _statusText = CreateLabel("SaveStatus", panel.rectTransform, string.Empty, 18, FontStyles.Normal, MutedTextColor, 30f);
            CreateSpacer(panel.rectTransform, 12f);
            CreateLabel("Footer", panel.rectTransform, "ТРИ ИСТОРИИ • ОДИН МОМЕНТ", 15, FontStyles.Normal, MutedTextColor, 24f, "ui.main.footer");

            _newGameButton.Select();
        }

        private Button CreateButton(
            string name,
            RectTransform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            string localizationKey = null)
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

            var text = CreateTextObject("Label", buttonObject.GetComponent<RectTransform>(), label, 21, FontStyles.Bold, TextColor);
            if (!string.IsNullOrWhiteSpace(localizationKey))
            {
                LocalizedTextBinding.Attach(text, LocalizationTables.Common, localizationKey, label);
            }
            Stretch(text.rectTransform, new Vector2(20f, 0f), new Vector2(-20f, 0f));

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
