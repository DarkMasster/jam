using System;
using System.IO;
using Jam.Core.Flow;
using Jam.Core.Localization;
using Jam.Core.Save;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jam.Core.UI
{
    public sealed class CharacterSelectController : MonoBehaviour
    {
        [SerializeField] private string driveScene = "Prologue_Drive";
        [SerializeField] private string driveBrowserGamePath = "Web/Drive/hasta-la-vista-jam.html";
        [SerializeField] private string officeScene = "Prologue_Office";
        [SerializeField] private string photoScene = "Prologue_Photo";
        [SerializeField] private string mainMenuScene = "Main";
        [SerializeField] private string temporaryGameplayScene = "SampleScene";
        [SerializeField] private Sprite darkButtonSprite;
        [SerializeField] private Sprite darkDividerSprite;
        [SerializeField] private CharacterSelectPortraitRenderer portraitRenderer;

        private static readonly Color BackgroundColor = new(0.035f, 0.045f, 0.065f, 1f);
        private static readonly Color PanelColor = new(0.075f, 0.09f, 0.12f, 0.98f);
        private static readonly Color AccentColor = new(0.91f, 0.55f, 0.24f, 1f);
        private static readonly Color ButtonColor = new(0.13f, 0.16f, 0.21f, 1f);
        private static readonly Color ButtonHoverColor = new(0.20f, 0.24f, 0.31f, 1f);
        private static readonly Color TextColor = new(0.94f, 0.93f, 0.89f, 1f);
        private static readonly Color MutedTextColor = new(0.60f, 0.64f, 0.70f, 1f);
        private static readonly Color CompletedColor = new(0.25f, 0.48f, 0.34f, 1f);

        private readonly Button[] _characterButtons = new Button[3];
        private readonly TMP_Text[] _characterLabels = new TMP_Text[3];

        private Button _finaleButton;
        private Button _backButton;
        private TMP_Text _progressText;
        private TMP_Text _statusText;
        private bool _isLoading;

        private void Awake()
        {
            if (!GameSaveService.HasSave)
            {
                GameSaveService.StartNewGame(gameObject.scene.name);
            }

            EnsureEventSystem();
            BuildInterface();
            RefreshProgress();
        }

        private void OnEnable()
        {
            Loc.LocaleChanged += RefreshProgress;
            if (_progressText != null)
            {
                RefreshProgress();
            }
        }

        private void OnDisable()
        {
            Loc.LocaleChanged -= RefreshProgress;
        }

        public void SelectDrive()
        {
            if (TryLaunchDriveBrowserGame())
            {
                return;
            }

            SelectCharacter(CharacterId.Drive, driveScene);
        }

        private bool TryLaunchDriveBrowserGame()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.LogWarning("The local Drive browser game cannot be opened from a WebGL player. Falling back to the Unity scene.");
            return false;
#else
            var fullPath = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, driveBrowserGamePath));
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"Drive browser game was not found at '{fullPath}'. Falling back to '{driveScene}'.");
                return false;
            }

            GameSaveService.SelectCharacter(CharacterId.Drive, gameObject.scene.name);
            Application.OpenURL(new Uri(fullPath).AbsoluteUri);
            SetStatus(Loc.Get(
                LocalizationTables.Common,
                "ui.character.status.drive_browser_opened",
                "Игра первого персонажа открыта в браузере. После закрытия вкладки вернитесь в это меню."));
            return true;
#endif
        }

        public void SelectOffice()
        {
            SelectCharacter(CharacterId.Office, officeScene);
        }

        public void SelectPhoto()
        {
            SelectCharacter(CharacterId.Photo, photoScene);
        }

        public void StartFinale()
        {
            if (_isLoading || !GameSaveService.EpilogueUnlocked)
            {
                return;
            }

            if (!EpilogueService.TryOpen())
            {
                SetStatus(Loc.Get(LocalizationTables.Common, "ui.character.error.no_epilogue", "Не удалось открыть эпилог в браузере."));
            }
        }

        public void ReturnToMainMenu()
        {
            if (_isLoading)
            {
                return;
            }

            GameSaveService.SetLastScene(gameObject.scene.name);
            LoadScene(mainMenuScene);
        }

        private void SelectCharacter(CharacterId characterId, string preferredScene)
        {
            if (_isLoading)
            {
                return;
            }

            var targetScene = ResolveGameplayScene(characterId, preferredScene);
            if (string.IsNullOrEmpty(targetScene))
            {
                SetStatus(Loc.Get(LocalizationTables.Common, "ui.character.error.no_scene", "Для линии {0} не найдена игровая сцена.", characterId));
                Debug.LogError($"No gameplay scene found for character '{characterId}'.");
                return;
            }

            GameSaveService.SelectCharacter(characterId, targetScene);
            LoadScene(targetScene);
        }

        private string ResolveGameplayScene(CharacterId characterId, string preferredScene)
        {
            if (GameSaveService.TryGetCharacterCheckpoint(characterId, out var checkpoint)
                && !checkpoint.completed
                && IsSceneInBuild(checkpoint.sceneName))
            {
                return checkpoint.sceneName;
            }

            if (IsSceneInBuild(preferredScene))
            {
                return preferredScene;
            }

            return IsSceneInBuild(temporaryGameplayScene) ? temporaryGameplayScene : null;
        }

        private void LoadScene(string sceneName)
        {
            _isLoading = true;
            SetButtonsInteractable(false);
            SetStatus(Loc.Get(LocalizationTables.Common, "ui.character.status.loading", "Сохранение… Загрузка…"));
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }

        private void RefreshProgress()
        {
            var completed = GameSaveService.CompletedCount;
            var activeCharacter = GameSaveService.ActiveCharacter;

            _progressText.text = Loc.Get(LocalizationTables.Common, "ui.character.progress", "ПРОЙДЕНО ИСТОРИЙ: {0} / 3", completed);
            UpdateCharacterButton(
                0,
                CharacterId.Drive,
                Loc.Get(LocalizationTables.Common, "ui.character.drive.route", "МОСКВА → ТБИЛИСИ"),
                Loc.Get(LocalizationTables.Common, "ui.character.drive.description", "РУКОВОДИТЕЛЬ • ДОРОГА"),
                activeCharacter);
            UpdateCharacterButton(
                1,
                CharacterId.Office,
                Loc.Get(LocalizationTables.Common, "ui.character.office.route", "ЕКАТЕРИНБУРГ → АЛМАТЫ"),
                Loc.Get(LocalizationTables.Common, "ui.character.office.description", "РАЗРАБОТЧИК • ОФИСНЫЙ КОШМАР"),
                activeCharacter);
            UpdateCharacterButton(
                2,
                CharacterId.Photo,
                Loc.Get(LocalizationTables.Common, "ui.character.photo.route", "САНКТ-ПЕТЕРБУРГ → БАЛИ"),
                Loc.Get(LocalizationTables.Common, "ui.character.photo.description", "ФОТОГРАФКА • ЧЕСТНЫЙ КАДР"),
                activeCharacter);

            _finaleButton.interactable = GameSaveService.EpilogueUnlocked;
            _finaleButton.transform.Find("Label").GetComponent<TMP_Text>().text = GameSaveService.EpilogueUnlocked
                ? Loc.Get(LocalizationTables.Common, "ui.character.epilogue.unlocked", "ЭПИЛОГ • РАЗБЛОКИРОВАН")
                : Loc.Get(LocalizationTables.Common, "ui.character.epilogue.locked", "ЭПИЛОГ • ЗАБЛОКИРОВАН");
            SetStatus(GameSaveService.EpilogueUnlocked
                ? Loc.Get(LocalizationTables.Common, "ui.character.status.epilogue_unlocked", "Линии второго и третьего персонажей завершены. Эпилог доступен.")
                : Loc.Get(LocalizationTables.Common, "ui.character.status.default", "Истории можно проходить в любом порядке. Прогресс сохраняется автоматически."));
        }

        private void UpdateCharacterButton(
            int index,
            CharacterId characterId,
            string route,
            string description,
            CharacterId activeCharacter)
        {
            var completed = GameSaveService.IsCharacterCompleted(characterId);
            var marker = completed
                ? "  " + Loc.Get(LocalizationTables.Common, "ui.character.completed", "✓ ПРОЙДЕНО")
                : activeCharacter == characterId
                    ? "  " + Loc.Get(LocalizationTables.Common, "ui.character.current", "• ТЕКУЩАЯ")
                    : string.Empty;

            _characterLabels[index].text = $"{route}{marker}\n{description}";
            var colors = _characterButtons[index].colors;
            colors.normalColor = completed ? CompletedColor : ButtonColor;
            colors.selectedColor = completed ? CompletedColor * 1.15f : ButtonHoverColor;
            colors.highlightedColor = completed ? CompletedColor * 1.15f : ButtonHoverColor;
            _characterButtons[index].colors = colors;
        }

        private void SetButtonsInteractable(bool value)
        {
            foreach (var button in _characterButtons)
            {
                button.interactable = value;
            }

            _finaleButton.interactable = value && GameSaveService.EpilogueUnlocked;
            _backButton.interactable = value;
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

            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private void BuildInterface()
        {
            var theme = DarkUiTheme.Load();
            if (darkButtonSprite == null && theme != null) darkButtonSprite = theme.Button;
            if (darkDividerSprite == null && theme != null) darkDividerSprite = theme.Divider;
            var canvasObject = new GameObject(
                "CharacterSelectCanvas",
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

            var panel = CreateImage("SelectionPanel", background.rectTransform, PanelColor);
            SetAnchoredRect(
                panel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(1420f, 920f));

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(70, 70, 38, 32);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateLabel("Title", panel.rectTransform, "ВЫБЕРИТЕ ИСТОРИЮ", 42, FontStyles.Bold, TextColor, 64f, "ui.character.title");
            CreateLabel("Subtitle", panel.rectTransform, "ТРИ МАРШРУТА • ОДИН ДЕНЬ", 18, FontStyles.Normal, AccentColor, 30f, "ui.character.subtitle");
            var divider = CreateImage("Divider", panel.rectTransform, new Color(1f, 1f, 1f, 0.22f));
            divider.sprite = darkDividerSprite;
            divider.gameObject.AddComponent<LayoutElement>().preferredHeight = 2f;
            CreateSpacer(panel.rectTransform, 8f);
            _progressText = CreateLabel("Progress", panel.rectTransform, string.Empty, 20, FontStyles.Bold, TextColor, 30f);
            CreateSpacer(panel.rectTransform, 8f);

            CreateCharacterButton(
                0,
                "DriveButton",
                panel.rectTransform,
                SelectDrive,
                new Color(0.16f, 0.23f, 0.34f, 1f));
            CreateCharacterButton(
                1,
                "OfficeButton",
                panel.rectTransform,
                SelectOffice,
                new Color(0.16f, 0.25f, 0.22f, 1f));
            CreateCharacterButton(
                2,
                "PhotoButton",
                panel.rectTransform,
                SelectPhoto,
                new Color(0.27f, 0.18f, 0.29f, 1f));

            CreateSpacer(panel.rectTransform, 10f);
            _finaleButton = CreateButton("EpilogueButton", panel.rectTransform, "ЭПИЛОГ • ЗАБЛОКИРОВАН", StartFinale, 64f);
            _backButton = CreateButton("BackButton", panel.rectTransform, "НАЗАД В ГЛАВНОЕ МЕНЮ", ReturnToMainMenu, 50f, "ui.character.back");
            _statusText = CreateLabel("Status", panel.rectTransform, string.Empty, 16, FontStyles.Normal, MutedTextColor, 38f);

            _characterButtons[0].Select();
        }

        private void CreateCharacterButton(
            int index,
            string name,
            RectTransform parent,
            UnityEngine.Events.UnityAction action,
            Color baseColor)
        {
            var button = CreateButton(name, parent, string.Empty, action, 160f);
            var colors = button.colors;
            colors.normalColor = baseColor;
            button.colors = colors;
            button.GetComponent<Image>().color = baseColor;
            _characterButtons[index] = button;
            _characterLabels[index] = button.transform.Find("Label").GetComponent<TMP_Text>();
            _characterLabels[index].alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(_characterLabels[index].rectTransform, new Vector2(190f, 12f), new Vector2(-32f, -12f));

            var portraitFrame = CreateImage("PortraitFrame", button.GetComponent<RectTransform>(), new Color(0.035f, 0.04f, 0.055f, 1f));
            SetAnchoredRect(portraitFrame.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(82f, 0f), new Vector2(132f, 132f));
            var portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(RawImage));
            portraitObject.transform.SetParent(portraitFrame.rectTransform, false);
            var portrait = portraitObject.GetComponent<RawImage>();
            portrait.texture = portraitRenderer != null ? portraitRenderer.GetPortrait(index) : null;
            portrait.color = Color.white;
            portrait.raycastTarget = false;
            Stretch(portrait.rectTransform, new Vector2(4f, 4f), new Vector2(-4f, -4f));
        }

        private Button CreateButton(
            string name,
            RectTransform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            float height,
            string localizationKey = null)
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

            var text = CreateTextObject("Label", buttonObject.GetComponent<RectTransform>(), label, 19, FontStyles.Bold, TextColor);
            if (!string.IsNullOrWhiteSpace(localizationKey))
            {
                LocalizedTextBinding.Attach(text, LocalizationTables.Common, localizationKey, label);
            }
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
            float preferredHeight,
            string localizationKey = null)
        {
            var text = CreateTextObject(name, parent, value, fontSize, fontStyle, color);
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
