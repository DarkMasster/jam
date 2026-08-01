using System.IO;
using Jam.Core.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        private Button _quitButton;
        private Text _statusText;
        private bool _isLoading;
        private Font _font;

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildInterface();
            RefreshState();
        }

        private void OnEnable()
        {
            if (_continueButton != null)
            {
                RefreshState();
            }
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
                SetStatus("Не найдена сцена начала игры.");
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
                SetStatus("Сохранение не найдено или устарело.");
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
            SetStatus("Загрузка…");
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

            SetStatus(canContinue ? $"Последняя точка: {sceneName}" : "Сохранение не найдено");
        }

        private void SetButtonsInteractable(bool value)
        {
            _newGameButton.interactable = value;
            _continueButton.interactable = value;
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
            if (FindFirstObjectByType<EventSystem>() != null)
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
            SetAnchoredRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 610f));

            var panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(54, 54, 48, 42);
            panelLayout.spacing = 18f;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = false;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            CreateLabel("Title", panel.rectTransform, "ОТРАЖЕНИЕ / ИМПУЛЬС", 42, FontStyle.Bold, TextColor, 72f);
            CreateLabel("Subtitle", panel.rectTransform, "REFLECTION + MOMENTUM", 19, FontStyle.Normal, AccentColor, 34f);
            CreateSpacer(panel.rectTransform, 30f);

            _newGameButton = CreateButton("NewGameButton", panel.rectTransform, "НАЧАТЬ НОВУЮ ИГРУ", StartNewGame);
            _continueButton = CreateButton("ContinueButton", panel.rectTransform, "ПРОДОЛЖИТЬ", ContinueGame);
            _quitButton = CreateButton("QuitButton", panel.rectTransform, "ВЫХОД", QuitGame);

            CreateSpacer(panel.rectTransform, 22f);
            _statusText = CreateLabel("SaveStatus", panel.rectTransform, string.Empty, 18, FontStyle.Normal, MutedTextColor, 30f);
            CreateSpacer(panel.rectTransform, 12f);
            CreateLabel("Footer", panel.rectTransform, "ТРИ ИСТОРИИ • ОДИН МОМЕНТ", 15, FontStyle.Normal, MutedTextColor, 24f);

            _newGameButton.Select();
        }

        private Button CreateButton(string name, RectTransform parent, string label, UnityEngine.Events.UnityAction action)
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

            var text = CreateTextObject("Label", buttonObject.GetComponent<RectTransform>(), label, 21, FontStyle.Bold, TextColor);
            Stretch(text.rectTransform, new Vector2(20f, 0f), new Vector2(-20f, 0f));

            return button;
        }

        private Text CreateLabel(
            string name,
            RectTransform parent,
            string value,
            int fontSize,
            FontStyle fontStyle,
            Color color,
            float preferredHeight)
        {
            var text = CreateTextObject(name, parent, value, fontSize, fontStyle, color);
            var layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            return text;
        }

        private Text CreateTextObject(
            string name,
            RectTransform parent,
            string value,
            int fontSize,
            FontStyle fontStyle,
            Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
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
