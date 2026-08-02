using Jam.Core.Audio;
using Jam.Core.Localization;
using Jam.Core.Save;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jam.Core.UI
{
    public sealed class GlobalHudController : MonoBehaviour
    {
        private const string MainScene = "Main";
        private const string CharacterSelectScene = "CharacterSelect";

        private static readonly Color OverlayColor = new(0.02f, 0.025f, 0.04f, 0.86f);
        private static readonly Color PanelColor = new(0.075f, 0.09f, 0.12f, 0.99f);
        private static readonly Color ButtonColor = new(0.13f, 0.16f, 0.21f, 1f);
        private static readonly Color ButtonHoverColor = new(0.20f, 0.24f, 0.31f, 1f);
        private static readonly Color AccentColor = new(0.91f, 0.55f, 0.24f, 1f);
        private static readonly Color TextColor = new(0.94f, 0.93f, 0.89f, 1f);
        private static readonly Color MutedTextColor = new(0.62f, 0.66f, 0.72f, 1f);

        private Canvas _canvas;
        private GameObject _overlay;
        private Button _menuButton;
        private Button _saveButton;
        private TMP_Text _statusText;
        private bool _isGameplayScene;
        private bool _menuOpen;
        private bool _cutsceneActive;
        private float _previousTimeScale = 1f;
        private CursorLockMode _previousCursorLockMode;
        private bool _previousCursorVisible;

        private void Awake()
        {
            EnsureEventSystem();
            BuildInterface();
            RefreshForScene(SceneManager.GetActiveScene());
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            Loc.LocaleChanged += HandleLocaleChanged;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Loc.LocaleChanged -= HandleLocaleChanged;
            if (_menuOpen)
            {
                _menuOpen = false;
                RestoreGameplayState();
            }
            AudioService.Instance?.ClearContext(this);
        }

        private void Update()
        {
            if (!_isGameplayScene || _cutsceneActive || Keyboard.current?.escapeKey.wasPressedThisFrame != true)
            {
                return;
            }

            if (_menuOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }

        public void OpenMenu()
        {
            if (!_isGameplayScene || _menuOpen)
            {
                return;
            }

            _menuOpen = true;
            _previousTimeScale = Time.timeScale;
            _previousCursorLockMode = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            Time.timeScale = 0f;
            AudioService.Instance?.SetContext(this, AudioMixContext.Paused);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _menuButton.gameObject.SetActive(false);
            _overlay.SetActive(true);
            RefreshSaveState();
        }

        public void CloseMenu()
        {
            if (!_menuOpen)
            {
                return;
            }

            _menuOpen = false;
            _overlay.SetActive(false);
            _menuButton.gameObject.SetActive(_isGameplayScene);
            AudioService.Instance?.ClearContext(this);
            RestoreGameplayState();
        }

        public void SaveGame()
        {
            var saved = GameModeSaveService.TrySaveActiveMode(out var message);
            _statusText.text = message;
            _statusText.color = saved ? AccentColor : MutedTextColor;
            RefreshSaveState(false);
        }

        public void ExitToMainMenu()
        {
            CloseMenu();
            SceneManager.LoadSceneAsync(MainScene, LoadSceneMode.Single);
        }

        public void SetCutsceneActive(bool active)
        {
            _cutsceneActive = active;
            if (active && _menuOpen)
            {
                CloseMenu();
            }

            _menuButton.gameObject.SetActive(_isGameplayScene && !_menuOpen && !_cutsceneActive);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_menuOpen)
            {
                CloseMenu();
            }

            RefreshForScene(scene);
        }

        private void HandleLocaleChanged()
        {
            if (_menuOpen)
            {
                RefreshSaveState();
            }
        }

        private void RefreshForScene(Scene scene)
        {
            _isGameplayScene = scene.IsValid()
                               && scene.isLoaded
                               && scene.name != MainScene
                               && scene.name != CharacterSelectScene;
            _canvas.enabled = _isGameplayScene;
            _menuButton.gameObject.SetActive(_isGameplayScene && !_menuOpen && !_cutsceneActive);
            _overlay.SetActive(_isGameplayScene && _menuOpen);
        }

        private void RefreshSaveState(bool resetStatus = true)
        {
            var canSave = GameModeSaveService.CanSaveActiveMode;
            _saveButton.interactable = canSave;
            if (resetStatus)
            {
                _statusText.color = MutedTextColor;
                _statusText.text = canSave
                    ? Loc.Get(LocalizationTables.Common, "ui.hud.save.available", "Сохранение запишет состояние текущего режима.")
                    : Loc.Get(LocalizationTables.Common, "ui.hud.save.unavailable", "Этот режим пока не поддерживает ручное сохранение.");
            }
        }

        private void RestoreGameplayState()
        {
            Time.timeScale = _previousTimeScale;
            Cursor.lockState = _previousCursorLockMode;
            Cursor.visible = _previousCursorVisible;
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject(
                "GlobalEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(transform, false);
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject(
                "GlobalHudCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            _menuButton = CreateButton("GlobalMenuButton", canvasRect, "МЕНЮ  [ESC]", OpenMenu, 18, "ui.hud.menu");
            SetAnchoredRect(
                _menuButton.GetComponent<RectTransform>(),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-105f, -48f),
                new Vector2(180f, 58f));

            var overlayImage = CreateImage("GlobalPauseOverlay", canvasRect, OverlayColor);
            Stretch(overlayImage.rectTransform);
            _overlay = overlayImage.gameObject;

            var panel = CreateImage("GlobalPausePanel", overlayImage.rectTransform, PanelColor);
            SetAnchoredRect(
                panel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(580f, 520f));

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(56, 56, 46, 42);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateLabel("PauseTitle", panel.rectTransform, "МЕНЮ", 40, FontStyles.Bold, TextColor, 64f, "ui.hud.pause.title");
            CreateLabel("PauseSubtitle", panel.rectTransform, "ИГРА ПРИОСТАНОВЛЕНА", 17, FontStyles.Normal, AccentColor, 30f, "ui.hud.pause.subtitle");
            CreateSpacer(panel.rectTransform, 18f);
            CreateButton("ResumeButton", panel.rectTransform, "ПРОДОЛЖИТЬ", CloseMenu, 21, "ui.hud.resume");
            _saveButton = CreateButton("SaveGameButton", panel.rectTransform, "СОХРАНИТЬ", SaveGame, 21, "ui.hud.save");
            CreateButton("ExitToMainButton", panel.rectTransform, "ВЫЙТИ В ГЛАВНОЕ МЕНЮ", ExitToMainMenu, 19, "ui.hud.exit");
            CreateSpacer(panel.rectTransform, 12f);
            _statusText = CreateLabel("GlobalSaveStatus", panel.rectTransform, string.Empty, 16, FontStyles.Normal, MutedTextColor, 46f);
            _overlay.SetActive(false);
        }

        private Button CreateButton(
            string name,
            RectTransform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            int fontSize,
            string localizationKey = null)
        {
            var buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<LayoutElement>().preferredHeight = 66f;

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
            button.colors = colors;
            button.onClick.AddListener(action);

            var text = CreateTextObject("Label", buttonObject.GetComponent<RectTransform>(), label, fontSize, FontStyles.Bold, TextColor);
            if (!string.IsNullOrWhiteSpace(localizationKey))
            {
                LocalizedTextBinding.Attach(text, LocalizationTables.Common, localizationKey, label);
            }
            Stretch(text.rectTransform, new Vector2(18f, 0f), new Vector2(-18f, 0f));
            return button;
        }

        private TMP_Text CreateLabel(
            string name,
            RectTransform parent,
            string value,
            int fontSize,
            FontStyles style,
            Color color,
            float preferredHeight,
            string localizationKey = null)
        {
            var text = CreateTextObject(name, parent, value, fontSize, style, color);
            if (!string.IsNullOrWhiteSpace(localizationKey))
            {
                LocalizedTextBinding.Attach(text, LocalizationTables.Common, localizationKey, value);
            }
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            return text;
        }

        private TMP_Text CreateTextObject(
            string name,
            RectTransform parent,
            string value,
            int fontSize,
            FontStyles style,
            Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
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
