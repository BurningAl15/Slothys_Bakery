using System;
using System.Collections.Generic;
using UnityEngine;
using INART.Framework.Core;

namespace INART.SlothyBakery.DailyChallenge
{
    using Core;
    using Analytics;

    /// <summary>
    /// Local daily challenge system — no server required.
    ///
    /// HOW IT WORKS:
    ///   • On startup, checks if today's challenge exists in PlayerPrefs.
    ///   • If not (new day), picks a random challenge from the assigned definitions.
    ///   • Game code calls AddProgress(key, amount) when the relevant action happens.
    ///   • When progress >= target, fires OnChallengeCompleted.
    ///   • ClaimReward() marks the reward as collected and fires OnRewardClaimed.
    ///
    /// SETUP:
    ///   1. Create DailyChallengeDefinition assets via
    ///      Assets → Create → INART → Daily Challenge Definition
    ///   2. Assign them to the Definitions list in the Inspector.
    ///   3. Add this component to the persistent Loader GameObject.
    ///   4. Subscribe to OnChallengeCompleted from your UI to show the reward banner.
    ///
    /// INTEGRATIONS:
    ///   • Fires AnalyticsManager.TrackDailyChallengeCompleted on completion.
    ///   • Reads/writes GameConstants.PREFS_DAILY_* keys.
    ///   • Editor context menus for fast testing without waiting a real day.
    /// </summary>
    public class DailyChallengeManager : Singleton<DailyChallengeManager>
    {
        // ── Singleton config ───────────────────────────────────────────────────
        protected override bool PersistAcrossScenes => true;

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Challenge Pool")]
        [Tooltip("All possible daily challenges for this game. At least one required.")]
        [SerializeField] private List<DailyChallengeDefinition> definitions
            = new List<DailyChallengeDefinition>();

        [Header("Debug")]
        [Tooltip("When true, always generates a new challenge on startup (ignores saved date).")]
        [SerializeField] private bool forceNewChallengeOnStart = false;

        // ── Events ─────────────────────────────────────────────────────────────
        /// <summary>Fired when the player reaches the target for today's challenge.</summary>
        public event Action<DailyChallengeState> OnChallengeCompleted;

        /// <summary>Fired when the reward is successfully claimed.</summary>
        public event Action<DailyChallengeState> OnRewardClaimed;

        /// <summary>Fired whenever progress updates (even if not yet complete).</summary>
        public event Action<DailyChallengeState> OnProgressUpdated;

        // ── Runtime state ──────────────────────────────────────────────────────
        private DailyChallengeState _state;
        private DailyChallengeDefinition _activeDefinition;

        // ── Properties ────────────────────────────────────────────────────────

        public DailyChallengeState  State            => _state;
        public DailyChallengeDefinition Definition   => _activeDefinition;
        public bool HasChallengeToday                => _state != null;
        public bool IsTodayCompleted                 => _state != null && _state.isCompleted;
        public bool IsRewardClaimed                  => _state != null && _state.rewardClaimed;

        /// <summary>Progress as 0–1 float, clamped.</summary>
        public float ProgressNormalized => _state == null || _state.targetValue == 0
            ? 0f
            : Mathf.Clamp01((float)_state.progress / _state.targetValue);

        // ── Lifecycle ──────────────────────────────────────────────────────────

        protected override void OnAwake()
        {
            if (definitions == null || definitions.Count == 0)
            {
                GameLogger.Warning(
                    "DailyChallengeManager: no definitions assigned. Add at least one in the Inspector.",
                    LogCategory.Gameplay);
                return;
            }

            LoadOrGenerateChallenge();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Add progress toward the current challenge.
        /// Only counts if the challengeKey matches today's active challenge.
        ///
        /// Usage:
        ///   // At the end of a successful round in Minigame1:
        ///   DailyChallengeManager.Instance?.AddProgress("complete_rounds", 1);
        /// </summary>
        public void AddProgress(string challengeKey, int amount = 1)
        {
            if (_state == null || _state.isCompleted) return;
            if (_state.challengeKey != challengeKey)  return;

            _state.progress = Mathf.Min(_state.progress + amount, _state.targetValue);
            SaveState();

            GameLogger.Log(
                $"DailyChallenge '{challengeKey}': {_state.progress}/{_state.targetValue}",
                LogCategory.Gameplay);

            OnProgressUpdated?.Invoke(_state);

            if (_state.progress >= _state.targetValue)
                CompleteChallenge();
        }

        /// <summary>
        /// Marks the reward as claimed. Call when the player taps the reward banner.
        /// Does nothing if the challenge isn't completed or already claimed.
        /// </summary>
        public bool ClaimReward()
        {
            if (_state == null || !_state.isCompleted || _state.rewardClaimed)
                return false;

            _state.rewardClaimed = true;
            SaveState();

            OnRewardClaimed?.Invoke(_state);
            GameLogger.Log($"Daily challenge reward claimed: {_state.challengeKey}",
                LogCategory.Gameplay);
            return true;
        }

        /// <summary>
        /// Returns the definition matching a given key, or null if not found.
        /// Useful for UI that needs to display challenge details.
        /// </summary>
        public DailyChallengeDefinition GetDefinition(string key)
        {
            foreach (var def in definitions)
                if (def.challengeKey == key) return def;
            return null;
        }

        // ── Internal ───────────────────────────────────────────────────────────

        private void LoadOrGenerateChallenge()
        {
            string todayUtc = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string json     = PlayerPrefs.GetString(GameConstants.PREFS_DAILY_DATE, "");

            // Try to load existing state for today
            if (!forceNewChallengeOnStart && !string.IsNullOrEmpty(json))
            {
                var loaded = JsonUtility.FromJson<DailyChallengeState>(json);
                if (loaded != null && loaded.dateUtc == todayUtc)
                {
                    _state           = loaded;
                    _activeDefinition = GetDefinition(_state.challengeKey);
                    GameLogger.Log(
                        $"Daily challenge loaded: {_state.challengeKey} " +
                        $"({_state.progress}/{_state.targetValue})",
                        LogCategory.Gameplay);
                    return;
                }
            }

            // New day — generate a fresh challenge
            GenerateNewChallenge(todayUtc);
        }

        private void GenerateNewChallenge(string dateUtc)
        {
            // Pick randomly, but avoid repeating yesterday's challenge if possible
            string yesterday = PlayerPrefs.GetString(GameConstants.PREFS_DAILY_PROGRESS, "");
            DailyChallengeDefinition pick = null;

            if (definitions.Count == 1)
            {
                pick = definitions[0];
            }
            else
            {
                var candidates = new List<DailyChallengeDefinition>(definitions);
                // Try to remove yesterday's challenge key to avoid repeat
                if (!string.IsNullOrEmpty(yesterday))
                {
                    var prev = JsonUtility.FromJson<DailyChallengeState>(yesterday);
                    if (prev != null)
                        candidates.RemoveAll(d => d.challengeKey == prev.challengeKey);
                }
                pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            }

            _activeDefinition = pick;
            _state = new DailyChallengeState
            {
                dateUtc      = dateUtc,
                challengeKey = pick.challengeKey,
                targetValue  = pick.targetValue,
                progress     = 0,
                isCompleted  = false,
                rewardClaimed = false
            };

            SaveState();
            GameLogger.Log(
                $"New daily challenge generated: {pick.challengeKey} (target={pick.targetValue})",
                LogCategory.Gameplay);
        }

        private void CompleteChallenge()
        {
            _state.isCompleted = true;
            SaveState();

            GameLogger.Log($"Daily challenge COMPLETED: {_state.challengeKey}", LogCategory.Gameplay);

            if (AnalyticsManager.Instance != null)
                AnalyticsManager.Instance.TrackDailyChallengeCompleted(
                    _state.challengeKey, _state.progress);

            OnChallengeCompleted?.Invoke(_state);
        }

        private void SaveState()
        {
            string json = JsonUtility.ToJson(_state);
            PlayerPrefs.SetString(GameConstants.PREFS_DAILY_DATE, json);
            // Also keep yesterday's for the anti-repeat logic
            PlayerPrefs.SetString(GameConstants.PREFS_DAILY_PROGRESS, json);
            PlayerPrefs.Save();
        }

        // ── Editor helpers ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        [ContextMenu("Simulate: Add 1 Progress")]
        private void EditorAddProgress()
        {
            if (_state != null) AddProgress(_state.challengeKey, 1);
        }

        [ContextMenu("Simulate: Complete Challenge")]
        private void EditorComplete()
        {
            if (_state != null)
            {
                _state.progress = _state.targetValue;
                CompleteChallenge();
            }
        }

        [ContextMenu("Force New Challenge Tomorrow (delete saved state)")]
        private void EditorResetState()
        {
            PlayerPrefs.DeleteKey(GameConstants.PREFS_DAILY_DATE);
            PlayerPrefs.DeleteKey(GameConstants.PREFS_DAILY_PROGRESS);
            PlayerPrefs.Save();
            _state           = null;
            _activeDefinition = null;
            Debug.Log("[DailyChallengeManager] State cleared — restart Play Mode to generate new challenge.");
        }

        [ContextMenu("Log Current State")]
        private void EditorLogState()
        {
            if (_state == null) { Debug.Log("[DailyChallenge] No state."); return; }
            Debug.Log($"[DailyChallenge] Key={_state.challengeKey} " +
                      $"Progress={_state.progress}/{_state.targetValue} " +
                      $"Completed={_state.isCompleted} Claimed={_state.rewardClaimed}");
        }
#endif
    }
}
