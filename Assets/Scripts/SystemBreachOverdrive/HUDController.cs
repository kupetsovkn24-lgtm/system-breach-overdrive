using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace SystemBreachOverdrive
{
    public class HUDController : MonoBehaviour
    {
        private Text _levelText;
        private Text _timerText;
        private Text _statusText;
        private Text _progressText;
        private Text _poolText;
        private Button _pauseButton;
        private Button _restartButton;
        private Button _menuButton;
        private Button _settingsButton;
        private RectTransform _menuOverlay;
        private Button _menuResumeButton;
        private Button _menuMainMenuButton;
        private Button _menuSettingsButton;
        private Button _menuRulesButton;
        private RectTransform _startOverlay;
        private Button _startContinueButton;
        private Button _startNewGameButton;
        private Button _startResetProgressButton;
        private Button _startSettingsButton;
        private Button _startRulesButton;
        private Text _startSubtitleText;
        private RectTransform _settingsOverlay;
        private Slider _volumeSlider;
        private Button _settingsBackButton;
        private Text _volumeValueText;
        private RectTransform _resultsOverlay;
        private Text _resultsHeaderText;
        private Text _resultsScoreText;
        private Text _resultsMetaText;
        private Button _resultsRetryButton;
        private Button _resultsNextButton;
        private RectTransform _rulesOverlay;
        private Button _rulesBackButton;
        private RawImage _scanlineOverlay;
        private Image _flashOverlay;
        private Coroutine _flashRoutine;

        public void Initialize(
            Action onPause,
            Action onRestart,
            Action onMenu,
            Action onMenuResume,
            Action onMenuMain,
            Action onStartContinue,
            Action onStartNewGame,
            Action onStartResetProgress,
            Action onOpenSettings,
            Action onCloseSettings,
            Action<float> onVolumeChanged,
            Action onResultsRetry,
            Action onResultsNext,
            Action onOpenRules,
            Action onCloseRules)
        {
            EnsureEventSystem();
            CreateCanvas();

            _pauseButton.onClick.AddListener(() => onPause?.Invoke());
            _restartButton.onClick.AddListener(() => onRestart?.Invoke());
            _menuButton.onClick.AddListener(() => onMenu?.Invoke());
            _settingsButton.onClick.AddListener(() => onOpenSettings?.Invoke());
            _menuResumeButton.onClick.AddListener(() => onMenuResume?.Invoke());
            _menuMainMenuButton.onClick.AddListener(() => onMenuMain?.Invoke());
            _menuSettingsButton.onClick.AddListener(() => onOpenSettings?.Invoke());
            _menuRulesButton.onClick.AddListener(() => onOpenRules?.Invoke());
            _startContinueButton.onClick.AddListener(() => onStartContinue?.Invoke());
            _startNewGameButton.onClick.AddListener(() => onStartNewGame?.Invoke());
            _startResetProgressButton.onClick.AddListener(() => onStartResetProgress?.Invoke());
            _startSettingsButton.onClick.AddListener(() => onOpenSettings?.Invoke());
            _startRulesButton.onClick.AddListener(() => onOpenRules?.Invoke());
            _settingsBackButton.onClick.AddListener(() => onCloseSettings?.Invoke());
            _resultsRetryButton.onClick.AddListener(() => onResultsRetry?.Invoke());
            _resultsNextButton.onClick.AddListener(() => onResultsNext?.Invoke());
            _rulesBackButton.onClick.AddListener(() => onCloseRules?.Invoke());

            _volumeSlider.onValueChanged.AddListener(value =>
            {
                if (_volumeValueText != null)
                {
                    _volumeValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
                }

                onVolumeChanged?.Invoke(value);
            });
        }

        public void SetLevel(int levelNumber, int totalLevels)
        {
            if (_levelText != null)
            {
                _levelText.text = $"LEVEL {levelNumber}/{totalLevels}";
            }
        }

        public void SetTimer(float seconds)
        {
            if (_timerText != null)
            {
                var clamped = Mathf.Max(0f, seconds);
                _timerText.text = $"SYSTEM HEALTH: {clamped:00.0}s";
            }
        }

        public void SetStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
        }

        public void SetProgress(int connected, int total)
        {
            if (_progressText == null)
            {
                return;
            }

            _progressText.text = total > 0 ? $"OBJECTIVE NODES: {connected}/{total}" : "OBJECTIVE NODES: -";
        }

        public void SetPoolStats(int active, int cached, int created, int reused)
        {
            if (_poolText == null)
            {
                return;
            }

            _poolText.text = created > 0
                ? $"POOL A:{active} C:{cached} R:{reused}"
                : "POOL: -";
        }

        public void SetPauseState(bool paused)
        {
            var text = _pauseButton.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = paused ? "RESUME" : "PAUSE";
            }
        }

        public void SetMenuState(bool isVisible)
        {
            if (_menuOverlay != null)
            {
                _menuOverlay.gameObject.SetActive(isVisible);
            }
        }

        public void SetStartScreenState(bool isVisible, bool hasContinue, int continueLevel, int totalLevels)
        {
            if (_startOverlay == null)
            {
                return;
            }

            _startOverlay.gameObject.SetActive(isVisible);
            _startContinueButton.gameObject.SetActive(hasContinue);
            _startResetProgressButton.gameObject.SetActive(hasContinue);
            _startSubtitleText.text = hasContinue
                ? $"CONTINUE AVAILABLE: LEVEL {continueLevel}/{totalLevels}"
                : "NEW SESSION INITIALIZED";
        }

        public void SetSettingsState(bool isVisible)
        {
            if (_settingsOverlay != null)
            {
                _settingsOverlay.gameObject.SetActive(isVisible);
            }
        }

        public void ConfigureSettings(float volumeNormalized)
        {
            if (_volumeSlider != null)
            {
                var clamped = Mathf.Clamp01(volumeNormalized);
                _volumeSlider.SetValueWithoutNotify(clamped);
                if (_volumeValueText != null)
                {
                    _volumeValueText.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
                }
            }
        }

        public void SetResultsState(bool isVisible)
        {
            if (_resultsOverlay != null)
            {
                _resultsOverlay.gameObject.SetActive(isVisible);
            }
        }

        public void SetRulesState(bool isVisible)
        {
            if (_rulesOverlay != null)
            {
                _rulesOverlay.gameObject.SetActive(isVisible);
            }
        }

        public void SetResultsContent(string header, int score, int rotations, float timeLeft, int combo, string rating, bool nextAvailable)
        {
            if (_resultsHeaderText != null)
            {
                _resultsHeaderText.text = header;
            }

            if (_resultsScoreText != null)
            {
                _resultsScoreText.text = $"SCORE: {score}   RATING: {rating}";
            }

            if (_resultsMetaText != null)
            {
                _resultsMetaText.text = $"TIME LEFT: {Mathf.Max(0f, timeLeft):00.0}s    ROTATIONS: {rotations}    QUICK COMBO: x{Mathf.Max(1, combo)}";
            }

            if (_resultsNextButton != null)
            {
                _resultsNextButton.gameObject.SetActive(nextAvailable);
            }
        }

        public void Flash(Color color, float durationSeconds)
        {
            if (_flashOverlay == null)
            {
                return;
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashRoutine(color, durationSeconds));
        }

        private IEnumerator FlashRoutine(Color color, float durationSeconds)
        {
            _flashOverlay.gameObject.SetActive(true);
            var c = color;
            c.a = Mathf.Clamp01(c.a);
            _flashOverlay.color = c;

            var elapsed = 0f;
            var duration = Mathf.Max(0.05f, durationSeconds);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var alpha = Mathf.Lerp(c.a, 0f, t);
                var current = _flashOverlay.color;
                _flashOverlay.color = new Color(current.r, current.g, current.b, alpha);
                yield return null;
            }

            _flashOverlay.gameObject.SetActive(false);
            _flashRoutine = null;
        }

        private void CreateCanvas()
        {
            var canvasObject = new GameObject("GameHUD");
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _scanlineOverlay = CreateRawOverlay(canvasObject.transform, "Scanlines", new Color(0.35f, 0.92f, 1f, 0.07f), BuildScanlineTexture());
            _scanlineOverlay.raycastTarget = false;

            var topPanelObject = new GameObject("TopPanel");
            topPanelObject.transform.SetParent(canvasObject.transform, false);
            var topPanel = topPanelObject.AddComponent<RectTransform>();
            topPanel.anchorMin = new Vector2(0f, 1f);
            topPanel.anchorMax = new Vector2(1f, 1f);
            topPanel.pivot = new Vector2(0.5f, 1f);
            topPanel.anchoredPosition = Vector2.zero;
            topPanel.sizeDelta = new Vector2(0f, 70f);
            topPanelObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

            _levelText = CreateAnchoredLabel(topPanel, "LevelText", "LEVEL", font, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(250f, 0f));
            _timerText = CreateAnchoredLabel(topPanel, "TimerText", "SYSTEM HEALTH", font, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 0f));
            _statusText = CreateAnchoredLabel(topPanel, "StatusText", "", font, TextAnchor.MiddleRight, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(250f, 0f));
            _progressText = CreateAnchoredLabel(topPanel, "ProgressText", "OBJECTIVE NODES: -", font, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(420f, 0f));
            _progressText.fontSize = 16;
            _poolText = CreateAnchoredLabel(topPanel, "PoolText", "POOL: -", font, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(16f, -22f), new Vector2(420f, 0f));
            _poolText.fontSize = 14;

            var buttonsPanel = CreatePanel(canvasObject.transform, "ButtonsPanel", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-240f, 50f), new Vector2(420f, 70f), new Color(0f, 0f, 0f, 0.35f));

            _pauseButton = CreateButton(buttonsPanel, "PauseButton", "PAUSE", font, new Vector2(-150f, 0f));
            _restartButton = CreateButton(buttonsPanel, "RestartButton", "RESTART", font, new Vector2(-50f, 0f));
            _menuButton = CreateButton(buttonsPanel, "MenuButton", "MENU", font, new Vector2(50f, 0f));
            _settingsButton = CreateButton(buttonsPanel, "SettingsButton", "SETTINGS", font, new Vector2(150f, 0f), new Vector2(100f, 38f));

            _menuOverlay = CreatePanel(canvasObject.transform, "MenuOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.90f));
            var menuCard = CreateCard(_menuOverlay, "MenuCard", new Vector2(440f, 380f));
            CreateLabel(menuCard, "MenuTitle", "PAUSE MENU", font, TextAnchor.MiddleCenter, new Vector2(0f, 130f), new Vector2(320f, 44f));
            _menuResumeButton = CreateButton(menuCard, "MenuResumeButton", "RESUME", font, new Vector2(0f, 55f), new Vector2(240f, 52f));
            _menuMainMenuButton = CreateButton(menuCard, "MenuMainButton", "MAIN MENU", font, new Vector2(0f, -7f), new Vector2(240f, 52f));
            _menuSettingsButton = CreateButton(menuCard, "MenuSettingsButton", "SETTINGS", font, new Vector2(0f, -69f), new Vector2(240f, 52f));
            _menuRulesButton = CreateButton(menuCard, "MenuRulesButton", "RULES", font, new Vector2(0f, -131f), new Vector2(240f, 52f));

            _startOverlay = CreatePanel(canvasObject.transform, "StartOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.92f));
            var startCard = CreateCard(_startOverlay, "StartCard", new Vector2(620f, 610f));
            CreateLabel(startCard, "StartTitle", "SYSTEM BREACH: OVERDRIVE", font, TextAnchor.MiddleCenter, new Vector2(0f, 230f), new Vector2(580f, 60f));
            _startSubtitleText = CreateLabel(startCard, "StartSubtitle", "", font, TextAnchor.MiddleCenter, new Vector2(0f, 185f), new Vector2(520f, 40f));
            CreateLabel(startCard, "StartHint", "LMB: ROTATE  |  R: RESTART  |  ESC: MENU", font, TextAnchor.MiddleCenter, new Vector2(0f, -230f), new Vector2(520f, 34f));
            _startContinueButton = CreateButton(startCard, "StartContinueButton", "CONTINUE", font, new Vector2(0f, 95f), new Vector2(270f, 52f));
            _startNewGameButton = CreateButton(startCard, "StartNewGameButton", "NEW GAME", font, new Vector2(0f, 28f), new Vector2(270f, 52f));
            _startResetProgressButton = CreateButton(startCard, "StartResetButton", "RESET PROGRESS", font, new Vector2(0f, -39f), new Vector2(270f, 52f));
            _startSettingsButton = CreateButton(startCard, "StartSettingsButton", "SETTINGS", font, new Vector2(0f, -106f), new Vector2(270f, 52f));
            _startRulesButton = CreateButton(startCard, "StartRulesButton", "RULES", font, new Vector2(0f, -173f), new Vector2(270f, 52f));

            _settingsOverlay = CreatePanel(canvasObject.transform, "SettingsOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.92f));
            var settingsCard = CreateCard(_settingsOverlay, "SettingsCard", new Vector2(640f, 360f));
            CreateLabel(settingsCard, "SettingsTitle", "SETTINGS", font, TextAnchor.MiddleCenter, new Vector2(0f, 120f), new Vector2(420f, 48f));
            CreateLabel(settingsCard, "VolumeLabel", "MASTER VOLUME", font, TextAnchor.MiddleLeft, new Vector2(-160f, 25f), new Vector2(250f, 36f));
            _volumeValueText = CreateLabel(settingsCard, "VolumeValue", "100%", font, TextAnchor.MiddleRight, new Vector2(195f, 25f), new Vector2(120f, 36f));

            _volumeSlider = CreateSlider(settingsCard, "VolumeSlider", new Vector2(20f, -2f), new Vector2(390f, 26f));
            _settingsBackButton = CreateButton(settingsCard, "SettingsBackButton", "BACK", font, new Vector2(0f, -90f), new Vector2(220f, 52f));

            _resultsOverlay = CreatePanel(canvasObject.transform, "ResultsOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.92f));
            var resultsCard = CreateCard(_resultsOverlay, "ResultsCard", new Vector2(720f, 430f));
            _resultsHeaderText = CreateLabel(resultsCard, "ResultsHeader", "LEVEL COMPLETE", font, TextAnchor.MiddleCenter, new Vector2(0f, 136f), new Vector2(560f, 50f));
            _resultsScoreText = CreateLabel(resultsCard, "ResultsScore", "SCORE: 0", font, TextAnchor.MiddleCenter, new Vector2(0f, 66f), new Vector2(620f, 42f));
            _resultsMetaText = CreateLabel(resultsCard, "ResultsMeta", "", font, TextAnchor.MiddleCenter, new Vector2(0f, 8f), new Vector2(660f, 40f));
            _resultsRetryButton = CreateButton(resultsCard, "ResultsRetry", "RETRY", font, new Vector2(-130f, -95f), new Vector2(230f, 52f));
            _resultsNextButton = CreateButton(resultsCard, "ResultsNext", "NEXT", font, new Vector2(130f, -95f), new Vector2(230f, 52f));

            _rulesOverlay = CreatePanel(canvasObject.transform, "RulesOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.94f));
            var rulesCard = CreateCard(_rulesOverlay, "RulesCard", new Vector2(860f, 610f));
            CreateLabel(rulesCard, "RulesTitle", "RULES", font, TextAnchor.MiddleCenter, new Vector2(0f, 250f), new Vector2(420f, 52f));
            var rulesBody = CreateLabel(
                rulesCard,
                "RulesBody",
                "1) Rotate nodes with LMB to connect SOURCE to all EXIT nodes.\n\n" +
                "2) Required nodes must be part of the active signal path.\n\n" +
                "3) Rotating overloaded nodes applies a time penalty.\n\n" +
                "4) Build the route before timer reaches zero.\n\n" +
                "5) Score depends on remaining time, rotations, objectives, and quick-finish combo.",
                font,
                TextAnchor.UpperLeft,
                new Vector2(0f, 20f),
                new Vector2(760f, 340f));
            rulesBody.fontSize = 24;
            rulesBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            rulesBody.verticalOverflow = VerticalWrapMode.Overflow;
            _rulesBackButton = CreateButton(rulesCard, "RulesBack", "BACK", font, new Vector2(0f, -242f), new Vector2(230f, 52f));

            _flashOverlay = CreateImageOverlay(canvasObject.transform, "FlashOverlay", new Color(1f, 1f, 1f, 0f));
            _flashOverlay.raycastTarget = false;
            _flashOverlay.gameObject.SetActive(false);

            SetMenuState(false);
            SetStartScreenState(true, false, 1, 1);
            SetSettingsState(false);
            SetResultsState(false);
            SetRulesState(false);
        }

        private static void EnsureEventSystem()
        {
            var existingEventSystem = FindFirstObjectByType<EventSystem>();
            if (existingEventSystem != null)
            {
#if ENABLE_INPUT_SYSTEM
                if (existingEventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    var standalone = existingEventSystem.GetComponent<StandaloneInputModule>();
                    if (standalone != null)
                    {
                        Destroy(standalone);
                    }

                    existingEventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }
#endif
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = panel.AddComponent<Image>();
            image.color = color;

            return rect;
        }

        private static RectTransform CreateCard(Transform parent, string name, Vector2 size)
        {
            CreatePanel(
                parent,
                name + "Shadow",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(8f, -8f),
                size,
                new Color(0f, 0f, 0f, 0.55f));

            var card = CreatePanel(
                parent,
                name,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                size,
                new Color(0.03f, 0.10f, 0.15f, 0.94f));

            CreatePanel(
                card,
                name + "Accent",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -8f),
                new Vector2(size.x - 28f, 4f),
                new Color(0.35f, 0.92f, 1f, 0.85f));

            return card;
        }

        private static RawImage CreateRawOverlay(Transform parent, string name, Color tint, Texture texture)
        {
            var overlayObject = new GameObject(name);
            overlayObject.transform.SetParent(parent, false);

            var rect = overlayObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var rawImage = overlayObject.AddComponent<RawImage>();
            rawImage.texture = texture;
            rawImage.color = tint;
            rawImage.uvRect = new Rect(0f, 0f, 32f, 18f);

            return rawImage;
        }

        private static Image CreateImageOverlay(Transform parent, string name, Color color)
        {
            var overlayObject = new GameObject(name);
            overlayObject.transform.SetParent(parent, false);

            var rect = overlayObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = overlayObject.AddComponent<Image>();
            image.color = color;

            return image;
        }

        private static Texture2D BuildScanlineTexture()
        {
            const int width = 8;
            const int height = 8;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Point;

            for (var y = 0; y < height; y++)
            {
                var alpha = (y % 2 == 0) ? 0.75f : 0.1f;
                for (var x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private static Text CreateLabel(Transform parent, string name, string initialText, Font font, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size)
        {
            var label = new GameObject(name);
            label.transform.SetParent(parent, false);

            var rect = label.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = label.AddComponent<Text>();
            text.font = font;
            text.fontSize = 20;
            text.text = initialText;
            text.alignment = alignment;
            text.color = new Color(0.35f, 0.92f, 1f);

            return text;
        }

        private static Text CreateAnchoredLabel(
            Transform parent,
            string name,
            string initialText,
            Font font,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var label = new GameObject(name);
            label.transform.SetParent(parent, false);

            var rect = label.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = label.AddComponent<Text>();
            text.font = font;
            text.fontSize = 20;
            text.text = initialText;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = new Color(0.35f, 0.92f, 1f);

            return text;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);

            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var background = new GameObject("Background");
            background.transform.SetParent(root.transform, false);
            var bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.15f, 0.2f, 1f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(root.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(6f, 0f);
            fillAreaRect.offsetMax = new Vector2(-6f, 0f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.20f, 0.85f, 0.96f, 1f);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(root.transform, false);
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(14f, 30f);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.75f, 0.97f, 1f, 1f);

            var slider = root.AddComponent<Slider>();
            slider.targetGraphic = handleImage;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }

        private static Dropdown CreateDropdown(Transform parent, string name, Font font, Vector2 anchoredPosition, Vector2 size)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);

            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = root.AddComponent<Image>();
            image.color = new Color(0.05f, 0.18f, 0.24f, 0.95f);

            var dropdown = root.AddComponent<Dropdown>();
            dropdown.targetGraphic = image;

            var label = new GameObject("Label");
            label.transform.SetParent(root.transform, false);
            var labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-24f, 0f);

            var labelText = label.AddComponent<Text>();
            labelText.font = font;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = new Color(0.35f, 0.92f, 1f);
            labelText.text = "QUALITY";

            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(root.transform, false);
            var arrowRect = arrow.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0.5f);
            arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-10f, 0f);
            arrowRect.sizeDelta = new Vector2(8f, 8f);
            var arrowImage = arrow.AddComponent<Image>();
            arrowImage.color = new Color(0.75f, 0.97f, 1f, 1f);

            var template = new GameObject("Template");
            template.transform.SetParent(root.transform, false);
            var templateRect = template.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -2f);
            templateRect.sizeDelta = new Vector2(0f, 110f);
            var templateImage = template.AddComponent<Image>();
            templateImage.color = new Color(0.04f, 0.12f, 0.18f, 0.98f);
            var scrollRect = template.AddComponent<ScrollRect>();

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.0f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 28f);

            var item = new GameObject("Item");
            item.transform.SetParent(content.transform, false);
            var itemRect = item.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 28f);
            item.AddComponent<Toggle>();

            var itemBg = item.AddComponent<Image>();
            itemBg.color = new Color(0.05f, 0.18f, 0.24f, 0.95f);

            var itemLabelObj = new GameObject("Item Label");
            itemLabelObj.transform.SetParent(item.transform, false);
            var itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(10f, 0f);
            itemLabelRect.offsetMax = new Vector2(-10f, 0f);
            var itemLabel = itemLabelObj.AddComponent<Text>();
            itemLabel.font = font;
            itemLabel.alignment = TextAnchor.MiddleLeft;
            itemLabel.color = new Color(0.35f, 0.92f, 1f);

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            template.SetActive(false);

            dropdown.captionText = labelText;
            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;

            return dropdown;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 anchoredPosition)
        {
            return CreateButton(parent, name, label, font, anchoredPosition, new Vector2(120f, 50f));
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 anchoredPosition, Vector2 size)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.03f, 0.10f, 0.15f, 0.98f);

            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.07f, 0.18f, 0.26f, 1f);
            colors.pressedColor = new Color(0.02f, 0.07f, 0.12f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(buttonObject.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.35f, 0.92f, 1f);
            text.fontSize = 19;

            return button;
        }
    }
}
