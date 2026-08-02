using System;
using Jam.Core.Audio;
using Jam.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Jam.Core.Cutscenes
{
    public sealed class UiStoryboardPresentation : MonoBehaviour, ICutscenePresentation
    {
        [SerializeField] private string cutsceneId = "cutscene.storyboard";
        [SerializeField] private StoryboardCutsceneAsset sequence;
        [SerializeField] private MonoBehaviour scenePresenter;

        private static readonly Color FallbackBackground = new(0.035f, 0.045f, 0.065f, 1f);
        private static readonly Color DialoguePanel = new(0.055f, 0.065f, 0.09f, 0.96f);
        private static readonly Color Accent = new(0.91f, 0.55f, 0.24f, 1f);
        private static readonly Color TextColor = new(0.94f, 0.93f, 0.89f, 1f);
        private static readonly Color MutedText = new(0.66f, 0.69f, 0.75f, 1f);

        public string CutsceneId => cutsceneId;
        public bool IsPlaying => _onFinished != null;
        public bool CanSkip => sequence != null && sequence.Skippable;

        public void Configure(string id, StoryboardCutsceneAsset storyboard, MonoBehaviour presenter = null)
        {
            cutsceneId = id;
            sequence = storyboard;
            scenePresenter = presenter;
        }

        private Action<CutsceneEndReason> _onFinished;
        private GameObject _root;
        private Image _background;
        private Image _portrait;
        private RawImage _renderedBackground;
        private RawImage _renderedPortrait;
        private TMP_Text _speaker;
        private TMP_Text _body;
        private TMP_Text _progress;
        private Button _skipButton;
        private int _frameIndex;
        private float _frameElapsed;

        private void Awake()
        {
            BuildInterface();
        }

        private void OnEnable()
        {
            Loc.LocaleChanged += RefreshLocalizedFrame;
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            _frameElapsed += Time.unscaledDeltaTime;
            var frame = sequence.Frames[_frameIndex];
            if (frame.autoAdvanceSeconds > 0f && _frameElapsed >= frame.autoAdvanceSeconds)
            {
                Advance();
                return;
            }

            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true
                || Keyboard.current?.enterKey.wasPressedThisFrame == true)
            {
                Advance();
            }
        }

        private void OnDisable()
        {
            Loc.LocaleChanged -= RefreshLocalizedFrame;
            if (IsPlaying)
            {
                Finish(CutsceneEndReason.SceneChanged);
            }
        }

        public void Play(CutsceneContext context, Action<CutsceneEndReason> onFinished)
        {
            if (IsPlaying)
            {
                throw new InvalidOperationException($"Storyboard '{cutsceneId}' is already playing.");
            }

            if (sequence == null || sequence.Frames == null || sequence.Frames.Length == 0)
            {
                onFinished?.Invoke(CutsceneEndReason.Failed);
                return;
            }

            _onFinished = onFinished;
            _frameIndex = 0;
            _root.SetActive(true);
            _skipButton.gameObject.SetActive(CanSkip);
            ShowFrame();
        }

        public void Skip()
        {
            if (IsPlaying && CanSkip)
            {
                Finish(CutsceneEndReason.Skipped);
            }
        }

        public void Stop(CutsceneEndReason reason)
        {
            if (IsPlaying)
            {
                Finish(reason);
            }
        }

        public void Advance()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (_frameIndex + 1 >= sequence.Frames.Length)
            {
                Finish(CutsceneEndReason.Completed);
                return;
            }

            _frameIndex++;
            ShowFrame();
        }

        private void ShowFrame()
        {
            var frame = sequence.Frames[_frameIndex];
            _frameElapsed = 0f;
            var rendered = scenePresenter as IStoryboardScenePresenter;
            rendered?.ShowFrame(_frameIndex);
            _renderedBackground.texture = rendered?.StageTexture;
            _renderedBackground.gameObject.SetActive(_renderedBackground.texture != null);
            _renderedPortrait.texture = rendered?.PortraitTexture;
            _renderedPortrait.gameObject.SetActive(_renderedPortrait.texture != null);
            _background.sprite = frame.background;
            _background.color = frame.background == null ? FallbackBackground : Color.white;
            _portrait.sprite = frame.portrait;
            _portrait.gameObject.SetActive(rendered == null && frame.portrait != null);
            RefreshLocalizedFrame();

            AudioService.Instance?.PlayVoice(frame.voice);
        }

        private void RefreshLocalizedFrame()
        {
            if (!IsPlaying || sequence == null || sequence.Frames == null || sequence.Frames.Length == 0)
            {
                return;
            }

            var frame = sequence.Frames[_frameIndex];
            var table = string.IsNullOrWhiteSpace(frame.localizationTable)
                ? LocalizationTables.Photo
                : frame.localizationTable;
            _speaker.text = string.IsNullOrWhiteSpace(frame.speakerKey)
                ? string.IsNullOrWhiteSpace(frame.speaker) ? "…" : frame.speaker
                : Loc.Get(table, frame.speakerKey, frame.speaker);
            _body.text = string.IsNullOrWhiteSpace(frame.textKey)
                ? frame.text ?? string.Empty
                : Loc.Get(table, frame.textKey, frame.text);
            _progress.text = Loc.Get(
                LocalizationTables.Common,
                "ui.cutscene.progress",
                "{0} / {1}   •   ПРОБЕЛ / КЛИК",
                _frameIndex + 1,
                sequence.Frames.Length);
        }

        private void Finish(CutsceneEndReason reason)
        {
            var callback = _onFinished;
            _onFinished = null;
            AudioService.Instance?.StopVoice();
            (scenePresenter as IStoryboardScenePresenter)?.Hide();
            _root.SetActive(false);
            callback?.Invoke(reason);
        }

        private void BuildInterface()
        {
            _root = new GameObject(
                "StoryboardCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var canvasRect = _root.GetComponent<RectTransform>();
            _background = CreateImage("Background", canvasRect, FallbackBackground);
            Stretch(_background.rectTransform);

            _renderedBackground = CreateRawImage("RenderedBackground", _background.rectTransform);
            Stretch(_renderedBackground.rectTransform);

            _portrait = CreateImage("Portrait", _background.rectTransform, Color.white);
            SetAnchoredRect(
                _portrait.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(0.46f, 0.92f),
                Vector2.zero,
                Vector2.zero);
            _portrait.preserveAspect = true;

            _renderedPortrait = CreateRawImage("RenderedPortrait", _background.rectTransform);
            SetAnchoredRect(_renderedPortrait.rectTransform, new Vector2(0.06f, 0.05f), new Vector2(0.25f, 0.32f), Vector2.zero, Vector2.zero);

            var panel = CreateImage("DialoguePanel", _background.rectTransform, DialoguePanel);
            SetAnchoredRect(
                panel.rectTransform,
                new Vector2(0.06f, 0.04f),
                new Vector2(0.94f, 0.34f),
                Vector2.zero,
                Vector2.zero);

            _speaker = CreateText("Speaker", panel.rectTransform, string.Empty, 24, FontStyles.Bold, Accent, TextAlignmentOptions.TopLeft);
            SetAnchoredRect(_speaker.rectTransform, new Vector2(0.04f, 0.70f), new Vector2(0.96f, 0.94f), Vector2.zero, Vector2.zero);
            _body = CreateText("Body", panel.rectTransform, string.Empty, 25, FontStyles.Normal, TextColor, TextAlignmentOptions.TopLeft);
            SetAnchoredRect(_body.rectTransform, new Vector2(0.25f, 0.20f), new Vector2(0.96f, 0.72f), Vector2.zero, Vector2.zero);
            _progress = CreateText("Progress", panel.rectTransform, string.Empty, 14, FontStyles.Normal, MutedText, TextAlignmentOptions.MidlineRight);
            SetAnchoredRect(_progress.rectTransform, new Vector2(0.50f, 0.02f), new Vector2(0.96f, 0.18f), Vector2.zero, Vector2.zero);

            var next = CreateButton("AdvanceFrameButton", _background.rectTransform, string.Empty, Advance, 1, Color.clear);
            Stretch(next.GetComponent<RectTransform>());
            _skipButton = CreateButton("SkipCutsceneButton", _background.rectTransform, "ПРОПУСТИТЬ  [ESC]", Skip, 16, DialoguePanel);
            LocalizedTextBinding.Attach(
                _skipButton.transform.Find("Label").GetComponent<TMP_Text>(),
                LocalizationTables.Common,
                "ui.cutscene.skip",
                "ПРОПУСТИТЬ  [ESC]");
            SetAnchoredRect(
                _skipButton.GetComponent<RectTransform>(),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-118f, -42f),
                new Vector2(210f, 54f));
            _renderedPortrait.transform.SetAsLastSibling();
            _root.SetActive(false);
        }

        private Button CreateButton(
            string name,
            RectTransform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            int fontSize,
            Color color)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = color;
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var text = CreateText("Label", buttonObject.GetComponent<RectTransform>(), label, fontSize, FontStyles.Bold, TextColor, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            return button;
        }

        private TMP_Text CreateText(
            string name,
            RectTransform parent,
            string value,
            int fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
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

        private static RawImage CreateRawImage(string name, RectTransform parent)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<RawImage>();
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
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
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
