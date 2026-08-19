using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using INART.Framework.Core;
using INART.SlothyBakery.Core;
using INART.SlothyBakery.Analytics;
using INART.SlothyBakery.DailyChallenge;

namespace INART.Framework.Modular
{
    /// <summary>
    /// Central orchestrator for the modular minigame flow.
    ///
    /// SETUP (one-time, in your persistent Loader scene):
    ///   1. Add this component to the same persistent GameObject as your other managers.
    ///   2. Assign a <see cref="GameFlowConfigSO"/> asset in the Inspector.
    ///   3. That's it — no more hardcoded scene names or MINIGAME_COUNT constants.
    ///
    /// HOW IT WORKS:
    ///   - Tracks which minigame is currently active (by scene name).
    ///   - Tracks round progress per minigame (how many rounds completed).
    ///   - Exposes <see cref="OnRoundCompleted"/> for minigames to call instead of
    ///     writing to SaveLoadState directly.
    ///   - Fires events that ModularProgressionCanvas, InitialMenuManager, and
    ///     DailyChallengeManager can subscribe to — they don't need to poll anything.
    ///
    /// ADDING A NEW MINIGAME:
    ///   1. Create a MinigameDefinitionSO asset.
    ///   2. Fill in sceneName, roundCount, etc.
    ///   3. Drag it into the GameFlowConfigSO's minigames list.
    ///   4. Add the scene to Build Settings.
    ///   Done. Zero code changes.
    /// </summary>
    public class GameFlowManager : Singleton<GameFlowManager>
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Tooltip("The master config asset that defines the minigame sequence.")]
        [SerializeField] private GameFlowConfigSO config;

        // ── Singleton config ───────────────────────────────────────────────────
        protected override bool PersistAcrossScenes => true;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when a round inside the current minigame is completed.
        /// Parameters: (minigameIndex, roundIndex, isLastRound)
        /// </summary>
        public static event Action<int, int, bool> OnRoundCompleted;

        /// <summary>
        /// Fired when all rounds of a minigame are completed and the manager
        /// is about to load the next scene.
        /// Parameter: minigameIndex of the completed minigame.
        /// </summary>
        public static event Action<int> OnMinigameCompleted;

        /// <summary>
        /// Fired when the full game is completed (last minigame done).
        /// </summary>
        public static event Action OnGameCompleted;

        /// <summary>
        /// Fired whenever the unlock state changes (e.g. after completing a minigame).
        /// The main menu subscribes to this to refresh button states.
        /// </summary>
        public static event Action OnProgressChanged;

        // ── Runtime state ──────────────────────────────────────────────────────

        // Index of the currently active minigame (matches position in config.minigames).
        private int _currentMinigameIndex = -1;

        // How many rounds have been completed in the current minigame this session.
        private int _currentRoundIndex = 0;

        // Persisted progress: roundsCompleted[i] = rounds done in minigame i (ever).
        // Stored in PlayerPrefs as JSON so it survives between sessions.
        private FlowProgressData _progress;

        private const string PREFS_KEY = "modular_flow_progress";

        // ── Lifecycle ──────────────────────────────────────────────────────────

        protected override void OnAwake()
        {
            if (config == null)
            {
                GameLogger.Error(
                    "GameFlowManager: no GameFlowConfigSO assigned. " +
                    "Assign one in the Inspector.",
                    LogCategory.System);
                return;
            }

            LoadProgress();
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameLogger.Log(
                $"GameFlowManager ready. {config.MinigameCount} minigames configured.",
                LogCategory.System);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // ── Scene tracking ─────────────────────────────────────────────────────

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (config == null) return;

            int idx = config.IndexOf(scene.name);
            if (idx < 0)
            {
                // Not a minigame scene (could be main menu, cutscene, loader)
                _currentMinigameIndex = -1;
                return;
            }

            _currentMinigameIndex = idx;
            _currentRoundIndex    = 0;

            GameLogger.Log(
                $"GameFlowManager: entered minigame [{idx}] '{scene.name}'. " +
                $"RoundCount={CurrentMinigameDef?.roundCount}",
                LogCategory.Gameplay);
        }

        // ── Public read API ────────────────────────────────────────────────────

        /// <summary>Config asset. Null if not assigned in Inspector.</summary>
        public GameFlowConfigSO Config => config;

        /// <summary>
        /// Definition of the minigame currently running.
        /// Null if the current scene is not a registered minigame.
        /// </summary>
        public MinigameDefinitionSO CurrentMinigameDef =>
            config?.GetMinigame(_currentMinigameIndex);

        /// <summary>0-based index of the current minigame, or -1 if not in one.</summary>
        public int CurrentMinigameIndex => _currentMinigameIndex;

        /// <summary>How many rounds have been completed in the current minigame this session.</summary>
        public int CurrentRoundIndex => _currentRoundIndex;

        /// <summary>
        /// True if the minigame at <paramref name="index"/> is unlocked and playable.
        /// Index 0 (first minigame) is always unlocked unless overridden.
        /// A minigame is unlocked when the previous one is fully completed.
        /// </summary>
        public bool IsUnlocked(int index)
        {
            if (config == null || index < 0 || index >= config.MinigameCount)
                return false;

            var def = config.GetMinigame(index);
            if (def != null && def.alwaysUnlocked) return true;
            if (index == 0) return true;

            // Unlocked when the previous minigame is fully completed
            return IsMinigameFullyCompleted(index - 1);
        }

        /// <summary>
        /// True if all rounds of minigame at <paramref name="index"/> have been completed.
        /// </summary>
        public bool IsMinigameFullyCompleted(int index)
        {
            if (config == null || index < 0 || index >= config.MinigameCount)
                return false;

            var def = config.GetMinigame(index);
            if (def == null) return false;

            return _progress.GetRoundsCompleted(index) >= def.roundCount;
        }

        /// <summary>
        /// How many rounds of minigame <paramref name="index"/> have been completed
        /// across all sessions (persisted).
        /// </summary>
        public int GetRoundsCompleted(int index) => _progress.GetRoundsCompleted(index);

        // ── Public write API (called by minigames) ─────────────────────────────

        /// <summary>
        /// Call this when a round inside the current minigame is successfully completed.
        /// Replaces the old pattern of calling ProgressionCanvas.FillBar + UnlockStuff +
        /// SaveLoadState.SetActualState + AnalyticsManager + DailyChallengeManager manually.
        ///
        /// This method fires OnRoundCompleted (ProgressionCanvas listens),
        /// handles Analytics, DailyChallenge, progression save, and — if it was the
        /// last round — fires OnMinigameCompleted and triggers the scene transition.
        /// </summary>
        public void ReportRoundCompleted()
        {
            if (_currentMinigameIndex < 0)
            {
#if UNITY_EDITOR
                // In editor, allow testing from unregistered scenes using minigame 0
                if (config != null && config.MinigameCount > 0)
                {
                    GameLogger.Warning(
                        "GameFlowManager.ReportRoundCompleted: not in a registered scene. " +
                        "Using minigame [0] for editor testing.",
                        LogCategory.Gameplay);
                    _currentMinigameIndex = 0;
                    _currentRoundIndex    = 0;
                }
                else
                {
#endif
                GameLogger.Warning(
                    "GameFlowManager.ReportRoundCompleted called outside a minigame scene.",
                    LogCategory.Gameplay);
                return;
#if UNITY_EDITOR
                }
#endif
            }

            var def = CurrentMinigameDef;
            if (def == null) return;

            // Ensure _progress is initialized (safety net for editor debug flow)
            if (_progress == null) _progress = new FlowProgressData();

            // Persist the new round completion
            _progress.IncrementRoundsCompleted(_currentMinigameIndex);
            SaveProgress();

            bool isLastRound = _currentRoundIndex >= def.roundCount - 1;

            GameLogger.Log(
                $"GameFlowManager: round {_currentRoundIndex + 1}/{def.roundCount} " +
                $"completed in '{def.sceneName}'. Last={isLastRound}",
                LogCategory.Gameplay);

            // Analytics
            AnalyticsManager.Instance?.TrackLevelCompleted(
                $"{def.ResolvedAnalyticsKey}_round_{_currentRoundIndex}", true);

            // Daily Challenge
            if (!string.IsNullOrEmpty(def.dailyChallengeKey))
                DailyChallengeManager.Instance?.AddProgress(def.dailyChallengeKey, 1);

            // Notify listeners (ModularProgressionCanvas updates its bars here)
            OnRoundCompleted?.Invoke(_currentMinigameIndex, _currentRoundIndex, isLastRound);
            OnProgressChanged?.Invoke();

            _currentRoundIndex++;

            if (isLastRound)
                StartCoroutine(HandleMinigameCompleted());
        }

        // ── Internal flow ──────────────────────────────────────────────────────

        private IEnumerator HandleMinigameCompleted()
        {
            int completedIndex = _currentMinigameIndex;
            var def = config.GetMinigame(completedIndex);

            GameLogger.Log(
                $"GameFlowManager: minigame '{def?.sceneName}' fully completed.",
                LogCategory.Gameplay);

            OnMinigameCompleted?.Invoke(completedIndex);

            bool isLastMinigame = completedIndex >= config.MinigameCount - 1;

            if (isLastMinigame)
            {
                GameLogger.Log("GameFlowManager: all minigames done — firing OnGameCompleted.",
                    LogCategory.Gameplay);
                OnGameCompleted?.Invoke();

                // Small pause so animations can finish before the scene loads
                yield return new WaitForSeconds(1.5f);
                SceneManager.LoadScene(config.finalCutsceneSceneName);
            }
            else
            {
                // Small pause so animations can finish before the scene loads
                yield return new WaitForSeconds(1.5f);

                var nextDef = config.GetMinigame(completedIndex + 1);
                if (nextDef != null)
                {
                    GameLogger.Log(
                        $"GameFlowManager: loading next minigame '{nextDef.sceneName}'.",
                        LogCategory.Gameplay);
                    SceneManager.LoadScene(nextDef.sceneName);
                }
                else
                {
                    GameLogger.Error(
                        $"GameFlowManager: next minigame at index {completedIndex + 1} is null.",
                        LogCategory.Gameplay);
                }
            }
        }

        // ── Persistence ────────────────────────────────────────────────────────

        private void LoadProgress()
        {
            string json = PlayerPrefs.GetString(PREFS_KEY, "");
            _progress = string.IsNullOrEmpty(json)
                ? new FlowProgressData()
                : JsonUtility.FromJson<FlowProgressData>(json) ?? new FlowProgressData();

            GameLogger.Log("GameFlowManager: progress loaded.", LogCategory.Save);
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(_progress));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Wipes all modular flow progress. Call from a "New Game" button or from
        /// DevTools during testing.
        /// </summary>
        public void DeleteProgress()
        {
            _progress = new FlowProgressData();
            PlayerPrefs.DeleteKey(PREFS_KEY);
            PlayerPrefs.Save();
            OnProgressChanged?.Invoke();
            GameLogger.Log("GameFlowManager: progress deleted.", LogCategory.Save);
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Log Flow State")]
        private void DebugLogState()
        {
            if (config == null) { Debug.Log("[GameFlowManager] No config assigned."); return; }
            if (_progress == null) { Debug.Log("[GameFlowManager] Not initialized yet — enter Play Mode first."); return; }
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[GameFlowManager] Config: '{config.name}' — {config.MinigameCount} minigames");
            sb.AppendLine($"  CurrentMinigameIndex : {_currentMinigameIndex}");
            sb.AppendLine($"  CurrentRoundIndex    : {_currentRoundIndex}");
            for (int i = 0; i < config.MinigameCount; i++)
            {
                var def = config.GetMinigame(i);
                int done = _progress.GetRoundsCompleted(i);
                string unlocked = IsUnlocked(i) ? "UNLOCKED" : "locked";
                string completed = IsMinigameFullyCompleted(i) ? "DONE" : $"{done}/{def?.roundCount}";
                sb.AppendLine($"  [{i}] '{def?.sceneName}' — {unlocked} — {completed}");
            }
            Debug.Log(sb.ToString());
        }

        [ContextMenu("Debug: Reset Progress")]
        private void DebugResetProgress()
        {
            DeleteProgress();
            Debug.Log("[GameFlowManager] Progress reset.");
        }
#endif
    }

    // ── Persistence data ───────────────────────────────────────────────────────

    /// <summary>
    /// Serializable save data for the modular flow.
    /// Stored as JSON in PlayerPrefs under "modular_flow_progress".
    /// </summary>
    [Serializable]
    internal class FlowProgressData
    {
        /// <summary>
        /// rounds[i] = how many rounds have been completed in minigame i (cumulative,
        /// capped at that minigame's roundCount). Index matches GameFlowConfigSO.minigames.
        /// </summary>
        public List<int> rounds = new List<int>();

        public int GetRoundsCompleted(int minigameIndex)
        {
            if (minigameIndex < 0 || minigameIndex >= rounds.Count) return 0;
            return rounds[minigameIndex];
        }

        public void IncrementRoundsCompleted(int minigameIndex)
        {
            // Grow the list if needed
            while (rounds.Count <= minigameIndex)
                rounds.Add(0);
            rounds[minigameIndex]++;
        }
    }
}
