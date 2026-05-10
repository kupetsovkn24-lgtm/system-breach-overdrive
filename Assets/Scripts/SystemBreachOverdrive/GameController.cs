using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SystemBreachOverdrive
{
    public class GameController : MonoBehaviour
    {
        private const int TargetLevelCount = 20;
        private const string ProgressKey = "SystemBreachOverdrive_UnlockedLevel";
        private const string MasterVolumeKey = "SystemBreachOverdrive_MasterVolume";

        private List<LevelConfig> _levels;

        private GridManager _gridManager;
        private HUDController _hud;
        private AudioSource _audioSource;
        private AudioClip _rotateClip;
        private AudioClip _successClip;
        private AudioClip _failureClip;
        private int _currentLevelIndex;
        private float _remainingTime;
        private bool _levelActive;
        private bool _isPaused;
        private bool _isMenuOpen;
        private bool _isResolvingSignal;
        private bool _isOnStartScreen;
        private bool _isSettingsOpen;
        private bool _isResultsOpen;
        private bool _isRulesOpen;
        private int _rotationCount;
        private int _quickCombo = 1;
        private int _levelScore;
        private int _totalScore;
        private bool _lastResultSuccess;
        private int _pendingNextLevel;
        private float _masterVolume;
        private Coroutine _transitionRoutine;

        private void Awake()
        {
            _gridManager = GetComponent<GridManager>();
            if (_gridManager == null)
            {
                _gridManager = gameObject.AddComponent<GridManager>();
            }

            _hud = GetComponent<HUDController>();
            if (_hud == null)
            {
                _hud = gameObject.AddComponent<HUDController>();
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.volume = 0.55f;

            _rotateClip = CreateToneClip(660f, 0.06f, 0.02f);
            _successClip = CreateSweepClip(440f, 990f, 0.26f, 0.04f);
            _failureClip = CreateSweepClip(520f, 170f, 0.34f, 0.05f);

            _masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            GamePalette.SetMode(PaletteMode.Default);

            _levels = LoadLevels();
            ApplyMasterVolume();
        }

        private void Start()
        {
            _gridManager.OnGridChanged += OnGridChanged;
            _gridManager.OnNodeRotated += HandleNodeRotated;

            _hud.Initialize(
                TogglePause,
                RestartLevel,
                ToggleMenu,
                ResumeFromMenu,
                ReturnToFirstLevel,
                StartFromContinue,
                StartNewGame,
                ResetProgress,
                OpenSettings,
                CloseSettings,
                SetMasterVolume,
                OnResultsRetry,
                OnResultsNext,
                OpenRules,
                CloseRules);

            _hud.ConfigureSettings(_masterVolume);
            _hud.SetPauseState(false);
            _hud.SetMenuState(false);
            ShowStartScreen();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;

            if (_gridManager != null)
            {
                _gridManager.OnGridChanged -= OnGridChanged;
                _gridManager.OnNodeRotated -= HandleNodeRotated;
            }
        }

        private void HandleNodeRotated(GridNode node)
        {
            _rotationCount++;
            PlayOneShot(_rotateClip, 0.35f);

            if (node != null && node.IsOverloaded)
            {
                _remainingTime = Mathf.Max(0f, _remainingTime - _gridManager.CurrentConfig.OverloadPenaltySeconds);
                _hud.SetTimer(_remainingTime);
                _hud.SetStatus("OVERLOAD PENALTY");
                _hud.Flash(new Color(GamePalette.Current.Overloaded.r, GamePalette.Current.Overloaded.g, GamePalette.Current.Overloaded.b, 0.35f), 0.28f);
            }
        }

        private void Update()
        {
            if (_isOnStartScreen)
            {
                if (IsEscapePressed() && _isSettingsOpen)
                {
                    CloseSettings();
                }

                if (IsEscapePressed() && _isRulesOpen)
                {
                    CloseRules();
                }

                return;
            }

            if (_isResultsOpen)
            {
                if (IsEscapePressed() && _isRulesOpen)
                {
                    CloseRules();
                }
                return;
            }

            if (IsEscapePressed())
            {
                if (_isRulesOpen)
                {
                    CloseRules();
                    return;
                }

                if (_isSettingsOpen)
                {
                    CloseSettings();
                    return;
                }

                ToggleMenu();
                return;
            }

            if (IsRestartPressed())
            {
                RestartLevel();
            }

            if (!_levelActive || _isPaused)
            {
                return;
            }

            _remainingTime -= Time.deltaTime;
            _hud.SetTimer(_remainingTime);

            if (_remainingTime <= 0f)
            {
                HandleSystemFailure();
            }
        }

        private void StartLevel(int levelIndex)
        {
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
            }

            _currentLevelIndex = Mathf.Clamp(levelIndex, 0, _levels.Count - 1);
            _isResolvingSignal = false;
            _isOnStartScreen = false;
            _isSettingsOpen = false;
            _isResultsOpen = false;
            _isRulesOpen = false;
            SetPaused(false);
            _isMenuOpen = false;
            _levelActive = true;
            _rotationCount = 0;
            _levelScore = 0;
            _lastResultSuccess = false;
            _pendingNextLevel = Mathf.Min(_currentLevelIndex + 1, _levels.Count - 1);

            var config = _levels[_currentLevelIndex];
            _remainingTime = config.TimerSeconds;

            _gridManager.Generate(config);
            _hud.SetLevel(_currentLevelIndex + 1, _levels.Count);
            _hud.SetTimer(_remainingTime);
            _hud.SetStatus("SYSTEM ONLINE");
            _hud.SetPauseState(false);
            _hud.SetMenuState(false);
            _hud.SetStartScreenState(false, false, 1, _levels.Count);
            _hud.SetSettingsState(false);
            _hud.SetResultsState(false);
            _hud.SetRulesState(false);
            _gridManager.SetInteractionEnabled(true);
            UpdateObjectiveProgress();
            UpdatePoolStats();
        }

        private void ShowStartScreen()
        {
            _isOnStartScreen = true;
            _levelActive = false;
            _isResolvingSignal = false;
            _isMenuOpen = false;
            _isSettingsOpen = false;
            _isResultsOpen = false;
            _isRulesOpen = false;

            SetPaused(false);
            _gridManager.SetInteractionEnabled(false);

            var unlocked = LoadUnlockedLevel();
            _hud.SetLevel(1, _levels.Count);
            _hud.SetTimer(0f);
            _hud.SetStatus("READY");
            _hud.SetMenuState(false);
            _hud.SetStartScreenState(true, unlocked > 0, unlocked + 1, _levels.Count);
            _hud.SetSettingsState(false);
            _hud.SetResultsState(false);
            _hud.SetRulesState(false);
            _hud.SetProgress(0, 0);
            UpdatePoolStats();
        }

        private void StartFromContinue()
        {
            StartLevel(LoadUnlockedLevel());
        }

        private void StartNewGame()
        {
            StartLevel(0);
        }

        private void ResetProgress()
        {
            PlayerPrefs.DeleteKey(ProgressKey);
            PlayerPrefs.Save();
            ShowStartScreen();
        }

        private void OpenSettings()
        {
            if (_isRulesOpen)
            {
                CloseRules();
            }

            _isSettingsOpen = true;
            _hud.SetSettingsState(true);
            _hud.SetMenuState(false);
            _hud.ConfigureSettings(_masterVolume);

            if (!_isOnStartScreen)
            {
                SetPaused(true);
            }

            _hud.SetStatus("SETTINGS");
        }

        private void CloseSettings()
        {
            if (!_isSettingsOpen)
            {
                return;
            }

            _isSettingsOpen = false;
            _hud.SetSettingsState(false);

            if (_isOnStartScreen)
            {
                _hud.SetStatus("READY");
                return;
            }

            if (_isMenuOpen)
            {
                _hud.SetMenuState(true);
                _hud.SetStatus("MENU");
                SetPaused(true);
                return;
            }

            SetPaused(false);
            _hud.SetStatus("SYSTEM ONLINE");
        }

        private void OpenRules()
        {
            _isRulesOpen = true;
            _hud.SetRulesState(true);
            _hud.SetMenuState(false);
            _hud.SetSettingsState(false);

            if (!_isOnStartScreen && !_isResultsOpen)
            {
                SetPaused(true);
            }

            _hud.SetStatus("RULES");
        }

        private void CloseRules()
        {
            if (!_isRulesOpen)
            {
                return;
            }

            _isRulesOpen = false;
            _hud.SetRulesState(false);

            if (_isOnStartScreen)
            {
                _hud.SetStatus("READY");
                return;
            }

            if (_isResultsOpen)
            {
                return;
            }

            if (_isMenuOpen)
            {
                _hud.SetMenuState(true);
                _hud.SetStatus("MENU");
                SetPaused(true);
                return;
            }

            SetPaused(false);
            _hud.SetStatus("SYSTEM ONLINE");
        }

        private void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            ApplyMasterVolume();
            PlayerPrefs.SetFloat(MasterVolumeKey, _masterVolume);
            PlayerPrefs.Save();
        }

        private void ApplyMasterVolume()
        {
            AudioListener.volume = _masterVolume;
        }

        private void RestartLevel()
        {
            CloseSettings();
            CloseMenu();
            StartLevel(_currentLevelIndex);
        }

        private void ReturnToFirstLevel()
        {
            CloseSettings();
            CloseMenu();
            ShowStartScreen();
        }

        private void TogglePause()
        {
            if (_isMenuOpen || _isResolvingSignal || !_levelActive || _isSettingsOpen || _isResultsOpen || _isRulesOpen)
            {
                return;
            }

            _isPaused = !_isPaused;
            ApplyPauseState();
            _hud.SetStatus(_isPaused ? "PAUSED" : "SYSTEM ONLINE");
        }

        private void ToggleMenu()
        {
            if (_isSettingsOpen)
            {
                CloseSettings();
                return;
            }

            if (_isRulesOpen)
            {
                CloseRules();
                return;
            }

            if (!_levelActive)
            {
                return;
            }

            if (_isMenuOpen)
            {
                ResumeFromMenu();
                return;
            }

            _isMenuOpen = true;
            SetPaused(true);
            _hud.SetMenuState(true);
            _hud.SetStatus("MENU");
        }

        private void ResumeFromMenu()
        {
            if (!_isMenuOpen)
            {
                return;
            }

            _isMenuOpen = false;
            _hud.SetMenuState(false);
            SetPaused(false);
            _hud.SetStatus("SYSTEM ONLINE");
        }

        private void CloseMenu()
        {
            _isMenuOpen = false;
            _hud.SetMenuState(false);
            SetPaused(false);
        }

        private void SetPaused(bool paused)
        {
            _isPaused = paused;
            ApplyPauseState();
        }

        private void ApplyPauseState()
        {
            Time.timeScale = _isPaused ? 0f : 1f;
            _hud.SetPauseState(_isPaused);
            _gridManager.SetInteractionEnabled(_levelActive && !_isPaused && !_isMenuOpen && !_isResolvingSignal && !_isSettingsOpen && !_isRulesOpen);
        }

        private void OnGridChanged()
        {
            if (!_levelActive)
            {
                return;
            }

            UpdateObjectiveProgress();
            UpdatePoolStats();

            if (_gridManager.IsLevelCompleted())
            {
                _transitionRoutine = StartCoroutine(ResolveSignalRoutine());
            }
        }

        private IEnumerator ResolveSignalRoutine()
        {
            if (_isResolvingSignal)
            {
                yield break;
            }

            _isResolvingSignal = true;
            _levelActive = false;
            CloseMenu();
            _gridManager.SetInteractionEnabled(false);
            _hud.SetStatus("SIGNAL ROUTING...");

            if (_gridManager.TryGetAllExitPaths(out var allPaths))
            {
                for (var p = 0; p < allPaths.Count; p++)
                {
                    var path = allPaths[p];
                    for (var i = 0; i < path.Count; i++)
                    {
                        path[i].TriggerPulse(0.20f);
                        yield return new WaitForSecondsRealtime(0.06f);
                    }

                    yield return new WaitForSecondsRealtime(0.08f);
                }
            }

            yield return new WaitForSecondsRealtime(0.15f);

            PlayOneShot(_successClip, 0.65f);
            _hud.Flash(new Color(GamePalette.Current.Signal.r, GamePalette.Current.Signal.g, GamePalette.Current.Signal.b, 0.38f), 0.35f);
            _hud.SetStatus("SYSTEM RESTORED");

            _lastResultSuccess = true;
            _levelScore = CalculateScore(success: true, out var rating);
            _totalScore += _levelScore;

            var nextLevel = _currentLevelIndex + 1;
            _pendingNextLevel = Mathf.Min(nextLevel, _levels.Count - 1);
            if (nextLevel >= _levels.Count)
            {
                SaveUnlockedLevel(_levels.Count - 1);
                ShowResults("ALL LEVELS STABLE", rating, false);
                yield break;
            }

            SaveUnlockedLevel(nextLevel);
            ShowResults("SYSTEM RESTORED", rating, true);
        }

        private void HandleSystemFailure()
        {
            if (_isResolvingSignal)
            {
                return;
            }

            _levelActive = false;
            CloseMenu();
            _gridManager.SetInteractionEnabled(false);
            _hud.SetTimer(0f);
            PlayOneShot(_failureClip, 0.72f);
            _hud.Flash(new Color(GamePalette.Current.Error.r, GamePalette.Current.Error.g, GamePalette.Current.Error.b, 0.42f), 0.42f);
            _hud.SetStatus("SYSTEM FAILURE");

            _lastResultSuccess = false;
            _quickCombo = 1;
            _levelScore = CalculateScore(success: false, out var rating);
            ShowResults("SYSTEM FAILURE", rating, false);
        }

        private void ShowResults(string header, string rating, bool nextAvailable)
        {
            _isResultsOpen = true;
            _hud.SetResultsContent(header, _levelScore, _rotationCount, _remainingTime, _quickCombo, rating, nextAvailable);
            _hud.SetResultsState(true);
            _hud.SetMenuState(false);
            _hud.SetSettingsState(false);
            _hud.SetRulesState(false);
        }

        private void OnResultsRetry()
        {
            _isResultsOpen = false;
            _hud.SetResultsState(false);
            StartLevel(_currentLevelIndex);
        }

        private void OnResultsNext()
        {
            if (!_lastResultSuccess)
            {
                return;
            }

            _isResultsOpen = false;
            _hud.SetResultsState(false);

            if (_currentLevelIndex >= _levels.Count - 1)
            {
                ShowStartScreen();
                return;
            }

            StartLevel(_pendingNextLevel);
        }

        private int CalculateScore(bool success, out string rating)
        {
            var config = _levels[_currentLevelIndex];
            var timeRatio = config.TimerSeconds > 0f ? Mathf.Clamp01(_remainingTime / config.TimerSeconds) : 0f;
            var timeScore = Mathf.RoundToInt(timeRatio * 1000f);
            var rotationScore = Mathf.Max(0, 700 - (_rotationCount * 25));

            _gridManager.GetObjectiveProgress(out var connectedObjectives, out var totalObjectives);
            var objectiveScore = totalObjectives > 0 ? Mathf.RoundToInt((connectedObjectives / (float)totalObjectives) * 700f) : 0;

            if (success)
            {
                if (timeRatio >= config.QuickFinishThreshold)
                {
                    _quickCombo++;
                }
                else
                {
                    _quickCombo = 1;
                }
            }

            var baseScore = success ? (timeScore + rotationScore + objectiveScore) : Mathf.RoundToInt(objectiveScore * 0.5f);
            var comboMultiplier = success ? Mathf.Max(1, _quickCombo) : 1;
            var finalScore = baseScore * comboMultiplier;

            if (!success)
            {
                rating = "F";
                return Mathf.Max(0, finalScore);
            }

            rating = finalScore >= 2600 ? "S" : finalScore >= 1800 ? "A" : finalScore >= 1100 ? "B" : "C";
            return Mathf.Max(0, finalScore);
        }

        private void UpdateObjectiveProgress()
        {
            _gridManager.GetObjectiveProgress(out var connected, out var total);
            _hud.SetProgress(connected, total);
        }

        private void UpdatePoolStats()
        {
            _hud.SetPoolStats(
                _gridManager.ActiveNodeCount,
                _gridManager.CachedNodeCount,
                _gridManager.CreatedNodeCount,
                _gridManager.ReusedNodeCount);
        }

        private void PlayOneShot(AudioClip clip, float volumeScale)
        {
            if (_audioSource == null || clip == null)
            {
                return;
            }

            _audioSource.PlayOneShot(clip, volumeScale);
        }

        private static AudioClip CreateToneClip(float frequency, float durationSeconds, float fadeSeconds)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.Max(32, Mathf.CeilToInt(sampleRate * durationSeconds));
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = BuildEnvelope(t, durationSeconds, fadeSeconds);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
            }

            var clip = AudioClip.Create($"Tone_{frequency}", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateSweepClip(float startFrequency, float endFrequency, float durationSeconds, float fadeSeconds)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.Max(32, Mathf.CeilToInt(sampleRate * durationSeconds));
            var samples = new float[sampleCount];

            var phase = 0f;
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)(sampleCount - 1);
                var frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                phase += (2f * Mathf.PI * frequency) / sampleRate;

                var envelope = BuildEnvelope(i / (float)sampleRate, durationSeconds, fadeSeconds);
                samples[i] = Mathf.Sin(phase) * envelope;
            }

            var clip = AudioClip.Create($"Sweep_{startFrequency}_{endFrequency}", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float BuildEnvelope(float time, float duration, float fade)
        {
            if (fade <= 0f)
            {
                return 1f;
            }

            var attack = Mathf.Clamp01(time / fade);
            var release = Mathf.Clamp01((duration - time) / fade);
            return Mathf.Clamp01(Mathf.Min(attack, release));
        }

        private static bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private static bool IsRestartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.rKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.R);
#endif
        }

        private IEnumerator LoadNextLevelRoutine(int nextLevel)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            StartLevel(nextLevel);
        }

        private IEnumerator RestartCurrentLevelRoutine()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            StartLevel(_currentLevelIndex);
        }

        private IEnumerator RestartFromBeginningRoutine()
        {
            _hud.SetStatus("ALL SYSTEMS STABLE");
            yield return new WaitForSecondsRealtime(1.6f);
            StartLevel(0);
        }

        private List<LevelConfig> LoadLevels()
        {
            var levels = new List<LevelConfig>();

            var database = Resources.Load<LevelDatabaseSO>("LevelDatabase");
            if (database != null)
            {
                var configs = database.ToConfigs();
                if (configs.Count > 0)
                {
                    levels.AddRange(configs);
                }
            }

            if (levels.Count == 0)
            {
                levels.AddRange(BuildProgressiveLevels(TargetLevelCount, 0));
                return levels;
            }

            if (levels.Count < TargetLevelCount)
            {
                levels.AddRange(BuildProgressiveLevels(TargetLevelCount - levels.Count, levels.Count));
            }
            else if (levels.Count > TargetLevelCount)
            {
                levels.RemoveRange(TargetLevelCount, levels.Count - TargetLevelCount);
            }

            return levels;
        }

        private static List<LevelConfig> BuildProgressiveLevels(int count, int startIndex)
        {
            var generated = new List<LevelConfig>(Mathf.Max(0, count));
            for (var i = 0; i < count; i++)
            {
                var progression = startIndex + i;
                var gridSize = progression < 5 ? 3 : progression < 10 ? 4 : 5;
                var timer = Mathf.Max(30f, 62f - progression * 1.35f);
                var allowTNodes = progression >= 3;
                var lockedNodes = Mathf.Min(gridSize * gridSize - 3, progression / 2);
                var exitCount = Mathf.Clamp(1 + progression / 6, 1, 3);
                var requiredNodes = Mathf.Clamp(progression / 4, 0, gridSize + 1);
                var overloadedNodes = Mathf.Clamp((progression - 5) / 4, 0, gridSize);
                var penalty = Mathf.Min(4.5f, 2.4f + progression * 0.09f);
                var quickThreshold = Mathf.Clamp(0.72f - progression * 0.012f, 0.45f, 0.72f);
                var seed = 1201 + progression * 173;

                generated.Add(new LevelConfig(
                    gridSize,
                    timer,
                    allowTNodes,
                    lockedNodes,
                    seed,
                    exitCount,
                    requiredNodes,
                    overloadedNodes,
                    penalty,
                    quickThreshold));
            }

            return generated;
        }

        private int LoadUnlockedLevel()
        {
            var unlocked = PlayerPrefs.GetInt(ProgressKey, 0);
            return Mathf.Clamp(unlocked, 0, _levels.Count - 1);
        }

        private void SaveUnlockedLevel(int unlockedLevel)
        {
            var clamped = Mathf.Clamp(unlockedLevel, 0, _levels.Count - 1);
            var stored = PlayerPrefs.GetInt(ProgressKey, 0);
            if (clamped <= stored)
            {
                return;
            }

            PlayerPrefs.SetInt(ProgressKey, clamped);
            PlayerPrefs.Save();
        }
    }
}
